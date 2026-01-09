using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.EnsureTest
{
    [TestFixture]
    public class PathExtensionFixture : TestBase
    {
        [TestCase(@"/var/user/file with, comma.mp3")]
        public void EnsureLinuxPath(string path)
        {
            Ensure.That(path, () => path).IsValidPath(PathValidationType.CurrentOs);
        }
    }
}
