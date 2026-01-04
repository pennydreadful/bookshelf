using System.Collections.Generic;

namespace Readarr.Api.V1.Author
{
    public class AuthorBooksAddResource
    {
        public List<string> ForeignBookIds { get; set; }
        public bool SearchForNewBook { get; set; }
    }
}
