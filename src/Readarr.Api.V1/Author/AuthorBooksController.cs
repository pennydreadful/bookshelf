using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using Readarr.Api.V1.Books;
using Readarr.Http;

namespace Readarr.Api.V1.Author
{
    [V1ApiController("author/{authorId:int}/books")]
    public class AuthorBooksController : Controller
    {
        private readonly IAddBookService _addBookService;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IProvideAuthorInfo _authorInfo;
        private readonly IImportListExclusionService _importListExclusionService;

        public AuthorBooksController(IAddBookService addBookService,
                                     IAuthorService authorService,
                                     IBookService bookService,
                                     IMapCoversToLocal coverMapper,
                                     IProvideAuthorInfo authorInfo,
                                     IImportListExclusionService importListExclusionService)
        {
            _addBookService = addBookService;
            _authorService = authorService;
            _bookService = bookService;
            _coverMapper = coverMapper;
            _authorInfo = authorInfo;
            _importListExclusionService = importListExclusionService;
        }

        [HttpGet]
        public List<BookResource> GetAvailable(int authorId)
        {
            var author = _authorService.GetAuthor(authorId);
            var books = GetAvailableBooks(author);

            return MapToResource(books);
        }

        [HttpPost]
        public ActionResult<List<BookResource>> AddBooks(int authorId, [FromBody] AuthorBooksAddResource resource)
        {
            var author = _authorService.GetAuthor(authorId);
            var books = GetAvailableBooks(author);

            if (resource?.ForeignBookIds?.Any() == true)
            {
                var selectedIds = new HashSet<string>(resource.ForeignBookIds);
                books = books.Where(book => selectedIds.Contains(book.ForeignBookId)).ToList();
            }

            if (!books.Any())
            {
                return Ok(new List<BookResource>());
            }

            if (resource?.SearchForNewBook == true)
            {
                books.ForEach(book => book.AddOptions.SearchForNewBook = true);
            }

            books.ForEach(book => book.Monitored = true);

            var added = _addBookService.AddBooks(books);

            return Ok(MapToResource(added));
        }

        [HttpPost("exclude")]
        public IActionResult ExcludeBooks(int authorId, [FromBody] AuthorBooksExcludeResource resource)
        {
            var author = _authorService.GetAuthor(authorId);

            if (resource?.ForeignBookIds == null || !resource.ForeignBookIds.Any())
            {
                return Ok(new { removedCount = 0 });
            }

            var remoteAuthor = _authorInfo.GetAuthorInfo(author.Metadata.Value.ForeignAuthorId, true);
            var remoteBooks = remoteAuthor?.Books?.Value ?? new List<Book>();
            var lookup = remoteBooks.ToDictionary(book => book.ForeignBookId, book => book);
            var removedCount = 0;

            foreach (var foreignId in resource.ForeignBookIds.Distinct())
            {
                if (_importListExclusionService.FindByForeignId(foreignId) != null)
                {
                    continue;
                }

                var title = lookup.TryGetValue(foreignId, out var book) ? book.Title : foreignId;

                _importListExclusionService.Add(new ImportListExclusion
                {
                    ForeignId = foreignId,
                    Name = $"{author.Name} - {title}"
                });

                removedCount++;
            }

            return Ok(new { removedCount });
        }

        private List<Book> GetAvailableBooks(NzbDrone.Core.Books.Author author)
        {
            var remoteAuthor = _authorInfo.GetAuthorInfo(author.Metadata.Value.ForeignAuthorId, true);
            var existingBookIds = _bookService.GetBooksByAuthor(author.Id)
                .Select(book => book.ForeignBookId)
                .ToHashSet();

            var available = remoteAuthor.Books.Value
                .Where(book => !existingBookIds.Contains(book.ForeignBookId))
                .OrderByDescending(book => book.ReleaseDate ?? DateTime.MinValue)
                .ToList();

            if (!available.Any())
            {
                return available;
            }

            var excluded = _importListExclusionService
                .FindByForeignId(available.Select(book => book.ForeignBookId).ToList())
                .Select(exclusion => exclusion.ForeignId)
                .ToHashSet();

            return available.Where(book => !excluded.Contains(book.ForeignBookId)).ToList();
        }

        private List<BookResource> MapToResource(IEnumerable<Book> books)
        {
            return books.Select(MapToResource).ToList();
        }

        private BookResource MapToResource(Book book)
        {
            var resource = book.ToResource();

            _coverMapper.ConvertToLocalUrls(resource.Id, MediaCoverEntity.Book, resource.Images);

            var cover = resource.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);

            if (cover != null)
            {
                resource.RemoteCover = cover.RemoteUrl;
            }

            return resource;
        }
    }
}
