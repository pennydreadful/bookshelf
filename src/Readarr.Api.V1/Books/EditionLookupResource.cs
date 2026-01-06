using System;
using NzbDrone.Core.Books;

namespace Readarr.Api.V1.Books
{
    public class EditionLookupResource
    {
        public string ForeignBookId { get; set; }
        public string ForeignEditionId { get; set; }
        public string BookTitle { get; set; }
        public string AuthorName { get; set; }
        public string Title { get; set; }
        public string Disambiguation { get; set; }
        public string Language { get; set; }
        public string Publisher { get; set; }
        public string Isbn13 { get; set; }
        public string Format { get; set; }
        public bool IsEbook { get; set; }
        public int PageCount { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public Ratings Ratings { get; set; }
    }

    public class SelectEditionResource
    {
        public string ForeignBookId { get; set; }
        public string ForeignEditionId { get; set; }
    }
}
