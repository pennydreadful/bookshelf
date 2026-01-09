using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.DiskTests
{
    [TestFixture]
    public class DirectoryLookupServiceFixture : TestBase<FileSystemLookupService>
    {
        private const string LOST_FOUND = "lost+found";
        private const string QNAP_THUMB = ".@__thumb";
        private const string SYNOLOGY_RECYCLE = "#recycle";
        private List<IDirectoryInfo> _folders;

        private void SetupFolders(string root)
        {
            var folders = new List<string>
            {
                LOST_FOUND,
                QNAP_THUMB,
                SYNOLOGY_RECYCLE,
                "books",
                "downloads",
                "media",
                "test"
            };

            _folders = folders.Select(f => (DirectoryInfoBase)new DirectoryInfo(Path.Combine(root, f))).ToList<IDirectoryInfo>();
        }

        [Test]
        public void should_not_contain_lost_found_for_root()
        {
            var root = "/".AsOsAgnostic();
            SetupFolders(root);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetDirectoryInfos(It.IsAny<string>()))
                .Returns(_folders);

            Subject.LookupContents(root, false, false).Directories.Should().NotContain(Path.Combine(root, LOST_FOUND));
        }

        [Test]
        public void should_not_contain_qnap_thumb()
        {
            var root = "/".AsOsAgnostic();
            SetupFolders(root);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetDirectoryInfos(It.IsAny<string>()))
                .Returns(_folders);

            Subject.LookupContents(root, false, false).Directories.Should().NotContain(Path.Combine(root, QNAP_THUMB));
        }

        [Test]
        public void should_not_contain_hidden_system_folders_for_root()
        {
            var root = "/".AsOsAgnostic();
            SetupFolders(root);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetDirectoryInfos(It.IsAny<string>()))
                .Returns(_folders);

            var result = Subject.LookupContents(root, false, false);

            result.Directories.Should().HaveCount(_folders.Count - 3);

            result.Directories.Should().NotContain(f => f.Name == LOST_FOUND);
            result.Directories.Should().NotContain(f => f.Name == QNAP_THUMB);
            result.Directories.Should().NotContain(f => f.Name == SYNOLOGY_RECYCLE);
        }
    }
}
