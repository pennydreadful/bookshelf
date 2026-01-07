using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class QualityAllowedByProfileSpecificationFixture : CoreTest<QualityAllowedByProfileSpecification>
    {
        private RemoteBook _remoteBook;

        public static object[] AllowedTestCases =
        {
            new object[] { Quality.MP3 },
            new object[] { Quality.MP3 },
            new object[] { Quality.MP3 }
        };

        public static object[] DeniedTestCases =
        {
            new object[] { Quality.FLAC },
            new object[] { Quality.Unknown }
        };

        [SetUp]
        public void Setup()
        {
            var fakeAuthor = Builder<Author>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.MP3.Id })
                         .Build();

            _remoteBook = new RemoteBook
            {
                Author = fakeAuthor,
                ParsedBookInfo = new ParsedBookInfo { Quality = new QualityModel(Quality.MP3, new Revision(version: 2)) },
                Books = new List<Book>
                {
                    BuildBookWithFiles(
                        CreateBookFile("book.epub", Quality.EPUB),
                        CreateBookFile("book.mp3", Quality.MP3))
                }
            };
        }

        [Test]
        [TestCaseSource(nameof(AllowedTestCases))]
        public void should_allow_if_quality_is_defined_in_profile(Quality qualityType)
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = qualityType;
            _remoteBook.Author.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.MP3, Quality.MP3, Quality.MP3);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeTrue();
        }

        [Test]
        [TestCaseSource(nameof(DeniedTestCases))]
        public void should_not_allow_if_quality_is_not_defined_in_profile(Quality qualityType)
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = qualityType;
            _remoteBook.Author.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.MP3, Quality.MP3, Quality.MP3);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_allow_if_media_type_is_missing_from_existing_files()
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = Quality.MP3;
            _remoteBook.Author.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.EPUB);
            _remoteBook.Books = new List<Book>
            {
                BuildBookWithFiles(CreateBookFile("book.epub", Quality.EPUB))
            };

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_allow_if_media_type_exists_for_non_allowed_quality()
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = Quality.MP3;
            _remoteBook.Author.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.EPUB);
            _remoteBook.Books = new List<Book>
            {
                BuildBookWithFiles(CreateBookFile("book.mp3", Quality.MP3))
            };

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_allow_unknown_audio_when_missing_from_profile_but_mp3_allowed()
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = Quality.UnknownAudio;
            _remoteBook.Author.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.MP3, Quality.MP3, Quality.MP3);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_allow_unknown_audio_when_missing_from_profile_and_mp3_not_allowed()
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = Quality.UnknownAudio;
            _remoteBook.Author.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.EPUB);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeFalse();
        }

        private static Book BuildBookWithFiles(params BookFile[] files)
        {
            return new Book
            {
                BookFiles = files.ToList()
            };
        }

        private static BookFile CreateBookFile(string path, Quality quality)
        {
            return new BookFile
            {
                Path = path,
                Quality = new QualityModel(quality, new Revision(version: 1))
            };
        }
    }
}
