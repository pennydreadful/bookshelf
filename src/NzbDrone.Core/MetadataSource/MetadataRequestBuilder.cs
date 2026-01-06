using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;

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
            var builder = new HttpRequestBuilder(_configService.MetadataSource.TrimEnd("/") + "/{route}");

            var languageHeader = GetUiLanguageTag();
            if (languageHeader.IsNotNullOrWhiteSpace())
            {
                builder.SetHeader("Accept-Language", languageHeader);
            }

            return builder.KeepAlive().CreateFactory();
        }

        private string GetUiLanguageTag()
        {
            var isoLanguage = IsoLanguages.Get((Language)_configService.UILanguage) ?? IsoLanguages.Get(Language.English);
            if (isoLanguage == null || isoLanguage.TwoLetterCode.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (isoLanguage.CountryCode.IsNullOrWhiteSpace())
            {
                return isoLanguage.TwoLetterCode;
            }

            return $"{isoLanguage.TwoLetterCode}-{isoLanguage.CountryCode.ToUpperInvariant()}";
        }
    }
}
