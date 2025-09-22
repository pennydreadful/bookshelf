using System;

namespace NzbDrone.Core.MediaFiles.Azw
{
    [Serializable]
    public class AzwTagException : Exception
    {
        public AzwTagException(string message)
            : base(message)
        {
        }
    }
}
