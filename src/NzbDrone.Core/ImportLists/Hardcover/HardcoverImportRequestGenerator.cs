using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Hardcover
{
    public class HardcoverImportRequestGenerator : IImportListRequestGenerator
    {
        private const string StatusPrefix = "status:";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public HardcoverImportSettings Settings { get; set; }

        public int MaxPages { get; set; } = 1;
        public int PageSize { get; set; } = 200;

        public ImportListPageableRequestChain GetListItems()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            var slugs = new List<string>();
            var statusIds = new List<int>();

            foreach (var id in Settings.ListIds)
            {
                if (id.StartsWith(StatusPrefix))
                {
                    if (int.TryParse(id.Substring(StatusPrefix.Length), out var statusId))
                    {
                        statusIds.Add(statusId);
                    }
                }
                else
                {
                    slugs.Add(id);
                }
            }

            if (slugs.Any())
            {
                pageableRequests.Add(GetSlugRequests(slugs));
            }

            if (statusIds.Any())
            {
                pageableRequests.Add(GetStatusRequests(statusIds));
            }

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetSlugRequests(List<string> slugs)
        {
            var apiKey = NormalizeApiKey(Settings.ApiKey);

            Logger.Info("Hardcover: Fetching books for lists '{0}'", string.Join(", ", slugs));

            var graphQlBody = JsonSerializer.Serialize(new
            {
                query = @"
                    query ListBooks($slugs: [String!]!) { me { lists(where: { slug: { _in: $slugs } } ) { slug name list_books { book { id title contributions { author { id name } } } } } } }
                ",
                variables = new
                {
                    slugs
                }
            });

            yield return new ImportListRequest(BuildRequest(apiKey, graphQlBody));
        }

        private IEnumerable<ImportListRequest> GetStatusRequests(List<int> statusIds)
        {
            var apiKey = NormalizeApiKey(Settings.ApiKey);

            Logger.Info("Hardcover: Fetching books for reading statuses '{0}'", string.Join(", ", statusIds));

            var graphQlBody = JsonSerializer.Serialize(new
            {
                query = @"
                    query UserBooks($statusIds: [Int!]!) { me { user_books(where: { status_id: { _in: $statusIds } }) { book { id title contributions { author { id name } } } } } }
                ",
                variables = new
                {
                    statusIds
                }
            });

            yield return new ImportListRequest(BuildRequest(apiKey, graphQlBody));
        }

        private HttpRequest BuildRequest(string apiKey, string graphQlBody)
        {
            var request = new HttpRequestBuilder($"{Settings.BaseUrl.TrimEnd('/')}/v1/graphql")
                .Post()
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", $"Bearer {apiKey}")
                .SetHeader("X-Api-Key", apiKey)
                .SetHeader("User-Agent", "Readarr (Hardcover Import)")
                .SetHeader("Content-Type", "application/json")
                .KeepAlive()
                .Build();

            request.SetContent(graphQlBody);
            return request;
        }

        private string NormalizeApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
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
}
