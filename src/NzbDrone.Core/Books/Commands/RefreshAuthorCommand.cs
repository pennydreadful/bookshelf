using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Books.Commands
{
    public class RefreshAuthorCommand : Command
    {
        public int? AuthorId { get; set; }
        public bool IsNewAuthor { get; set; }
        public bool SkipNewBooks { get; set; }

        public RefreshAuthorCommand()
        {
        }

        public RefreshAuthorCommand(int? authorId, bool isNewAuthor = false)
        {
            AuthorId = authorId;
            IsNewAuthor = isNewAuthor;
            SkipNewBooks = false;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => !AuthorId.HasValue;

        public override string CompletionMessage => "Completed";
    }
}
