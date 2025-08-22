using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MetadataSource
{
    public interface IMetadataRequestBuilder
    {
        IHttpRequestBuilderFactory GetRequestBuilder();
    }

    public class MetadataRequestBuilder : IMetadataRequestBuilder
    {
        private readonly IConfigService _configService;

        public MetadataRequestBuilder(IConfigService configService)
        {
            _configService = configService;
        }

        public IHttpRequestBuilderFactory GetRequestBuilder()
        {
            return new HttpRequestBuilder(_configService.MetadataSource.TrimEnd("/") + "/{route}").KeepAlive().CreateFactory();
        }
    }
}
