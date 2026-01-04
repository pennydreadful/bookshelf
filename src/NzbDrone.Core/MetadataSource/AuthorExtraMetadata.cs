using System.Collections.Generic;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MetadataSource
{
    public class AuthorExtraMetadata
    {
        public string ImageUrl { get; set; }
        public string Overview { get; set; }
        public List<Links> Links { get; set; }
    }

    public interface IAuthorExtraMetadataProvider
    {
        AuthorExtraMetadata GetAuthorExtraMetadata(string authorName);
        AuthorExtraMetadata RefreshAuthorExtraMetadata(string authorName);
    }
}
