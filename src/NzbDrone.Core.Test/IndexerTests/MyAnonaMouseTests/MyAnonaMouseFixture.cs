using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.MyAnonaMouse;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common.Categories;

namespace NzbDrone.Core.Test.IndexerTests.MyAnonaMouseTests
{
    [IntegrationTest]
    public class MyAnonaMouseFixture : CoreTest<MyAnonaMouse>
    {
        private string _mamId;

        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            _mamId = Environment.GetEnvironmentVariable("MAM_ID");
            if (_mamId.IsNullOrWhiteSpace())
            {
                Assert.Ignore("MAM_ID environment variable not set. Skipping integration test.");
            }

            Subject.Definition = new IndexerDefinition()
            {
                Name = "MyAnonaMouse",
                Settings = new MyAnonaMouseSettings()
                {
                    BaseUrl = "https://www.myanonamouse.net/",
                    MamId = _mamId,
                    MinimumSeeders = 1
                }
            };
        }

        [Test]
        public async Task should_fetch_recent_releases()
        {
            var releases = await Subject.FetchRecent();

            releases.Should().NotBeEmpty();
            releases.Should().OnlyContain(c => c.GetType() == typeof(TorrentInfo));

            var firstRelease = releases.First() as TorrentInfo;
            firstRelease.Title.Should().NotBeNullOrEmpty();
            firstRelease.DownloadUrl.Should().NotBeNullOrEmpty();
            firstRelease.InfoUrl.Should().NotBeNullOrEmpty();
            firstRelease.Size.Should().BeGreaterThan(0);
            firstRelease.PublishDate.Should().BeAfter(DateTime.MinValue);
            firstRelease.Seeders.Should().BeGreaterOrEqualTo(0);
        }

        [Test]
        public async Task should_search_for_book()
        {
            var searchCriteria = new BookSearchCriteria()
            {
                Author = new Books.Author
                {
                    Name = "J.K. Rowling"
                },
                BookTitle = "Harry Potter",
            };

            var releases = await Subject.Fetch(searchCriteria);

            releases.Should().NotBeEmpty();
            releases.Should().OnlyContain(c => c.GetType() == typeof(TorrentInfo));

            var torrentReleases = releases.Cast<TorrentInfo>();
            torrentReleases.Should().OnlyContain(c => c.Title.Contains("Harry Potter", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task should_search_for_author()
        {
            var searchCriteria = new AuthorSearchCriteria()
            {
                Author = new Books.Author
                {
                    Name = "J.K. Rowling"
                }
            };

            var releases = await Subject.Fetch(searchCriteria);

            releases.Should().NotBeEmpty();
            releases.Should().OnlyContain(c => c.GetType() == typeof(TorrentInfo));
        }

        [Test]
        public async Task should_test_connection()
        {
            await Subject.TestConnection();
        }

        [Test]
        public void should_validate_settings()
        {
            var validationResult = Subject.Definition.Settings.Validate();
            validationResult.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_fail_validation_with_empty_mam_id()
        {
            Subject.Definition.Settings = new MyAnonaMouseSettings()
            {
                BaseUrl = "https://www.myanonamouse.net/",
                MamId = ""
            };

            var validationResult = Subject.Definition.Settings.Validate();
            validationResult.IsValid.Should().BeFalse();
            validationResult.Errors.Should().Contain(c => c.PropertyName == "MamId");
        }
    }
}
