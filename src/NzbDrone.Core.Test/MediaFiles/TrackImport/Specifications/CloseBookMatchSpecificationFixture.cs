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
        [TestCase(true)]
        [TestCase(false)]
        public void should_apply_configured_minimum_match(bool newDownload)
        {
            var item = new LocalEdition { NewDownload = newDownload, Distance = new Distance() };
            item.Distance.Add("book", 0.40);

            Mocker.GetMock<IConfigService>().SetupGet(x => x.BookImportMinimumMatchPercent).Returns(50);
            Subject.IsSatisfiedBy(item, null).Accepted.Should().BeTrue();

            Mocker.GetMock<IConfigService>().SetupGet(x => x.BookImportMinimumMatchPercent).Returns(90);
            Subject.IsSatisfiedBy(item, null).Accepted.Should().BeFalse();
        }
    }
}
