using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.MediaFiles.BookImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Specifications
{
    [TestFixture]
    public class CloseBookMatchSpecificationFixture : CoreTest<CloseBookMatchSpecification>
    {
        private LocalEdition _localEdition;

        [SetUp]
        public void Setup()
        {
            _localEdition = new LocalEdition
            {
                Distance = new Distance(),
                NewDownload = true
            };

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.BookImportMinimumMatchPercent)
                .Returns(50);
        }

        private void GivenNewDownload(bool newDownload)
        {
            _localEdition.NewDownload = newDownload;
        }

        private void GivenMinimumMatchPercent(int percent)
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.BookImportMinimumMatchPercent)
                .Returns(percent);
        }

        private void GivenDistance(string key, double value)
        {
            _localEdition.Distance = new Distance();
            _localEdition.Distance.Add(key, value);
        }

        [Test]
        public void should_accept_when_distance_is_zero()
        {
            GivenDistance("book", 0.0);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_distance_is_within_default_threshold()
        {
            // 0.40 normalized distance = 60% match, which is > 50% minimum
            GivenDistance("book", 0.40);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_distance_exceeds_default_threshold()
        {
            // With default 50%, threshold = 0.50
            // A full penalty on book_id (weight 5.0) gives normalized distance = 1.0
            _localEdition.Distance = new Distance();
            _localEdition.Distance.Add("book_id", 1.0);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void default_value_should_preserve_existing_behavior()
        {
            // Default is 50%, which means threshold = 0.50
            // This is the same as the previous hard-coded value
            GivenMinimumMatchPercent(50);
            GivenDistance("book", 0.40);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_stricter_with_higher_percentage()
        {
            // 90% minimum match = threshold of 0.10
            GivenMinimumMatchPercent(90);

            // distance of 0.20 > threshold of 0.10, should reject
            GivenDistance("book", 0.20);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_strict_threshold_when_match_is_good()
        {
            // 90% minimum match = threshold of 0.10
            GivenMinimumMatchPercent(90);

            // distance of 0.05 < threshold of 0.10, should accept
            GivenDistance("book", 0.05);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_looser_with_lower_percentage()
        {
            // 50% minimum match = threshold of 0.50
            GivenMinimumMatchPercent(50);

            // distance of 0.40 < threshold of 0.50, should accept
            GivenDistance("book", 0.40);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void ninety_percent_produces_threshold_of_point_one()
        {
            // 90% minimum match should produce internal threshold of 0.10
            GivenMinimumMatchPercent(90);

            // distance of 0.10 equals threshold of 0.10, should accept (not >)
            GivenDistance("book", 0.10);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void ninety_percent_rejects_just_above_threshold()
        {
            GivenMinimumMatchPercent(90);

            // distance of 0.11 > threshold of 0.10, should reject
            GivenDistance("book", 0.11);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_use_existing_library_path_when_not_new_download()
        {
            GivenNewDownload(false);
            GivenMinimumMatchPercent(50);

            // Uses NormalizedDistanceExcluding for existing library imports
            GivenDistance("book", 0.40);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_existing_library_import_when_above_threshold()
        {
            GivenNewDownload(false);
            GivenMinimumMatchPercent(50);

            _localEdition.Distance = new Distance();
            _localEdition.Distance.Add("book_id", 1.0);

            Subject.IsSatisfiedBy(_localEdition, null).Accepted.Should().BeFalse();
        }
    }
}
