namespace Readarr.Api.V1.Diagnostics
{
    public class DiagnosticsStatusResource
    {
        public bool IsDevelop { get; set; }
        public bool HasToken { get; set; }
        public string Repo { get; set; }
    }
}
