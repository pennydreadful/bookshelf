using System;
using System.Collections.Generic;
using System.IO;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MediaCover
{
    public interface IMediaCoverProxy
    {
        string RegisterUrl(string url);

        string GetUrl(string hash);
        byte[] GetImage(string hash);
    }

    public class MediaCoverProxy : IMediaCoverProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly ICached<string> _cache;

        public MediaCoverProxy(IHttpClient httpClient, IConfigFileProvider configFileProvider, ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _configFileProvider = configFileProvider;
            _cache = cacheManager.GetCache<string>(GetType());
        }

        public string RegisterUrl(string url)
        {
            if (url.IsNullOrWhiteSpace())
            {
                return null;
            }

            var hash = url.SHA256Hash();

            _cache.Set(hash, url, TimeSpan.FromHours(24));

            _cache.ClearExpired();

            var fileName = Uri.EscapeDataString(GetProxyFileName(url));
            return _configFileProvider.UrlBase + @"/MediaCoverProxy/" + hash + "/" + fileName;
        }

        public string GetUrl(string hash)
        {
            var result = _cache.Find(hash);

            if (result == null)
            {
                throw new KeyNotFoundException("Url no longer in cache");
            }

            return result;
        }

        public byte[] GetImage(string hash)
        {
            var url = GetUrl(hash);

            var request = new HttpRequest(url);

            return _httpClient.Get(request).ResponseData;
        }

        private static string GetProxyFileName(string url)
        {
            string fileName = null;

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                fileName = Path.GetFileName(uri.LocalPath);
            }
            else
            {
                fileName = Path.GetFileName(url);
            }

            if (fileName.IsNullOrWhiteSpace())
            {
                fileName = "cover";
            }

            if (Path.GetExtension(fileName).IsNullOrWhiteSpace())
            {
                fileName += ".jpg";
            }

            return fileName;
        }
    }
}
