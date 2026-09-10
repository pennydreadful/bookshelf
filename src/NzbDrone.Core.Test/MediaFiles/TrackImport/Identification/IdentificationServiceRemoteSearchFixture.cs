using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.BookImport;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Identification
{
    [TestFixture]
    public class IdentificationServiceRemoteSearchFixture : CoreTest<IdentificationService>
    {
        private List<LocalBook> GivenLocalBooks()
        {
            return new List<LocalBook>
            {
                new LocalBook
                {
                    Path = "C:\\book.epub",
                    FileTrackInfo = new ParsedTrackInfo()
                }
            };
        }

        private void GivenNoLocalCandidates()
        {
            Mocker.GetMock<ICandidateService>()
                .Setup(x => x.GetDbCandidatesFromTags(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>(), It.IsAny<bool>()))
                .Returns(new List<CandidateEdition>());
        }

        private void GivenRemoteSearch(bool succeeded)
        {
            Mocker.GetMock<ICandidateService>()
                .Setup(x => x.GetRemoteCandidates(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>()))
                .Callback<LocalEdition, IdentificationOverrides>((edition, _) => edition.RemoteSearchSucceeded = succeeded)
                .Returns(new List<CandidateEdition>());
        }

        private List<LocalEdition> Identify(List<LocalBook> localBooks)
        {
            var config = new ImportDecisionMakerConfig
            {
                SingleRelease = true,
                IncludeExisting = false,
                AddNewAuthors = true
            };

            return Subject.Identify(localBooks, new IdentificationOverrides(), config);
        }

        [Test]
        public void should_stamp_last_remote_search_time_when_search_succeeds_but_no_match()
        {
            GivenNoLocalCandidates();
            GivenRemoteSearch(succeeded: true);

            var localBooks = GivenLocalBooks();

            Identify(localBooks);

            localBooks[0].LastRemoteSearchTime.Should().HaveValue();
        }

        [Test]
        public void should_not_stamp_last_remote_search_time_when_search_fails()
        {
            GivenNoLocalCandidates();
            GivenRemoteSearch(succeeded: false);

            var localBooks = GivenLocalBooks();

            Identify(localBooks);

            localBooks[0].LastRemoteSearchTime.Should().NotHaveValue();
        }
    }
}
