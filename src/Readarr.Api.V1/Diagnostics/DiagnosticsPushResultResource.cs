namespace Readarr.Api.V1.Diagnostics
{
    public class DiagnosticsPushResultResource
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Commit { get; set; }
        public string Folder { get; set; }
    }
}
