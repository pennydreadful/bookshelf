using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.BookInfo;
using NzbDrone.Core.Test.Framework;
using IGoodreadsSearchProxy = NzbDrone.Core.MetadataSource.Goodreads.IGoodreadsSearchProxy;
using SearchJsonResource = NzbDrone.Core.MetadataSource.Goodreads.SearchJsonResource;

namespace NzbDrone.Core.Test.MetadataSource.BookInfo
{
    [TestFixture]
    public class BookInfoProxyDbIdsFixture : CoreTest<BookInfoProxy>
    {
        private Book _dbBook;
        private Edition _dbEdition;
        private List<BookFile> _bookFiles;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetadataSource)
                .Returns("https://api.bookinfo.pro");

            Mocker.SetConstant<IMetadataRequestBuilder>(Mocker.Resolve<MetadataRequestBuilder>());

            // Two ids route the search through the bulk endpoint (MapSearchResult)
            Mocker.GetMock<IGoodreadsSearchProxy>()
                .Setup(s => s.Search(It.IsAny<string>()))
                .Returns(new List<SearchJsonResource>
                {
                    new SearchJsonResource { BookId = 101, WorkId = 100 },
                    new SearchJsonResource { BookId = 201, WorkId = 200 }
                });

            var bulk = new BulkBookResource
            {
                Works = new List<WorkResource>
                {
                    Work(100, 101),
                    Work(200, 201)
                },
                Series = new List<SeriesResource>(),
                Authors = new List<AuthorResource> { new AuthorResource { ForeignId = 1, Name = "Chris Voss" } }
            };

            Mocker.GetMock<IHttpClient>()
                .Setup(c => c.Post<BulkBookResource>(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(r => new HttpResponse<BulkBookResource>(new HttpResponse(r, new HttpHeader(), bulk.ToJson())));

            _bookFiles = new List<BookFile> { new BookFile { Id = 7, Path = "/books/existing.epub" } };

            _dbBook = new Book { Id = 12, ForeignBookId = "100", BookFiles = new LazyLoaded<List<BookFile>>(_bookFiles) };
            _dbEdition = new Edition { Id = 34, ForeignEditionId = "101", BookFiles = new LazyLoaded<List<BookFile>>(_bookFiles) };

            Mocker.GetMock<IBookService>()
                .Setup(s => s.FindById("100"))
                .Returns(_dbBook);

            Mocker.GetMock<IEditionService>()
                .Setup(s => s.GetEditionsByBook(12))
                .Returns(new List<Edition> { _dbEdition });
        }

        private static WorkResource Work(int workId, int bookId)
        {
            return new WorkResource
            {
                ForeignId = workId,
                Title = "Never Split the Difference",
                Genres = new List<string>(),
                RelatedWorks = new List<int>(),
                Books = new List<BookResource>
                {
                    new BookResource
                    {
                        ForeignId = bookId,
                        Title = "Never Split the Difference",
                        Contributors = new List<ContributorResource> { new ContributorResource { ForeignId = 1 } }
                    }
                }
            };
        }

        [Test]
        public void search_result_matching_a_db_book_should_carry_its_book_files()
        {
            var result = Subject.SearchForNewBook("never split the difference", null, false);

            var book = result.Single(b => b.ForeignBookId == "100");
            book.Id.Should().Be(12);
            book.BookFiles.Should().NotBeNull();
            book.BookFiles.Value.Should().BeEquivalentTo(_bookFiles);

            var edition = book.Editions.Value.Single(e => e.ForeignEditionId == "101");
            edition.Id.Should().Be(34);
            edition.BookFiles.Should().NotBeNull();
            edition.BookFiles.Value.Should().BeEquivalentTo(_bookFiles);
        }
    }
}
