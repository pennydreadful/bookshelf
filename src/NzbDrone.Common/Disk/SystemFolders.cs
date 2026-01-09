using System.Collections.Generic;

namespace NzbDrone.Common.Disk
{
    public static class SystemFolders
    {
        public static List<string> GetSystemFolders()
        {
            return new List<string>
            {
                "/bin",
                "/boot",
                "/lib",
                "/sbin",
                "/proc",
                "/usr/bin"
            };
        }
    }
}
