// Modified from
// https://raw.githubusercontent.com/Prowlarr/Prowlarr/refs/heads/develop/src/NzbDrone.Core/Indexers/Definitions/MyAnonamouse.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.MyAnonaMouse
{
    public class MyAnonaMouse : HttpIndexerBase<MyAnonaMouseSettings>
    {
        public override string Name => "MyAnonaMouse";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        private IndexerCapabilities Capabilities => SetCapabilities();

        private readonly ICacheManager _cacheManager;

        public MyAnonaMouse(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger, ICacheManager cacheManager)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _cacheManager = cacheManager;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private void UpdateCookiesInternal(IDictionary<string, string> cookies, DateTime? expiry)
        {
            // Cookie updating functionality can be implemented here if needed
            // For now, this is a no-op
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new MyAnonaMouseRequestGenerator(Settings, Capabilities, _logger)
            {
                GetCookies = GetCookiesDictionary,
                CookiesUpdater = UpdateCookiesInternal
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new MyAnonaMouseParser(Settings, Capabilities.Categories, _httpClient, _cacheManager, _logger)
            {
                CookiesUpdater = UpdateCookiesInternal
            };
        }

        private IDictionary<string, string> GetCookiesDictionary()
        {
            return new Dictionary<string, string> { { "mam_id", Settings.MamId } };
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            CookiesUpdater?.Invoke(null, null);

            _logger.Debug("Cookies cleared.");

            await base.Test(failures).ConfigureAwait(false);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping("13", NewznabStandardCategory.AudioAudiobook, "AudioBooks");
            caps.Categories.AddCategoryMapping("14", NewznabStandardCategory.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping("15", NewznabStandardCategory.AudioAudiobook, "Musicology");
            caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.AudioAudiobook, "Radio");
            caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("49", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Art");
            caps.Categories.AddCategoryMapping("50", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Biographical");
            caps.Categories.AddCategoryMapping("83", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Business");
            caps.Categories.AddCategoryMapping("51", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("97", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Crafts");
            caps.Categories.AddCategoryMapping("40", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("41", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Fantasy");
            caps.Categories.AddCategoryMapping("106", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Food");
            caps.Categories.AddCategoryMapping("42", NewznabStandardCategory.AudioAudiobook, "Audiobooks - General Fiction");
            caps.Categories.AddCategoryMapping("52", NewznabStandardCategory.AudioAudiobook, "Audiobooks - General Non-Fic");
            caps.Categories.AddCategoryMapping("98", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("54", NewznabStandardCategory.AudioAudiobook, "Audiobooks - History");
            caps.Categories.AddCategoryMapping("55", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Home/Garden");
            caps.Categories.AddCategoryMapping("43", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Horror");
            caps.Categories.AddCategoryMapping("99", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Humor");
            caps.Categories.AddCategoryMapping("84", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Instructional");
            caps.Categories.AddCategoryMapping("44", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Juvenile");
            caps.Categories.AddCategoryMapping("56", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Language");
            caps.Categories.AddCategoryMapping("45", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Literary Classics");
            caps.Categories.AddCategoryMapping("57", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("85", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Medical");
            caps.Categories.AddCategoryMapping("87", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Mystery");
            caps.Categories.AddCategoryMapping("119", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Nature");
            caps.Categories.AddCategoryMapping("88", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Philosophy");
            caps.Categories.AddCategoryMapping("58", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("59", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Recreation");
            caps.Categories.AddCategoryMapping("46", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Romance");
            caps.Categories.AddCategoryMapping("47", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Science Fiction");
            caps.Categories.AddCategoryMapping("53", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Self-Help");
            caps.Categories.AddCategoryMapping("89", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("100", NewznabStandardCategory.AudioAudiobook, "Audiobooks - True Crime");
            caps.Categories.AddCategoryMapping("108", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("48", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Western");
            caps.Categories.AddCategoryMapping("111", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Young Adult");
            caps.Categories.AddCategoryMapping("60", NewznabStandardCategory.BooksEBook, "Ebooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("71", NewznabStandardCategory.BooksEBook, "Ebooks - Art");
            caps.Categories.AddCategoryMapping("72", NewznabStandardCategory.BooksEBook, "Ebooks - Biographical");
            caps.Categories.AddCategoryMapping("90", NewznabStandardCategory.BooksEBook, "Ebooks - Business");
            caps.Categories.AddCategoryMapping("61", NewznabStandardCategory.BooksComics, "Ebooks - Comics/Graphic novels");
            caps.Categories.AddCategoryMapping("73", NewznabStandardCategory.BooksEBook, "Ebooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("101", NewznabStandardCategory.BooksEBook, "Ebooks - Crafts");
            caps.Categories.AddCategoryMapping("62", NewznabStandardCategory.BooksEBook, "Ebooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("63", NewznabStandardCategory.BooksEBook, "Ebooks - Fantasy");
            caps.Categories.AddCategoryMapping("107", NewznabStandardCategory.BooksEBook, "Ebooks - Food");
            caps.Categories.AddCategoryMapping("64", NewznabStandardCategory.BooksEBook, "Ebooks - General Fiction");
            caps.Categories.AddCategoryMapping("74", NewznabStandardCategory.BooksEBook, "Ebooks - General Non-Fiction");
            caps.Categories.AddCategoryMapping("102", NewznabStandardCategory.BooksEBook, "Ebooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("76", NewznabStandardCategory.BooksEBook, "Ebooks - History");
            caps.Categories.AddCategoryMapping("77", NewznabStandardCategory.BooksEBook, "Ebooks - Home/Garden");
            caps.Categories.AddCategoryMapping("65", NewznabStandardCategory.BooksEBook, "Ebooks - Horror");
            caps.Categories.AddCategoryMapping("103", NewznabStandardCategory.BooksEBook, "Ebooks - Humor");
            caps.Categories.AddCategoryMapping("115", NewznabStandardCategory.BooksEBook, "Ebooks - Illusion/Magic");
            caps.Categories.AddCategoryMapping("91", NewznabStandardCategory.BooksEBook, "Ebooks - Instructional");
            caps.Categories.AddCategoryMapping("66", NewznabStandardCategory.BooksEBook, "Ebooks - Juvenile");
            caps.Categories.AddCategoryMapping("78", NewznabStandardCategory.BooksEBook, "Ebooks - Language");
            caps.Categories.AddCategoryMapping("67", NewznabStandardCategory.BooksEBook, "Ebooks - Literary Classics");
            caps.Categories.AddCategoryMapping("79", NewznabStandardCategory.BooksMags, "Ebooks - Magazines/Newspapers");
            caps.Categories.AddCategoryMapping("80", NewznabStandardCategory.BooksTechnical, "Ebooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("92", NewznabStandardCategory.BooksEBook, "Ebooks - Medical");
            caps.Categories.AddCategoryMapping("118", NewznabStandardCategory.BooksEBook, "Ebooks - Mixed Collections");
            caps.Categories.AddCategoryMapping("94", NewznabStandardCategory.BooksEBook, "Ebooks - Mystery");
            caps.Categories.AddCategoryMapping("120", NewznabStandardCategory.BooksEBook, "Ebooks - Nature");
            caps.Categories.AddCategoryMapping("95", NewznabStandardCategory.BooksEBook, "Ebooks - Philosophy");
            caps.Categories.AddCategoryMapping("81", NewznabStandardCategory.BooksEBook, "Ebooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("82", NewznabStandardCategory.BooksEBook, "Ebooks - Recreation");
            caps.Categories.AddCategoryMapping("68", NewznabStandardCategory.BooksEBook, "Ebooks - Romance");
            caps.Categories.AddCategoryMapping("69", NewznabStandardCategory.BooksEBook, "Ebooks - Science Fiction");
            caps.Categories.AddCategoryMapping("75", NewznabStandardCategory.BooksEBook, "Ebooks - Self-Help");
            caps.Categories.AddCategoryMapping("96", NewznabStandardCategory.BooksEBook, "Ebooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("104", NewznabStandardCategory.BooksEBook, "Ebooks - True Crime");
            caps.Categories.AddCategoryMapping("109", NewznabStandardCategory.BooksEBook, "Ebooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("70", NewznabStandardCategory.BooksEBook, "Ebooks - Western");
            caps.Categories.AddCategoryMapping("112", NewznabStandardCategory.BooksEBook, "Ebooks - Young Adult");
            caps.Categories.AddCategoryMapping("19", NewznabStandardCategory.AudioAudiobook, "Guitar/Bass Tabs");
            caps.Categories.AddCategoryMapping("20", NewznabStandardCategory.AudioAudiobook, "Individual Sheet");
            caps.Categories.AddCategoryMapping("24", NewznabStandardCategory.AudioAudiobook, "Individual Sheet MP3");
            caps.Categories.AddCategoryMapping("126", NewznabStandardCategory.AudioAudiobook, "Instructional Book with Video");
            caps.Categories.AddCategoryMapping("22", NewznabStandardCategory.AudioAudiobook, "Instructional Media - Music");
            caps.Categories.AddCategoryMapping("113", NewznabStandardCategory.AudioAudiobook, "Lick Library - LTP/Jam With");
            caps.Categories.AddCategoryMapping("114", NewznabStandardCategory.AudioAudiobook, "Lick Library - Techniques/QL");
            caps.Categories.AddCategoryMapping("17", NewznabStandardCategory.AudioAudiobook, "Music - Complete Editions");
            caps.Categories.AddCategoryMapping("26", NewznabStandardCategory.AudioAudiobook, "Music Book");
            caps.Categories.AddCategoryMapping("27", NewznabStandardCategory.AudioAudiobook, "Music Book MP3");
            caps.Categories.AddCategoryMapping("30", NewznabStandardCategory.AudioAudiobook, "Sheet Collection");
            caps.Categories.AddCategoryMapping("31", NewznabStandardCategory.AudioAudiobook, "Sheet Collection MP3");
            caps.Categories.AddCategoryMapping("127", NewznabStandardCategory.AudioAudiobook, "Radio -  Comedy");
            caps.Categories.AddCategoryMapping("130", NewznabStandardCategory.AudioAudiobook, "Radio - Drama");
            caps.Categories.AddCategoryMapping("128", NewznabStandardCategory.AudioAudiobook, "Radio - Factual/Documentary");
            caps.Categories.AddCategoryMapping("132", NewznabStandardCategory.AudioAudiobook, "Radio - Reading");

            return caps;
        }
    }

    public class MyAnonaMouseParser : IParseIndexerResponse
    {
        private readonly MyAnonaMouseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        private readonly ICached<string> _userClassCache;
        private readonly HashSet<string> _vipFreeleechUserClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "VIP",
            "Elite VIP"
        };

        public MyAnonaMouseParser(MyAnonaMouseSettings settings,
            IndexerCapabilitiesCategories categories,
            IHttpClient httpClient,
            ICacheManager cacheManager,
            Logger logger)
        {
            _settings = settings;
            _categories = categories;
            _httpClient = httpClient;
            _logger = logger;

            _userClassCache = cacheManager.GetCache<string>(GetType());
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var httpResponse = indexerResponse.HttpResponse;

            // Throw auth errors here before we try to parse
            if (httpResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new ApiKeyException("[403 Forbidden] - mam_id expired or invalid");
            }

            // Throw common http errors here before we try to parse
            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response status {httpResponse.StatusCode} code from indexer request");
            }

            if (!httpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                // Remove cookie cache
                CookiesUpdater?.Invoke(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response header {httpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var releaseInfos = new List<ReleaseInfo>();

            var jsonResponse = JsonConvert.DeserializeObject<MyAnonaMouseResponse>(indexerResponse.Content);

            var error = jsonResponse.Error;
            if (error.IsNotNullOrWhiteSpace() && error.StartsWithIgnoreCase("Nothing returned, out of"))
            {
                return releaseInfos.ToArray();
            }

            if (jsonResponse.Data == null)
            {
                throw new IndexerException(indexerResponse, "Unexpected response content from indexer request: {0}", jsonResponse.Message ?? "Check the logs for more information.");
            }

            var hasUserVip = HasUserVip(httpResponse.GetCookies());

            foreach (var item in jsonResponse.Data)
            {
                //TODO shift to ReleaseInfo object initializer for consistency
                var release = new TorrentInfo();

                var id = item.Id;

                release.Title = item.Title;

                if (item.AuthorInfo != null)
                {
                    var authorInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.AuthorInfo);
                    var author = authorInfo?.Take(5).Select(v => v.Value).Join(", ");

                    if (author.IsNotNullOrWhiteSpace())
                    {
                        release.Title += " by " + author;
                        release.Author = author;
                    }
                }

                var flags = new List<string>();

                var languageCode = item.LanguageCode;
                if (!string.IsNullOrEmpty(languageCode))
                {
                    flags.Add(languageCode);
                }

                var filetype = item.Filetype;
                if (!string.IsNullOrEmpty(filetype))
                {
                    flags.Add(filetype.ToUpper());
                }

                if (flags.Count > 0)
                {
                    release.Title += " [" + flags.Join(" / ") + "]";
                }

                if (item.Vip)
                {
                    release.Title += " [VIP]";
                }

                var isFreeLeech = item.Free || item.PersonalFreeLeech || (hasUserVip && item.FreeVip);

                release.DownloadUrl = GetDownloadUrl(id, !isFreeLeech);
                release.InfoUrl = $"{_settings.BaseUrl}t/{id}";
                release.InfoUrl = $"{_settings.BaseUrl}t/{id}";
                release.Guid = release.InfoUrl;
                release.Categories = _categories.MapTrackerCatToNewznab(item.Category).Select(c => c.Id).ToList();
                release.PublishDate = DateTime.ParseExact(item.Added, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                release.Seeders = item.Seeders;
                release.Peers = item.Leechers + release.Seeders;
                release.Size = RssParser.ParseSize(item.Size, true);

                releaseInfos.Add(release);
            }

            // Update cookies with the updated mam_id value received in the response
            CookiesUpdater?.Invoke(httpResponse.GetCookies(), DateTime.Now.AddDays(30));

            return releaseInfos.ToArray();
        }

        private string GetDownloadUrl(int torrentId, bool canUseToken)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/tor/download.php")
                .AddQueryParam("tid", torrentId);

            if (_settings.UseFreeleechWedge is (int)MyAnonaMouseFreeleechWedgeAction.Preferred or (int)MyAnonaMouseFreeleechWedgeAction.Required && canUseToken)
            {
                url = url.AddQueryParam("canUseToken", "true");
            }

            return url.FullUri;
        }

        private bool HasUserVip(Dictionary<string, string> cookies)
        {
            var cacheKey = "myanonamouse_user_class_" + _settings.ToJson().SHA256Hash();

            var userClass = _userClassCache.Get(
                cacheKey,
                () =>
                {
                    var request = new HttpRequestBuilder(_settings.BaseUrl.Trim('/'))
                        .Resource("/jsonLoad.php")
                        .Accept(HttpAccept.Json)
                        .SetCookies(cookies)
                        .Build();

                    _logger.Debug("Fetching user data: {0}", request.Url.FullUri);

                    var response = _httpClient.Execute(request);

                    var jsonResponse = JsonConvert.DeserializeObject<MyAnonaMouseUserDataResponse>(response.Content);

                    _logger.Trace("Current user class: '{0}'", jsonResponse.UserClass);

                    return jsonResponse.UserClass?.Trim();
                },
                TimeSpan.FromHours(1));

            return _vipFreeleechUserClasses.Contains(userClass);
        }
    }

    public enum MyAnonaMouseSearchType
    {
        [FieldOption(Label = "All torrents", Hint = "Search everything")]
        All = 0,

        [FieldOption(Label = "Only active", Hint = "Last update had 1+ seeders")]
        Active = 1,

        [FieldOption(Label = "Freeleech", Hint = "Freeleech torrents")]
        Freeleech = 2,

        [FieldOption(Label = "Freeleech or VIP", Hint = "Freeleech or VIP torrents")]
        FreeleechOrVip = 3,

        [FieldOption(Label = "VIP", Hint = "VIP torrents")]
        Vip = 4,

        [FieldOption(Label = "Not VIP", Hint = "Torrents not VIP")]
        NotVip = 5,
    }

    public enum MyAnonaMouseSearchLanguages
    {
        [FieldOption(Label = "English")]
        English = 1,

        [FieldOption(Label = "Afrikaans")]
        Afrikaans = 17,

        [FieldOption(Label = "Arabic")]
        Arabic = 32,

        [FieldOption(Label = "Bengali")]
        Bengali = 35,

        [FieldOption(Label = "Bosnian")]
        Bosnian = 51,

        [FieldOption(Label = "Bulgarian")]
        Bulgarian = 18,

        [FieldOption(Label = "Burmese")]
        Burmese = 6,

        [FieldOption(Label = "Cantonese")]
        Cantonese = 44,

        [FieldOption(Label = "Catalan")]
        Catalan = 19,

        [FieldOption(Label = "Chinese")]
        Chinese = 2,

        [FieldOption(Label = "Croatian")]
        Croatian = 49,

        [FieldOption(Label = "Czech")]
        Czech = 20,

        [FieldOption(Label = "Danish")]
        Danish = 21,

        [FieldOption(Label = "Dutch")]
        Dutch = 22,

        [FieldOption(Label = "Estonian")]
        Estonian = 61,

        [FieldOption(Label = "Farsi")]
        Farsi = 39,

        [FieldOption(Label = "Finnish")]
        Finnish = 23,

        [FieldOption(Label = "French")]
        French = 36,

        [FieldOption(Label = "German")]
        German = 37,

        [FieldOption(Label = "Greek")]
        Greek = 26,

        [FieldOption(Label = "Greek, Ancient")]
        GreekAncient = 59,

        [FieldOption(Label = "Gujarati")]
        Gujarati = 3,

        [FieldOption(Label = "Hebrew")]
        Hebrew = 27,

        [FieldOption(Label = "Hindi")]
        Hindi = 8,

        [FieldOption(Label = "Hungarian")]
        Hungarian = 28,

        [FieldOption(Label = "Icelandic")]
        Icelandic = 63,

        [FieldOption(Label = "Indonesian")]
        Indonesian = 53,

        [FieldOption(Label = "Irish")]
        Irish = 56,

        [FieldOption(Label = "Italian")]
        Italian = 43,

        [FieldOption(Label = "Japanese")]
        Japanese = 38,

        [FieldOption(Label = "Javanese")]
        Javanese = 12,

        [FieldOption(Label = "Kannada")]
        Kannada = 5,

        [FieldOption(Label = "Korean")]
        Korean = 41,

        [FieldOption(Label = "Lithuanian")]
        Lithuanian = 50,

        [FieldOption(Label = "Latin")]
        Latin = 46,

        [FieldOption(Label = "Latvian")]
        Latvian = 62,

        [FieldOption(Label = "Malay")]
        Malay = 33,

        [FieldOption(Label = "Malayalam")]
        Malayalam = 58,

        [FieldOption(Label = "Manx")]
        Manx = 57,

        [FieldOption(Label = "Marathi")]
        Marathi = 9,

        [FieldOption(Label = "Norwegian")]
        Norwegian = 48,

        [FieldOption(Label = "Polish")]
        Polish = 45,

        [FieldOption(Label = "Portuguese")]
        Portuguese = 34,

        [FieldOption(Label = "Brazilian Portuguese (BP)")]
        BrazilianPortuguese = 52,

        [FieldOption(Label = "Punjabi")]
        Punjabi = 14,

        [FieldOption(Label = "Romanian")]
        Romanian = 30,

        [FieldOption(Label = "Russian")]
        Russian = 16,

        [FieldOption(Label = "Scottish Gaelic")]
        ScottishGaelic = 24,

        [FieldOption(Label = "Sanskrit")]
        Sanskrit = 60,

        [FieldOption(Label = "Serbian")]
        Serbian = 31,

        [FieldOption(Label = "Slovenian")]
        Slovenian = 54,

        [FieldOption(Label = "Spanish")]
        Spanish = 4,

        [FieldOption(Label = "Castilian Spanish")]
        CastilianSpanish = 55,

        [FieldOption(Label = "Swedish")]
        Swedish = 40,

        [FieldOption(Label = "Tagalog")]
        Tagalog = 29,

        [FieldOption(Label = "Tamil")]
        Tamil = 11,

        [FieldOption(Label = "Telugu")]
        Telugu = 10,

        [FieldOption(Label = "Thai")]
        Thai = 7,

        [FieldOption(Label = "Turkish")]
        Turkish = 42,

        [FieldOption(Label = "Ukrainian")]
        Ukrainian = 25,

        [FieldOption(Label = "Urdu")]
        Urdu = 15,

        [FieldOption(Label = "Vietnamese")]
        Vietnamese = 13,

        [FieldOption(Label = "Other")]
        Other = 47,
    }

    public enum MyAnonaMouseFreeleechWedgeAction
    {
        [FieldOption(Label = "Never", Hint = "Do not buy as freeleech")]
        Never = 0,

        [FieldOption(Label = "Preferred", Hint = "Buy and use wedge if possible")]
        Preferred = 1,

        [FieldOption(Label = "Required", Hint = "Abort download if unable to buy wedge")]
        Required = 2,
    }

    public class MyAnonaMouseTorrent
    {
        public int Id { get; set; }
        public string Title { get; set; }
        [JsonProperty(PropertyName = "author_info")]
        public string AuthorInfo { get; set; }
        [JsonProperty(PropertyName = "lang_code")]
        public string LanguageCode { get; set; }
        public string Filetype { get; set; }
        public bool Vip { get; set; }
        public bool Free { get; set; }
        [JsonProperty(PropertyName = "personal_freeleech")]
        public bool PersonalFreeLeech { get; set; }
        [JsonProperty(PropertyName = "fl_vip")]
        public bool FreeVip { get; set; }
        public string Category { get; set; }
        public string Added { get; set; }
        [JsonProperty(PropertyName = "times_completed")]
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public string Size { get; set; }
    }

    public class MyAnonaMouseResponse
    {
        public string Error { get; set; }
        public IReadOnlyCollection<MyAnonaMouseTorrent> Data { get; set; }
        public string Message { get; set; }
    }

    public class MyAnonaMouseUserDataResponse
    {
        [JsonProperty(PropertyName = "classname")]
        public string UserClass { get; set; }
    }
}
