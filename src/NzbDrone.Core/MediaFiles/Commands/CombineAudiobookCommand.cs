using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class CombineAudiobookCommand : Command
    {
        public int BookId { get; set; }
        public List<int> BookFileIds { get; set; }
        public bool RenameParts { get; set; } = true;

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
        public override bool IsLongRunning => true;
    }
}
