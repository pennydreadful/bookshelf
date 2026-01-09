using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Hardcover
{
    public interface IHardcoverProxy
    {
        List<HardcoverListResource> GetLists(HardcoverImportSettings settings);
        ValidationFailure Test(HardcoverImportSettings settings);
    }

    public class HardcoverProxy : IHardcoverProxy
    {
        private const string ListQuery = @"{""query"":""query Lists { me { lists { name slug list_books { id } } } }""}";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public HardcoverProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<HardcoverListResource> GetLists(HardcoverImportSettings settings)
        {
            var apiKey = NormalizeApiKey(settings.ApiKey);

            if (apiKey.IsNullOrWhiteSpace())
            {
                _logger.Debug("Hardcover: API key is empty, returning empty list");
                return new List<HardcoverListResource>();
            }

            _logger.Debug("Hardcover: Fetching lists from {0}", settings.BaseUrl);

            var request = BuildGraphQlRequest(settings, ListQuery, apiKey);
            var response = _httpClient.Execute(request);

            if (response.HasHttpError)
            {
                _logger.Warn("Hardcover: HTTP error {0}", response.StatusCode);
                throw new HttpException(request, response);
            }

            var payload = JsonConvert.DeserializeObject<HardcoverGraphQlResponse>(response.Content);
            var lists = payload?.GetLists() ?? new List<HardcoverListResource>();

            _logger.Debug("Hardcover: Found {0} lists", lists.Count);

            return lists;
        }

        public ValidationFailure Test(HardcoverImportSettings settings)
        {
            try
            {
                GetLists(settings);
                _logger.Info("Hardcover authentication succeeded for {0}", settings.BaseUrl);
                return null;
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "Hardcover authentication failed (HTTP {0}) for {1}", ex.Response.StatusCode, settings.BaseUrl);
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized || ex.Response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new ValidationFailure(nameof(settings.ApiKey), "Invalid Hardcover API key");
                }

                return new ValidationFailure(string.Empty, "Unable to connect to Hardcover. Check URL/API key and logs for details.");
            }
            catch (System.Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to Hardcover for {0}", settings.BaseUrl);
                return new ValidationFailure(string.Empty, "Unable to connect to Hardcover. Check logs for details.");
            }
        }

        private HttpRequest BuildGraphQlRequest(HardcoverImportSettings settings, string query, string apiKey)
        {
            var request = new HttpRequestBuilder($"{settings.BaseUrl.TrimEnd('/')}/v1/graphql")
                .Post()
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", $"Bearer {apiKey}")
                .SetHeader("X-Api-Key", apiKey)
                .SetHeader("User-Agent", "Readarr (Hardcover Import)")
                .SetHeader("Content-Type", "application/json")
                .KeepAlive()
                .Build();

            request.SetContent(query);
            return request;
        }

        private string NormalizeApiKey(string apiKey)
        {
            if (apiKey.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var trimmed = apiKey.Trim();
            const string bearerPrefix = "bearer ";

            if (trimmed.StartsWith(bearerPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(bearerPrefix.Length).Trim();
            }

            return trimmed;
        }
    }

    public class HardcoverGraphQlResponse
    {
        [JsonProperty("data")]
        public HardcoverGraphQlData Data { get; set; }

        public List<HardcoverListResource> GetLists() =>
            Data?.Me?
                .SelectMany(m => m.Lists ?? new List<HardcoverListResource>())
                .ToList()
            ?? new List<HardcoverListResource>();
    }

    public class HardcoverGraphQlData
    {
        [JsonProperty("me")]
        public List<HardcoverGraphQlMe> Me { get; set; }
    }

    public class HardcoverGraphQlMe
    {
        [JsonProperty("lists")]
        public List<HardcoverListResource> Lists { get; set; }
    }

    public class HardcoverListResource
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public string DisplayName => Name ?? Slug ?? Id;
    }
}
