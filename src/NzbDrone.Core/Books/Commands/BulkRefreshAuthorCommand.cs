using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Books.Commands
{
    public class BulkRefreshAuthorCommand : Command
    {
        public BulkRefreshAuthorCommand()
        {
        }

        public BulkRefreshAuthorCommand(List<int> authorIds, bool areNewAuthors = false)
        {
            AuthorIds = authorIds;
            AreNewAuthors = areNewAuthors;
            SkipNewBooks = false;
        }

        public List<int> AuthorIds { get; set; }
        public bool AreNewAuthors { get; set; }
        public bool SkipNewBooks { get; set; }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => false;
    }
}
