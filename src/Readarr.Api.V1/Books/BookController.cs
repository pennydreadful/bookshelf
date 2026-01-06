using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.AuthorStats;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;
using Readarr.Http;

namespace Readarr.Api.V1.Books
{
    [V1ApiController]
    public class BookController : BookControllerWithSignalR,
        IHandle<BookGrabbedEvent>,
        IHandle<BookEditedEvent>,
        IHandle<BookUpdatedEvent>,
        IHandle<BookDeletedEvent>,
        IHandle<BookImportedEvent>,
        IHandle<TrackImportedEvent>,
        IHandle<BookFileDeletedEvent>
    {
        protected readonly IAuthorService _authorService;
        protected readonly IEditionService _editionService;
        protected readonly IAddBookService _addBookService;
        private readonly IRefreshBookService _refreshBookService;
        private readonly IConfigService _configService;

        public BookController(IAuthorService authorService,
                          IBookService bookService,
                          IAddBookService addBookService,
                          IEditionService editionService,
                          ISeriesBookLinkService seriesBookLinkService,
                          IAuthorStatisticsService authorStatisticsService,
                          IRefreshBookService refreshBookService,
                          IConfigService configService,
                          IMapCoversToLocal coverMapper,
                          IUpgradableSpecification upgradableSpecification,
                          IBroadcastSignalRMessage signalRBroadcaster,
                          QualityProfileExistsValidator qualityProfileExistsValidator,
                          MetadataProfileExistsValidator metadataProfileExistsValidator)

        : base(bookService, seriesBookLinkService, authorStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _authorService = authorService;
            _editionService = editionService;
            _addBookService = addBookService;
            _refreshBookService = refreshBookService;
            _configService = configService;

            PostValidator.RuleFor(s => s.ForeignBookId).NotEmpty();
            PostValidator.RuleFor(s => s.Author.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            PostValidator.RuleFor(s => s.Author.MetadataProfileId).SetValidator(metadataProfileExistsValidator);
            PostValidator.RuleFor(s => s.Author.RootFolderPath).IsValidPath().When(s => s.Author.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.Author.ForeignAuthorId).NotEmpty();
        }

        [HttpGet]
        public List<BookResource> GetBooks([FromQuery]int? authorId,
            [FromQuery]List<int> bookIds,
            [FromQuery]string titleSlug,
            [FromQuery]bool includeAllAuthorBooks = false)
        {
            if (!authorId.HasValue && !bookIds.Any() && titleSlug.IsNullOrWhiteSpace())
            {
                var editionTask = Task.Run(() => _editionService.GetAllMonitoredEditions());
                var metadataTask = Task.Run(() => _authorService.GetAllAuthors());
                var books = _bookService.GetAllBooks();

                var editions = editionTask.GetAwaiter().GetResult().GroupBy(x => x.BookId).ToDictionary(x => x.Key, y => y.ToList());

                var authors = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.AuthorMetadataId);

                foreach (var book in books)
                {
                    book.Author = authors[book.AuthorMetadataId];
                    if (editions.TryGetValue(book.Id, out var bookEditions))
                    {
                        book.Editions = bookEditions;
                    }
                    else
                    {
                        book.Editions = new List<Edition>();
                    }
                }

                return MapToResource(books, false);
            }

            if (authorId.HasValue)
            {
                var books = _bookService.GetBooksByAuthor(authorId.Value);

                var author = _authorService.GetAuthor(authorId.Value);
                var editions = _editionService.GetEditionsByAuthor(authorId.Value)
                    .GroupBy(x => x.BookId)
                    .ToDictionary(x => x.Key, y => y.ToList());

                foreach (var book in books)
                {
                    book.Author = author;
                    if (editions.TryGetValue(book.Id, out var bookEditions))
                    {
                        book.Editions = bookEditions;
                    }
                    else
                    {
                        book.Editions = new List<Edition>();
                    }
                }

                return MapToResource(books, false);
            }

            if (titleSlug.IsNotNullOrWhiteSpace())
            {
                var book = _bookService.FindBySlug(titleSlug);

                if (book == null)
                {
                    return MapToResource(new List<Book>(), false);
                }

                if (includeAllAuthorBooks)
                {
                    return MapToResource(_bookService.GetBooksByAuthor(book.AuthorId), false);
                }
                else
                {
                    return MapToResource(new List<Book> { book }, false);
                }
            }

            return MapToResource(_bookService.GetBooks(bookIds), false);
        }

        [HttpGet("{id:int}/overview")]
        public object Overview(int id)
        {
            var overview = _editionService.GetEditionsByBook(id).Single(x => x.Monitored).Overview;
            return new
            {
                id,
                overview
            };
        }

        [HttpPost("{id:int}/refresh-metadata")]
        public ActionResult<BookResource> RefreshMetadata(int id)
        {
            var book = _bookService.GetBook(id);

            _refreshBookService.RefreshBookInfo(book);

            var editions = _editionService.GetEditionsByBook(id);
            var preferredEdition = GetPreferredEdition(editions);
            if (preferredEdition != null && !preferredEdition.Monitored)
            {
                _editionService.SetMonitored(preferredEdition);
            }

            var refreshed = _bookService.GetBook(id);
            _coverMapper.DeleteBookCovers(refreshed.Id);
            _coverMapper.EnsureBookCovers(refreshed);

            return Accepted(MapToResource(refreshed, true));
        }

        private Edition GetPreferredEdition(List<Edition> editions)
        {
            if (editions == null || editions.Count == 0)
            {
                return null;
            }

            var isoLanguage = IsoLanguages.Get((Language)_configService.UILanguage) ?? IsoLanguages.Get(Language.English);
            if (isoLanguage == null)
            {
                return null;
            }

            return editions
                .Where(edition => LanguageMatches(edition.Language, isoLanguage))
                .OrderByDescending(edition => edition.Images?.Any() == true)
                .ThenByDescending(edition => edition.Overview.IsNotNullOrWhiteSpace())
                .FirstOrDefault();
        }

        private static bool LanguageMatches(string editionLanguage, IsoLanguage uiLanguage)
        {
            if (editionLanguage.IsNullOrWhiteSpace())
            {
                return false;
            }

            var normalized = editionLanguage.Trim().Replace('_', '-').ToLowerInvariant();
            var uiTwoLetter = uiLanguage.TwoLetterCode?.ToLowerInvariant();
            var uiThreeLetter = uiLanguage.ThreeLetterCode?.ToLowerInvariant();
            var uiName = uiLanguage.EnglishName?.ToLowerInvariant();

            if (normalized == uiTwoLetter ||
                normalized == uiThreeLetter ||
                normalized == uiName)
            {
                return true;
            }

            if (uiTwoLetter.IsNotNullOrWhiteSpace() && normalized.StartsWith(uiTwoLetter + "-"))
            {
                return true;
            }

            var iso = IsoLanguages.Find(normalized);
            return iso != null && iso.Language == uiLanguage.Language;
        }

        [RestPostById]
        public ActionResult<BookResource> AddBook(BookResource bookResource)
        {
            var book = _addBookService.AddBook(bookResource.ToModel());

            return Created(book.Id);
        }

        [RestPutById]
        public ActionResult<BookResource> UpdateBook(BookResource bookResource)
        {
            var book = _bookService.GetBook(bookResource.Id);

            var model = bookResource.ToModel(book);

            _bookService.UpdateBook(model);
            if (model.Editions?.Value != null)
            {
                _editionService.UpdateMany(model.Editions.Value);
            }
            else
            {
                model.Editions = _editionService.GetEditionsByBook(model.Id);
            }

            BroadcastResourceChange(ModelAction.Updated, model.Id);

            return Accepted(MapToResource(model, false));
        }

        [RestDeleteById]
        public void DeleteBook(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _bookService.DeleteBook(id, deleteFiles, addImportListExclusion);
        }

        [HttpPut("monitor")]
        public IActionResult SetBooksMonitored([FromBody]BooksMonitoredResource resource)
        {
            _bookService.SetMonitored(resource.BookIds, resource.Monitored);

            if (resource.BookIds.Count == 1)
            {
                _bookService.SetBookMonitored(resource.BookIds.First(), resource.Monitored);
            }
            else
            {
                _bookService.SetMonitored(resource.BookIds, resource.Monitored);
            }

            return Accepted(MapToResource(_bookService.GetBooks(resource.BookIds), false));
        }

        [NonAction]
        public void Handle(BookGrabbedEvent message)
        {
            foreach (var book in message.Book.Books)
            {
                var resource = book.ToResource();
                resource.Grabbed = true;

                BroadcastResourceChange(ModelAction.Updated, resource);
            }
        }

        [NonAction]
        public void Handle(BookEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Book, true));
        }

        [NonAction]
        public void Handle(BookUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Book, true));
        }

        [NonAction]
        public void Handle(BookDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Book.ToResource());
        }

        [NonAction]
        public void Handle(BookImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Book, true));
        }

        [NonAction]
        public void Handle(TrackImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.BookInfo.Book.ToResource());
        }

        [NonAction]
        public void Handle(BookFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.BookFile.Edition.Value.Book.Value, true));
        }
    }
}
