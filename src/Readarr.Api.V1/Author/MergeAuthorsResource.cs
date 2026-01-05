namespace Readarr.Api.V1.Author
{
    public class MergeAuthorsResource
    {
        public int WinnerAuthorId { get; set; }
        public int LoserAuthorId { get; set; }
        public bool MoveFiles { get; set; } = true;
    }
}
