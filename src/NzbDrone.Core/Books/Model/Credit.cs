using Equ;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Books
{
    public class Credit : MemberwiseEquatable<Credit>, IEmbeddedDocument
    {
        public string Name { get; set; }
        public string Role { get; set; }
    }
}
