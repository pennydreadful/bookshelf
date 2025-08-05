using System;
using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IReadarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory Metadata { get; }
    }

    public class ReadarrCloudRequestBuilder : IReadarrCloudRequestBuilder
    {
        public ReadarrCloudRequestBuilder()
        {
            //TODO: Create Update Endpoint
            Services = new HttpRequestBuilder("https://readarr.servarr.com/v1/")
                .CreateFactory();

            var md = Environment.GetEnvironmentVariable("METADATA_URL") ?? "https://api.bookinfo.pro";

            Metadata = new HttpRequestBuilder(md + "/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
