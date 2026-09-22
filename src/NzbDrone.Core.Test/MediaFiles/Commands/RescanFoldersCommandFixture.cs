using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Commands
{
    [TestFixture]
    public class RescanFoldersCommandFixture : CoreTest
    {
        [Test]
        public void scheduled_scan_should_reconsider_unmatched_files()
        {
            var command = new RescanFoldersCommand();

            command.Filter.Should().Be(FilterFilesType.Matched);
            command.AddNewAuthors.Should().BeTrue();
        }
    }
}
