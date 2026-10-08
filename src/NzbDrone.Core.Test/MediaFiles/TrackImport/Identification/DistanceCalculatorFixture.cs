using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Identification
{
    [TestFixture]
    public class DistanceCalculatorFixture : TestBase
    {
        [Test]
        public void should_reverse_single_reversed_author()
        {
            var input = new List<string> { "Last, First" };
            var authors = DistanceCalculator.GetAuthorVariants(input);

            authors.Should().Contain("First Last");
        }

        [Test]
        public void should_reverse_two_reversed_author()
        {
            var input = new List<string>
            {
                "Last, First",
                "Last2, First2"
            };

            var authors = DistanceCalculator.GetAuthorVariants(input);

            authors.Should().HaveCount(4);
            authors.Should().Contain("First Last");
            authors.Should().Contain("First2 Last2");
            authors.Should().Contain("Last, First");
            authors.Should().Contain("Last2, First2");
        }

        [Test]
        public void should_not_reverse_single_author()
        {
            var input = new List<string> { "First Last" };
            var authors = DistanceCalculator.GetAuthorVariants(input);

            authors.Should().HaveCount(1);
            authors.Should().Contain("First Last");
        }

        [TestCase("First1 Last1, First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1; First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 & First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 / First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 and First2 Last2", "First1 Last1", "First2 Last2")]
        public void should_split_concatenated_author(string inputString, string first, string second)
        {
            var input = new List<string> { inputString };
            var authors = DistanceCalculator.GetAuthorVariants(input);

            authors.Should().Contain(inputString);
            authors.Should().Contain(first);
            authors.Should().Contain(second);
            authors.Should().HaveCount(3);
        }

        [Test]
        public void should_split_concatenated_with_trailing_and()
        {
            var inputString = "First Last, First2 Last2 & First3 Last3";
            var input = new List<string> { inputString };
            var authors = DistanceCalculator.GetAuthorVariants(input);

            authors.Should().Contain(inputString);
            authors.Should().Contain("First Last");
            authors.Should().Contain("First2 Last2");
            authors.Should().Contain("First3 Last3");
            authors.Should().HaveCount(4);
        }

        [Test]
        public void should_not_split_if_multiple_input()
        {
            var input = new List<string>
            {
                "First Last",
                "Second Third, Fourth Fifth"
            };

            var authors = DistanceCalculator.GetAuthorVariants(input);

            authors.Should().HaveCount(2);
            authors.Should().Contain("First Last");
            authors.Should().Contain("Second Third, Fourth Fifth");
        }

        private static LocalBook GivenLocalBook(string title, string author, string asin = null, string isbn = null)
        {
            return new LocalBook
            {
                Path = @"C:\Books\book.azw3".AsOsAgnostic(),
                FileTrackInfo = new ParsedTrackInfo
                {
                    BookTitle = title,
                    Authors = new List<string> { author },
                    Asin = asin,
                    Isbn = isbn
                }
            };
        }

        private static Edition GivenEdition(string title, string author, string asin = null, string isbn = null)
        {
            var book = new Book
            {
                Title = title,
                AuthorMetadata = new LazyLoaded<AuthorMetadata>(new AuthorMetadata { Name = author }),
                SeriesLinks = new LazyLoaded<List<SeriesBookLink>>(new List<SeriesBookLink>())
            };

            return new Edition
            {
                Title = title,
                Asin = asin,
                Isbn13 = isbn,
                Book = new LazyLoaded<Book>(book)
            };
        }

        [TestCase("a2f97540-d315-4ee6-a025-5325c852d261")]
        [TestCase("urn:uuid:a2f97540-d315-4ee6-a025-5325c852d261")]
        [TestCase("not-an-asin")]
        public void should_ignore_malformed_asin_from_file_tags(string asin)
        {
            // A Calibre conversion writes its internal UUID into the EXTH ASIN record, which would
            // otherwise be compared against the edition ASIN and counted as a mismatch.
            var localBook = GivenLocalBook("Twisted Lies", "Ana Huang", asin: asin);
            var correctEdition = GivenEdition("Twisted Lies", "Ana Huang", asin: "B09TVV9NH2");

            var withBadAsin = DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, correctEdition);
            var withNoAsin = DistanceCalculator.BookDistance(
                new List<LocalBook> { GivenLocalBook("Twisted Lies", "Ana Huang") }, correctEdition);

            withBadAsin.NormalizedDistance().Should().Be(withNoAsin.NormalizedDistance());
        }

        [Test]
        public void should_use_valid_asin_from_file_tags()
        {
            var localBook = GivenLocalBook("Twisted Lies", "Ana Huang", asin: "B09TVV9NH2");

            var matching = GivenEdition("Twisted Lies", "Ana Huang", asin: "B09TVV9NH2");
            var different = GivenEdition("Twisted Lies", "Ana Huang", asin: "B0B9FPHBD6");

            DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, matching)
                .NormalizedDistance()
                .Should()
                .BeLessThan(DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, different).NormalizedDistance());
        }

        [Test]
        public void should_not_prefer_a_different_book_because_it_has_no_asin()
        {
            // Regression: a Calibre UUID in the ASIN tag made the correct book (which has an ASIN)
            // score worse than a different book by the same author that has none, so the file was
            // attached to the wrong book and later deleted as an "upgrade".
            var localBook = GivenLocalBook("Twisted Lies", "Ana Huang", asin: "a2f97540-d315-4ee6-a025-5325c852d261");

            var correctBook = GivenEdition("Twisted Lies", "Ana Huang", asin: "B09TVV9NH2", isbn: "9780349434292");
            var otherBook = GivenEdition("Twisted Games", "Ana Huang", isbn: "9781728274874");

            var correctDistance = DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, correctBook).NormalizedDistance();
            var otherDistance = DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, otherBook).NormalizedDistance();

            correctDistance.Should().BeLessThan(otherDistance);
        }

        [Test]
        public void should_not_penalise_a_differing_asin_more_than_a_missing_one()
        {
            // Editions of the same work legitimately carry different ASINs, so a mismatch must not
            // be treated as stronger evidence than a wrong title.
            var localBook = GivenLocalBook("Twisted Lies", "Ana Huang", asin: "B09TVV9NH2");

            var differingAsin = GivenEdition("Twisted Lies", "Ana Huang", asin: "B0B9FPHBD6");
            var noAsin = GivenEdition("Twisted Lies", "Ana Huang");

            DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, differingAsin)
                .NormalizedDistance()
                .Should()
                .Be(DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, noAsin).NormalizedDistance());
        }

        [Test]
        public void should_prefer_correct_title_over_book_with_matching_missing_identifiers()
        {
            // The real-world case from the data-loss report: the file has a valid ASIN that does
            // not appear on any edition the metadata source holds for the correct book.
            var localBook = GivenLocalBook("Twisted Lies", "Ana Huang", asin: "B09TVV9NH2");

            var correctBook = GivenEdition("Twisted Lies", "Ana Huang", asin: "B0B9FPHBD6", isbn: "9780349434292");
            var otherBook = GivenEdition("Twisted Games", "Ana Huang", isbn: "9781728274874");

            DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, correctBook)
                .NormalizedDistance()
                .Should()
                .BeLessThan(DistanceCalculator.BookDistance(new List<LocalBook> { localBook }, otherBook).NormalizedDistance());
        }
    }
}
