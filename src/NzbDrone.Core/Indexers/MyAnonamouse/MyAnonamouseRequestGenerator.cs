using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.MyAnonamouse
{
    public class MyAnonamouseRequestGenerator : IIndexerRequestGenerator
    {
        private static readonly Regex SanitizeSearchQueryRegex = new ("[^\\w]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly MyAnonamouseSettings _settings;
        private readonly IndexerCapabilities _capabilities;
        private readonly Logger _logger;

        public MyAnonamouseRequestGenerator(MyAnonamouseSettings settings, IndexerCapabilities capabilities, Logger logger)
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

            pageableRequests.Add(GetPagedRequests(new BookSearchCriteria { SearchTerm = "" }));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria)
        {
            var term = SanitizeSearchQueryRegex.Replace(searchCriteria.SanitizedSearchTerm, " ").Trim();

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && term.IsNullOrWhiteSpace())
            {
                _logger.Debug("Search term is empty after being sanitized, stopping search. Initial search term: '{0}'", searchCriteria.SearchTerm);

                yield break;
            }

            var searchType = _settings.SearchType switch
            {
                (int)MyAnonamouseSearchType.Active => "active",
                (int)MyAnonamouseSearchType.Freeleech => "fl",
                (int)MyAnonamouseSearchType.FreeleechOrVip => "fl-VIP",
                (int)MyAnonamouseSearchType.Vip => "VIP",
                (int)MyAnonamouseSearchType.NotVip => "nVIP",
                _ => "all"
            };

            var parameters = new NameValueCollection
            {
                { "tor[text]", term },
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

            var catList = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories).Distinct().ToList();

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

            if (searchCriteria.MinSize is > 0)
            {
                parameters.Set("tor[minSize]", searchCriteria.MinSize.Value.ToString());
            }

            if (searchCriteria.MaxSize is > 0)
            {
                parameters.Set("tor[maxSize]", searchCriteria.MaxSize.Value.ToString());
            }

            if (searchCriteria.MinSize is > 0 || searchCriteria.MaxSize is > 0)
            {
                parameters.Set("tor[unit]", "1");
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
