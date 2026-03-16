using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.BookImport;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Identification
{
    [TestFixture]
    public class CandidateServiceFixture : CoreTest<CandidateService>
    {
        private LocalEdition BuildLocalEdition(string bookTitle = "Book", string author = "Author")
        {
            return new LocalEdition
            {
                LocalBooks = new List<LocalBook>
                {
                    new LocalBook
                    {
                        FileTrackInfo = new ParsedTrackInfo
                        {
                            Authors = new List<string> { author },
                            BookTitle = bookTitle
                        }
                    }
                }
            };
        }

        [Test]
        public void should_fall_back_to_author_search_when_book_override_returns_no_candidates()
        {
            var author = new Author { Id = 1, AuthorMetadataId = 10 };
            var book = new Book { Id = 99 };
            var idOverrides = new IdentificationOverrides { Author = author, Book = book };

            // Book override path returns nothing (stale book)
            Mocker.GetMock<IEditionService>()
                  .Setup(s => s.GetEditionsByBook(99))
                  .Returns(new List<Edition>());

            // Author fallback should find candidates
            var edition = new Edition
            {
                Id = 5,
                BookId = 50,
                ForeignEditionId = "edition-1",
                Ratings = new Ratings { Votes = 1, Value = 1.0m },
                Book = new LazyLoaded<Book>(new Book
                {
                    Id = 50,
                    ForeignBookId = "foreign-50",
                    Author = new LazyLoaded<Author>(author)
                })
            };

            Mocker.GetMock<IBookService>()
                  .Setup(s => s.GetCandidates(10, "Book"))
                  .Returns(new List<Book> { new Book { Id = 50 } });

            Mocker.GetMock<IEditionService>()
                  .Setup(s => s.GetEditionsByBook(50))
                  .Returns(new List<Edition> { edition });

            Mocker.GetMock<IEditionService>()
                  .Setup(s => s.GetCandidates(10, "Book"))
                  .Returns(new List<Edition>());

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByBook(It.IsAny<int>()))
                  .Returns(new List<BookFile>());

            var localEdition = BuildLocalEdition();
            var result = Subject.GetDbCandidatesFromTags(localEdition, idOverrides, false);

            result.Should().HaveCount(1);
            result.First().Edition.Id.Should().Be(5);
        }

        [Test]
        public void should_not_throw_on_goodreads_exception()
        {
            Mocker.GetMock<ISearchForNewBook>()
                .Setup(s => s.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>(), true))
                .Throws(new GoodreadsException("Bad search"));

            var edition = new LocalEdition
            {
                LocalBooks = new List<LocalBook>
                {
                    new LocalBook
                    {
                        FileTrackInfo = new ParsedTrackInfo
                        {
                            Authors = new List<string> { "Author" },
                            BookTitle = "Book"
                        }
                    }
                }
            };

            Subject.GetRemoteCandidates(edition, null).Should().BeEmpty();
        }
    }
}
