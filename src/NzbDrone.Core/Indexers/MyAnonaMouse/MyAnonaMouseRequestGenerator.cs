// Modified from
// https://raw.githubusercontent.com/Prowlarr/Prowlarr/refs/heads/develop/src/NzbDrone.Core/Indexers/Definitions/MyAnonamouse.cs

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.MyAnonaMouse
{
    public class MyAnonaMouseRequestGenerator : IIndexerRequestGenerator
    {
        private readonly MyAnonaMouseSettings _settings;
        private readonly IndexerCapabilities _capabilities;
        private readonly Logger _logger;

        public MyAnonaMouseRequestGenerator(MyAnonaMouseSettings settings, IndexerCapabilities capabilities, Logger logger)
        {
            _settings = settings;
            _capabilities = capabilities;
            _logger = logger;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(""));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(BookSearchCriteria searchCriteria)
        {
            var query = searchCriteria.AuthorQuery + " " + searchCriteria.BookQuery;
            if (query.IsNullOrWhiteSpace())
            {
                _logger.Info("Search term is empty after being sanitized, stopping search. Initial book search term: '{0}'", query);
                return null;
            }

            return GetPagedRequests(query);
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(AuthorSearchCriteria searchCriteria)
        {
            var query = searchCriteria.AuthorQuery.Trim();
            if (query.IsNullOrWhiteSpace())
            {
                _logger.Info("Search term is empty after being sanitized, stopping search. Initial author search term: '{0}'", query);
                return null;
            }

            return GetPagedRequests(query);
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string query)
        {
            var term = query.Trim();

            var searchType = _settings.SearchType switch
            {
                (int)MyAnonaMouseSearchType.Active => "active",
                (int)MyAnonaMouseSearchType.Freeleech => "fl",
                (int)MyAnonaMouseSearchType.FreeleechOrVip => "fl-VIP",
                (int)MyAnonaMouseSearchType.Vip => "VIP",
                (int)MyAnonaMouseSearchType.NotVip => "nVIP",
                _ => "all"
            };

            var parameters = new NameValueCollection
            {
                { "tor[searchType]", searchType },
                { "tor[srchIn][title]", "true" },
                { "tor[srchIn][author]", "true" },
                { "tor[srchIn][narrator]", "true" },
                { "tor[searchIn]", "torrents" },
                { "tor[sortType]", "default" },
                { "tor[perpage]", "100" },
                { "tor[startNumber]", "0" },
                { "thumbnails", "1" }, // gives links for thumbnail sized versions of their posters
                { "description", "1" } // include the description
            };
            if (!term.IsNullOrWhiteSpace())
            {
                parameters.Set("tor[text]", term);
            }

            if (_settings.SearchInDescription)
            {
                parameters.Set("tor[srchIn][description]", "true");
            }

            if (_settings.SearchInSeries)
            {
                parameters.Set("tor[srchIn][series]", "true");
            }

            if (_settings.SearchInFilenames)
            {
                parameters.Set("tor[srchIn][filenames]", "true");
            }

            if (_settings.SearchLanguages.Count() > 0)
            {
                foreach (var (language, index) in _settings.SearchLanguages.Select((value, index) => (value, index)))
                {
                    parameters.Set($"tor[browse_lang][{index}]", language.ToString());
                }
            }

            var catList = _capabilities.Categories.GetTrackerCategories();

            if (catList.Any())
            {
                foreach (var (category, index) in catList.Select((value, index) => (value, index)))
                {
                    parameters.Set($"tor[cat][{index}]", category);
                }
            }
            else
            {
                parameters.Set("tor[cat][]", "0");
            }

            var searchUrl = _settings.BaseUrl + "tor/js/loadSearchJSONbasic.php";

            if (parameters.Count > 0)
            {
                var queryParts = new List<string>();
                foreach (var key in parameters.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        var values = parameters.GetValues(key);
                        if (values != null)
                        {
                            foreach (var value in values)
                            {
                                queryParts.Add($"{key}={value}");
                            }
                        }
                    }
                }

                searchUrl += "?" + string.Join("&", queryParts);
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            if (GetCookies != null)
            {
                var cookies = GetCookies();
                if (cookies != null && cookies.Any())
                {
                    foreach (var cookie in cookies)
                    {
                        request.HttpRequest.Cookies[cookie.Key] = cookie.Value;
                    }
                }
            }

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(AuthorSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }
    }
}
