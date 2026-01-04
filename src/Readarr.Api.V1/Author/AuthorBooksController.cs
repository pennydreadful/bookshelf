using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
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

        public AuthorBooksController(IAddBookService addBookService,
                                     IAuthorService authorService,
                                     IBookService bookService,
                                     IMapCoversToLocal coverMapper,
                                     IProvideAuthorInfo authorInfo)
        {
            _addBookService = addBookService;
            _authorService = authorService;
            _bookService = bookService;
            _coverMapper = coverMapper;
            _authorInfo = authorInfo;
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

            var added = _addBookService.AddBooks(books);

            return Ok(MapToResource(added));
        }

        private List<Book> GetAvailableBooks(NzbDrone.Core.Books.Author author)
        {
            var remoteAuthor = _authorInfo.GetAuthorInfo(author.Metadata.Value.ForeignAuthorId, true);
            var existingBookIds = _bookService.GetBooksByAuthor(author.Id)
                .Select(book => book.ForeignBookId)
                .ToHashSet();

            return remoteAuthor.Books.Value
                .Where(book => !existingBookIds.Contains(book.ForeignBookId))
                .OrderByDescending(book => book.ReleaseDate ?? DateTime.MinValue)
                .ToList();
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
