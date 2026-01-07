using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.MetadataSource.GoogleBooks;
using NzbDrone.Core.Parser;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class BookInfoProxy : IProvideAuthorInfo, IProvideBookInfo, ISearchForNewBook, ISearchForNewAuthor, ISearchForNewEntity, IAuthorExtraMetadataProvider
    {
        private const string GoogleBookPrefix = "gb:";
        private const string GoogleAuthorPrefix = "gba:";
        private const int GoogleBooksMaxResultsPerRequest = 40;
        private const int GoogleBooksAuthorMaxResults = 200;
        private const int GoogleBooksAuthorMinResults = 5;
        private const int AuthorImageCacheDays = 7;
        private const string OpenLibrarySeriesPrefix = "ol-series:";
        private const int OpenLibraryMaxGenres = 25;
        private const int OpenLibraryMaxCovers = 1;
        private const int OpenLibraryAuthorLookupLimit = 5;
        private static readonly JsonSerializerOptions SerializerSettings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            Converters = { new STJUtcConverter() }
        };

        private readonly IHttpClient _httpClient;
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly IGoodreadsSearchProxy _goodreadsSearchProxy;
        private readonly IConfigService _configService;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly Logger _logger;
        private readonly IMetadataRequestBuilder _requestBuilder;
        private readonly IHttpRequestBuilderFactory _googleBooksRequestBuilder;
        private readonly ICached<HashSet<string>> _cache;
        private readonly ICached<AuthorExtraMetadata> _authorExtrasCache;
        private readonly ICached<OpenLibraryBookData> _openLibraryBookCache;
        private readonly CachingService _authorCache;

        public BookInfoProxy(IHttpClient httpClient,
                             ICachedHttpResponseService cachedHttpClient,
                             IGoodreadsSearchProxy goodreadsSearchProxy,
                             IConfigService configService,
                             IAuthorService authorService,
                             IBookService bookService,
                             IEditionService editionService,
                             IMetadataRequestBuilder requestBuilder,
                             Logger logger,
                             ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _cachedHttpClient = cachedHttpClient;
            _goodreadsSearchProxy = goodreadsSearchProxy;
            _configService = configService;
            _authorService = authorService;
            _bookService = bookService;
            _editionService = editionService;
            _requestBuilder = requestBuilder;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _authorExtrasCache = cacheManager.GetCache<AuthorExtraMetadata>(GetType(), "authorImage");
            _openLibraryBookCache = cacheManager.GetCache<OpenLibraryBookData>(GetType(), "openLibraryBook");
            _logger = logger;

            _googleBooksRequestBuilder = new HttpRequestBuilder("https://www.googleapis.com/books/v1/{route}")
                .KeepAlive()
                .CreateFactory();

            _authorCache = new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 })));
            _authorCache.DefaultCachePolicy = new CacheDefaults
            {
                DefaultCacheDurationSeconds = 60
            };
        }

        private bool UseGoogleBooks
        {
            get { return string.Equals(_configService.MetadataProvider, "googlebooks", StringComparison.OrdinalIgnoreCase); }
        }

        private string GetUiLanguageCode()
        {
            var isoLanguage = IsoLanguages.Get((Language)_configService.UILanguage) ?? IsoLanguages.Get(Language.English);
            return isoLanguage?.TwoLetterCode;
        }

        public HashSet<string> GetChangedAuthors(DateTime startTime)
        {
            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                .SetSegment("route", "author/changed")
                .AddQueryParam("since", startTime.ToString("o"))
                .Build();

            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<RecentUpdatesResource>(httpRequest);

            if (httpResponse.Resource == null || httpResponse.Resource.Limited)
            {
                return null;
            }

            return new HashSet<string>(httpResponse.Resource.Ids.Select(x => x.ToString()));
        }

        public Author GetAuthorInfo(string foreignAuthorId, bool useCache = false)
        {
            _logger.Debug("Getting Author details GoodreadsId of {0}", foreignAuthorId);

            try
            {
                if (UseGoogleBooks && TryParseGoogleAuthorId(foreignAuthorId, out var authorName))
                {
                    return GetGoogleAuthorInfo(authorName);
                }

                if (useCache)
                {
                    return PollAuthor(foreignAuthorId);
                }

                return PollAuthorUncached(foreignAuthorId);
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Unexpected error getting author info: {foreignAuthorId}", foreignAuthorId);
                throw;
            }
        }

        public HashSet<string> GetChangedBooks(DateTime startTime)
        {
            return _cache.Get("ChangedBooks", () => GetChangedBooksUncached(startTime), TimeSpan.FromMinutes(30));
        }

        private HashSet<string> GetChangedBooksUncached(DateTime startTime)
        {
            return null;
        }

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string foreignBookId)
        {
            try
            {
                if (UseGoogleBooks && TryParseGoogleBookId(foreignBookId, out var volumeId))
                {
                    return GetGoogleBookInfo(volumeId);
                }

                return PollBook(foreignBookId);
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Unexpected error getting book info: {foreignBookId}", foreignBookId);
                throw;
            }
        }

        public List<object> SearchForNewEntity(string title)
        {
            var books = SearchForNewBook(title, null, false);

            var result = new List<object>();
            foreach (var book in books)
            {
                var author = book.Author.Value;

                if (!result.Contains(author))
                {
                    result.Add(author);
                }

                result.Add(book);
            }

            return result;
        }

        public List<Author> SearchForNewAuthor(string title)
        {
            if (UseGoogleBooks)
            {
                var query = title?.Trim();
                if (query.IsNullOrWhiteSpace())
                {
                    return new List<Author>();
                }

                var googleBooks = SearchGoogleBooks($"inauthor:{query}");

                return googleBooks
                    .Select(x => x.Author.Value)
                    .DistinctBy(x => x.ForeignAuthorId)
                    .ToList();
            }

            var books = SearchForNewBook(title, null);

            return books
                .Select(x => x.Author.Value)
                .DistinctBy(x => x.ForeignAuthorId)
                .ToList();
        }

        public List<Book> SearchForNewBook(string title, string author, bool getAllEditions = true)
        {
            if (UseGoogleBooks)
            {
                var query = title?.Trim() ?? string.Empty;
                if (query.IsNullOrWhiteSpace())
                {
                    return new List<Book>();
                }

                if (author.IsNotNullOrWhiteSpace())
                {
                    query = $"{query} inauthor:{author.Trim()}";
                }

                return SearchGoogleBooks(query);
            }

            var q = title.ToLower().Trim();
            if (author != null)
            {
                q += " " + author;
            }

            try
            {
                var lowerTitle = title.ToLowerInvariant();

                var split = lowerTitle.Split(':');
                var prefix = split[0];

                if (split.Length == 2 && new[] { "author", "work", "edition", "isbn", "asin" }.Contains(prefix))
                {
                    var slug = split[1].Trim();

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace))
                    {
                        return new List<Book>();
                    }

                    if (prefix == "author" || prefix == "work" || prefix == "edition")
                    {
                        var isValid = int.TryParse(slug, out var searchId);
                        if (!isValid)
                        {
                            return new List<Book>();
                        }

                        if (prefix == "author")
                        {
                            return SearchByGoodreadsAuthorId(searchId);
                        }

                        if (prefix == "work")
                        {
                            return SearchByGoodreadsWorkId(searchId);
                        }

                        if (prefix == "edition")
                        {
                            return SearchByGoodreadsBookId(searchId, getAllEditions);
                        }
                    }

                    // to handle isbn / asin
                    q = slug;
                }

                return Search(q, getAllEditions);
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for '{0}' failed. Unable to communicate with Goodreads.", ex, title);
            }
            catch (Exception ex) when (ex is not BookInfoException)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for '{0}' failed. Invalid response received from Goodreads.", ex, title);
            }
        }

        public List<Book> SearchByIsbn(string isbn)
        {
            if (UseGoogleBooks)
            {
                return SearchGoogleBooks($"isbn:{isbn}");
            }

            return Search(isbn, true);
        }

        public List<Book> SearchByAsin(string asin)
        {
            if (UseGoogleBooks)
            {
                return SearchGoogleBooks(asin);
            }

            return Search(asin, true);
        }

        private List<Book> Search(string query, bool getAllEditions)
        {
            List<SearchJsonResource> result;
            try
            {
                result = _goodreadsSearchProxy.Search(query);
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Error searching for {0}", query);
                return new List<Book>();
            }

            var books = new List<Book>();

            if (getAllEditions)
            {
                // Slower but more exhaustive, less intensive on metadata API
                var bookIds = result.Select(x => x.WorkId).ToList();

                var idMap = result.Select(x => new { AuthorId = x.Author.Id, BookId = x.WorkId })
                    .GroupBy(x => x.AuthorId)
                    .ToDictionary(x => x.Key, x => x.Select(i => i.BookId.ToString()).ToList());

                List<Book> authorBooks;
                foreach (var author in idMap.Keys)
                {
                    authorBooks = SearchByGoodreadsAuthorId(author);
                    books.AddRange(authorBooks.Where(b => idMap[author].Contains(b.ForeignBookId)));
                }

                var missingBooks = bookIds.ExceptBy(x => x.ToString(), books, x => x.ForeignBookId, StringComparer.Ordinal).ToList();
                foreach (var book in missingBooks)
                {
                    books.AddRange(SearchByGoodreadsWorkId(book));
                }

                return books;
            }
            else
            {
                // Use sparingly, hits metadata API quite hard
                var ids = result.Select(x => x.BookId).ToList();

                if (ids.Count == 0)
                {
                    return new List<Book>();
                }

                if (ids.Count == 1)
                {
                    return SearchByGoodreadsBookId(ids[0], false);
                }

                try
                {
                    return MapSearchResult(ids);
                }
                catch (HttpException ex)
                {
                    _logger.Warn(ex);
                    throw new BookInfoException("Search for '{0}' failed. Unable to communicate with ReadarrAPI, returning status code: {1}.", ex, query, ex.Response.StatusCode);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Error mapping search results");

                    return new List<Book>();
                }
            }
        }

        private List<Book> SearchByGoodreadsAuthorId(int id)
        {
            try
            {
                var authorId = id.ToString();
                var result = GetAuthorInfo(authorId);
                var books = result.Books.Value;
                var authors = new Dictionary<string, AuthorMetadata> { { authorId, result.Metadata.Value } };

                foreach (var book in books)
                {
                    AddDbIds(authorId, book, authors);
                }

                return books;
            }
            catch (AuthorNotFoundException)
            {
                return new List<Book>();
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Error searching by author id");
                return new List<Book>();
            }
        }

        public List<Book> SearchByGoodreadsWorkId(int id)
        {
            try
            {
                var tuple = GetBookInfo(id.ToString());
                AddDbIds(tuple.Item1, tuple.Item2, tuple.Item3.ToDictionary(x => x.ForeignAuthorId));
                return new List<Book> { tuple.Item2 };
            }
            catch (BookNotFoundException)
            {
                return new List<Book>();
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Error searching by work id");
                return new List<Book>();
            }
        }

        public List<Book> SearchByGoodreadsBookId(int id, bool getAllEditions)
        {
            try
            {
                var book = GetEditionInfo(id, getAllEditions);

                return new List<Book> { book };
            }
            catch (AuthorNotFoundException)
            {
                return new List<Book>();
            }
            catch (BookNotFoundException)
            {
                return new List<Book>();
            }
            catch (EditionNotFoundException)
            {
                return new List<Book>();
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Error searching by book id");
                return new List<Book>();
            }
        }

        private Book GetEditionInfo(int id, bool getAllEditions)
        {
            HttpRequest httpRequest;
            HttpResponse httpResponse;

            while (true)
            {
                httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"book/{id}")
                    .Build();

                httpRequest.SuppressHttpError = true;

                // we expect a redirect
                httpResponse = _httpClient.Get(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    WaitUntilRetry(httpResponse);
                }
                else
                {
                    break;
                }
            }

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new EditionNotFoundException(id.ToString());
            }

            if (!httpResponse.HasHttpRedirect)
            {
                throw new BookInfoException($"Unexpected response from {httpRequest.Url}");
            }

            var location = httpResponse.Headers.GetSingleValue("Location");
            var split = location.Split('/').Reverse().ToList();
            var newId = split[0];
            var type = split[1];

            Book book;
            List<AuthorMetadata> authors;

            if (type == "author")
            {
                var author = PollAuthor(newId);

                book = author.Books.Value.FirstOrDefault(b => b.Editions.Value.Any(e => e.ForeignEditionId == id.ToString()));
                authors = new List<AuthorMetadata> { author.Metadata.Value };
            }
            else if (type == "work")
            {
                var tuple = PollBook(newId);

                book = tuple.Item2;
                authors = tuple.Item3;
            }
            else
            {
                throw new NotImplementedException($"Unexpected response from {httpResponse.Request.Url}");
            }

            if (book == null || book.Editions.Value.All(e => e.ForeignEditionId != id.ToString()))
            {
                throw new EditionNotFoundException(id.ToString());
            }

            if (!getAllEditions)
            {
                var trimmed = new Book();
                trimmed.UseMetadataFrom(book);
                trimmed.Author.Value.Metadata = book.AuthorMetadata.Value;
                trimmed.AuthorMetadata = book.AuthorMetadata.Value;
                trimmed.SeriesLinks = book.SeriesLinks;
                var edition = book.Editions.Value.SingleOrDefault(e => e.ForeignEditionId == id.ToString());
                if (edition != null)
                {
                    edition.Monitored = true;
                }

                trimmed.Editions = new List<Edition> { edition };
                book = trimmed;
            }

            var authorDict = authors.ToDictionary(x => x.ForeignAuthorId);
            AddDbIds(book.AuthorMetadata.Value.ForeignAuthorId, book, authorDict);

            return book;
        }

        private List<Book> MapSearchResult(List<int> ids)
        {
            HttpResponse<BulkBookResource> httpResponse;

            while (true)
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", "book/bulk")
                    .SetHeader("Content-Type", "application/json")
                    .Build();

                httpRequest.SetContent(ids.ToJson());
                httpRequest.ContentSummary = ids.ToJson(Formatting.None);

                httpRequest.AllowAutoRedirect = true;
                httpRequest.SuppressHttpErrorStatusCodes = new[] { HttpStatusCode.TooManyRequests };

                httpResponse = _httpClient.Post<BulkBookResource>(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    WaitUntilRetry(httpResponse);
                }
                else
                {
                    break;
                }
            }

            return MapBulkBook(httpResponse.Resource);
        }

        private List<Book> MapBulkBook(BulkBookResource resource)
        {
            var books = new List<Book>();

            if (resource == null)
            {
                return books;
            }

            var authors = resource.Authors.Select(MapAuthorMetadata).ToDictionary(x => x.ForeignAuthorId, x => x);
            var series = resource.Series.Select(MapSeries).ToList();

            foreach (var work in resource.Works)
            {
                var book = MapBook(work);
                var authorId = work.Books.OrderByDescending(b => b.AverageRating * b.RatingCount).First().Contributors.First().ForeignId.ToString();

                AddDbIds(authorId, book, authors);

                books.Add(book);
            }

            MapSeriesLinks(series, books, resource.Series);

            return books;
        }

        private void AddDbIds(string authorId, Book book, Dictionary<string, AuthorMetadata> authors)
        {
            var dbBook = _bookService.FindById(book.ForeignBookId);
            if (dbBook != null)
            {
                book.UseDbFieldsFrom(dbBook);

                var editions = _editionService.GetEditionsByBook(dbBook.Id).ToDictionary(x => x.ForeignEditionId);

                // If we have any database editions, exactly one will be monitored.
                // So unmonitor all the found editions and let the UseDbFieldsFrom set
                // the monitored status
                foreach (var edition in book.Editions.Value)
                {
                    edition.Monitored = false;
                    if (editions.TryGetValue(edition.ForeignEditionId, out var dbEdition))
                    {
                        edition.UseDbFieldsFrom(dbEdition);
                    }
                }

                // Double check at least one edition is monitored
                if (book.Editions.Value.Any() && !book.Editions.Value.Any(x => x.Monitored))
                {
                    var mostPopular = book.Editions.Value.OrderByDescending(x => x.Ratings.Popularity).First();
                    mostPopular.Monitored = true;
                }
            }

            var author = _authorService.FindById(authorId);

            if (author == null)
            {
                if (!authors.TryGetValue(authorId, out var metadata))
                {
                    throw new BookInfoException(string.Format("Expected author metadata for id [{0}] in book data {1}", authorId, book));
                }

                author = new Author
                {
                    CleanName = Parser.Parser.CleanAuthorName(metadata.Name),
                    Metadata = metadata
                };
            }

            book.Author = author;
            book.AuthorMetadata = author.Metadata.Value;
            book.AuthorMetadataId = author.AuthorMetadataId;
        }

        private Author PollAuthor(string foreignAuthorId)
        {
            return _authorCache.GetOrAdd(foreignAuthorId,
                () => PollAuthorUncached(foreignAuthorId),
                new LazyCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                    ImmediateAbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                    Size = 1,
                    SlidingExpiration = TimeSpan.FromMinutes(1),
                    ExpirationMode = ExpirationMode.ImmediateEviction
                }.RegisterPostEvictionCallback((key, value, reason, state) => _logger.Debug($"Clearing cache for {key} due to {reason}")));
        }

        private Author PollAuthorUncached(string foreignAuthorId)
        {
            AuthorResource resource = null;

            for (var i = 0; i < 60; i++)
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"author/{foreignAuthorId}")
                    .Build();

                httpRequest.AllowAutoRedirect = true;
                httpRequest.SuppressHttpError = true;

                var httpResponse = _cachedHttpClient.Get(httpRequest, false, TimeSpan.FromMinutes(30));

                if (httpResponse.HasHttpError)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        WaitUntilRetry(httpResponse);
                        continue;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new AuthorNotFoundException(foreignAuthorId);
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException(foreignAuthorId);
                    }
                    else
                    {
                        throw new BookInfoException("Unexpected error fetching author data");
                    }
                }

                resource = JsonSerializer.Deserialize<AuthorResource>(httpResponse.Content, SerializerSettings);

                if (resource.Works != null)
                {
                    resource.Works ??= new List<WorkResource>();
                    resource.Series ??= new List<SeriesResource>();
                    break;
                }

                Thread.Sleep(2000);
            }

            if (resource?.Works == null)
            {
                throw new BookInfoException($"Failed to get works for {foreignAuthorId}");
            }

            return MapAuthor(resource);
        }

        private Tuple<string, Book, List<AuthorMetadata>> PollBook(string foreignBookId)
        {
            WorkResource resource = null;

            for (var i = 0; i < 60; i++)
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"work/{foreignBookId}")
                    .Build();

                httpRequest.SuppressHttpError = true;

                // this may redirect to an author
                var httpResponse = _httpClient.Get(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    WaitUntilRetry(httpResponse);
                    continue;
                }

                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new BookNotFoundException(foreignBookId);
                }

                if (httpResponse.HasHttpRedirect)
                {
                    var location = httpResponse.Headers.GetSingleValue("Location");
                    var split = location.Split('/').Reverse().ToList();
                    var newId = split[0];
                    var type = split[1];

                    if (type == "author")
                    {
                        var author = PollAuthor(newId);
                        var authorBook = author.Books.Value.SingleOrDefault(x => x.ForeignBookId == foreignBookId);

                        if (authorBook == null)
                        {
                            throw new BookNotFoundException(foreignBookId);
                        }

                        var authorMetadata = new List<AuthorMetadata> { author.Metadata.Value };

                        return Tuple.Create(author.ForeignAuthorId, authorBook, authorMetadata);
                    }
                    else
                    {
                        throw new NotImplementedException($"Unexpected response from {httpResponse.Request.Url}");
                    }
                }

                if (httpResponse.HasHttpError)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException(foreignBookId);
                    }
                    else
                    {
                        throw new BookInfoException("Unexpected response fetching book data");
                    }
                }

                resource = JsonSerializer.Deserialize<WorkResource>(httpResponse.Content, SerializerSettings);

                if (resource.Books != null)
                {
                    break;
                }

                Thread.Sleep(2000);
            }

            if (resource?.Books == null || resource?.Authors == null || (!resource?.Authors?.Any() ?? false))
            {
                throw new BookInfoException($"Failed to get books for {foreignBookId}");
            }

            var book = MapBook(resource);
            var authorId = GetAuthorId(resource).ToString();
            var metadata = resource.Authors.Select(MapAuthorMetadata).ToList();

            var series = resource.Series.Select(MapSeries).ToList();
            MapSeriesLinks(series, new List<Book> { book }, resource.Series);

            return Tuple.Create(authorId, book, metadata);
        }

        private void WaitUntilRetry(HttpResponse response)
        {
            var seconds = 5;

            if (response.Headers.ContainsKey("Retry-After"))
            {
                var retryAfter = response.Headers["Retry-After"];

                if (!int.TryParse(retryAfter, out seconds))
                {
                    seconds = 5;
                }
            }

            _logger.Info("BookInfo returned 429, backing off for {0}s", seconds);

            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }

        private AuthorMetadata MapAuthorMetadata(AuthorResource resource)
        {
            var metadata = new AuthorMetadata
            {
                ForeignAuthorId = resource.ForeignId.ToString(),
                TitleSlug = resource.ForeignId.ToString(),
                Name = resource.Name.CleanSpaces(),
                Overview = resource.Description,
                Ratings = new Ratings { Votes = resource.RatingCount, Value = (decimal)resource.AverageRating },
                Status = AuthorStatusType.Continuing
            };

            metadata.SortName = metadata.Name.ToLower();
            metadata.NameLastFirst = metadata.Name.ToLastFirst();
            metadata.SortNameLastFirst = metadata.NameLastFirst.ToLower();

            if (resource.ImageUrl.IsNotNullOrWhiteSpace())
            {
                metadata.Images.Add(new MediaCover.MediaCover
                {
                    Url = resource.ImageUrl,
                    CoverType = MediaCoverTypes.Poster
                });
            }

            if (resource.Url.IsNotNullOrWhiteSpace())
            {
                metadata.Links.Add(new Links { Url = resource.Url, Name = "Goodreads" });
            }

            TryAddExternalAuthorImage(metadata);

            return metadata;
        }

        private Author MapAuthor(AuthorResource resource)
        {
            var metadata = MapAuthorMetadata(resource);

            var books = resource.Works
                .Where(x => x.ForeignId > 0 && GetAuthorId(x) == resource.ForeignId)
                .Select(MapBook)
                .ToList();

            books.ForEach(x => x.AuthorMetadata = metadata);

            var series = resource.Series.Select(MapSeries).ToList();

            MapSeriesLinks(series, books, resource.Series);

            var result = new Author
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanAuthorName(metadata.Name),
                Books = books,
                Series = series
            };

            return result;
        }

        private static void MapSeriesLinks(List<Series> series, List<Book> books, List<SeriesResource> resource)
        {
            var bookDict = books.ToDictionary(x => x.ForeignBookId);
            var seriesDict = series.ToDictionary(x => x.ForeignSeriesId);

            foreach (var book in books)
            {
                book.SeriesLinks = new List<SeriesBookLink>();
            }

            // only take series where there are some works
            foreach (var s in resource.Where(x => x.LinkItems.Any()))
            {
                if (seriesDict.TryGetValue(s.ForeignId.ToString(), out var curr))
                {
                    curr.LinkItems = s.LinkItems.Where(x => x.ForeignWorkId != 0 && bookDict.ContainsKey(x.ForeignWorkId.ToString())).Select(l => new SeriesBookLink
                    {
                        Book = bookDict[l.ForeignWorkId.ToString()],
                        Series = curr,
                        IsPrimary = l.Primary,
                        Position = l.PositionInSeries,
                        SeriesPosition = l.SeriesPosition
                    }).ToList();

                    foreach (var l in curr.LinkItems.Value)
                    {
                        l.Book.Value.SeriesLinks.Value.Add(l);
                    }
                }
            }
        }

        private static Series MapSeries(SeriesResource resource)
        {
            var series = new Series
            {
                ForeignSeriesId = resource.ForeignId.ToString(),
                Title = resource.Title,
                Description = resource.Description
            };

            return series;
        }

        private static Book MapBook(WorkResource resource)
        {
            var book = new Book
            {
                ForeignBookId = resource.ForeignId.ToString(),
                Title = resource.Title,
                TitleSlug = resource.ForeignId.ToString(),
                CleanTitle = Parser.Parser.CleanAuthorName(resource.Title),
                ReleaseDate = resource.ReleaseDate,
                Genres = resource.Genres,
                RelatedBooks = resource.RelatedWorks
            };

            book.Links.Add(new Links { Url = resource.Url, Name = "Goodreads Editions" });

            if (resource.Books != null)
            {
                book.Editions = resource.Books.Select(x => MapEdition(x)).ToList();

                // monitor the most popular release
                var mostPopular = book.Editions.Value.MaxBy(x => x.Ratings.Popularity);
                if (mostPopular != null)
                {
                    mostPopular.Monitored = true;

                    // fix work title if missing
                    if (book.Title.IsNullOrWhiteSpace())
                    {
                        book.Title = mostPopular.Title;
                    }
                }
            }
            else
            {
                book.Editions = new List<Edition>();
            }

            // If we are missing the book release date, set as the earliest edition release date
            if (!book.ReleaseDate.HasValue)
            {
                var editionReleases = book.Editions.Value
                    .Where(x => x.ReleaseDate.HasValue && x.ReleaseDate.Value.Month != 1 && x.ReleaseDate.Value.Day != 1)
                    .ToList();

                if (editionReleases.Any())
                {
                    book.ReleaseDate = editionReleases.Min(x => x.ReleaseDate.Value);
                }
                else
                {
                    editionReleases = book.Editions.Value.Where(x => x.ReleaseDate.HasValue).ToList();
                    if (editionReleases.Any())
                    {
                        book.ReleaseDate = editionReleases.Min(x => x.ReleaseDate.Value);
                    }
                }
            }

            Debug.Assert(!book.Editions.Value.Any() || book.Editions.Value.Count(x => x.Monitored) == 1, "one edition monitored");

            book.AnyEditionOk = true;

            var ratingCount = book.Editions.Value.Sum(x => x.Ratings.Votes);

            if (ratingCount > 0)
            {
                book.Ratings = new Ratings
                {
                    Votes = ratingCount,
                    Value = book.Editions.Value.Sum(x => x.Ratings.Votes * x.Ratings.Value) / ratingCount
                };
            }
            else
            {
                book.Ratings = new Ratings { Votes = 0, Value = 0 };
            }

            return book;
        }

        private List<Book> SearchGoogleBooks(string query, int maxResults = 20, int startIndex = 0)
        {
            HttpResponse<GoogleBooksVolumeResponse> response;

            try
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "q", query },
                    { "maxResults", maxResults.ToString() }
                };

                if (startIndex > 0)
                {
                    queryParams["startIndex"] = startIndex.ToString();
                }

                var request = BuildGoogleBooksRequest("volumes", queryParams);

                request.SuppressHttpError = true;

                response = _httpClient.Get<GoogleBooksVolumeResponse>(request);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error searching Google Books for {0}", query);
                return new List<Book>();
            }

            if (response.HasHttpError)
            {
                if (IsGoogleBooksQuotaError(response))
                {
                    throw new NzbDroneClientException(response.StatusCode,
                        "Google Books free tier quota exceeded. Please try again later.");
                }

                _logger.Warn("Google Books returned {0} for query {1}", response.StatusCode, query);
                return new List<Book>();
            }

            if (response.Resource?.Items == null)
            {
                return new List<Book>();
            }

            return response.Resource.Items
                .Select(MapGoogleVolume)
                .Where(x => x != null)
                .ToList();
        }

        private List<Book> SearchGoogleBooksPaged(string query, int maxResults)
        {
            var all = new List<Book>();
            var startIndex = 0;

            while (startIndex < maxResults)
            {
                var pageSize = Math.Min(GoogleBooksMaxResultsPerRequest, maxResults - startIndex);
                var page = SearchGoogleBooks(query, pageSize, startIndex);

                if (!page.Any())
                {
                    break;
                }

                all.AddRange(page);

                if (page.Count < pageSize)
                {
                    break;
                }

                startIndex += pageSize;
            }

            return all
                .DistinctBy(x => x.ForeignBookId)
                .ToList();
        }

        private Tuple<string, Book, List<AuthorMetadata>> GetGoogleBookInfo(string volumeId)
        {
            var volume = GetGoogleVolume(volumeId);
            if (volume == null)
            {
                throw new BookNotFoundException(volumeId);
            }

            var book = MapGoogleVolume(volume);
            if (book?.AuthorMetadata?.Value == null)
            {
                throw new BookNotFoundException(volumeId);
            }

            var authorMetadata = book.AuthorMetadata.Value;
            TryEnrichGoogleBookFromOpenLibrary(book, GetOpenLibraryIsbn(volume.VolumeInfo?.IndustryIdentifiers));
            return Tuple.Create(authorMetadata.ForeignAuthorId, book, new List<AuthorMetadata> { authorMetadata });
        }

        private Author GetGoogleAuthorInfo(string authorName)
        {
            var authorId = BuildGoogleAuthorId(authorName);
            var metadata = BuildGoogleAuthorMetadata(authorId, authorName);
            TryAddExternalAuthorImage(metadata);
            var books = SearchGoogleBooksAuthor(authorName);

            var author = new Author
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanAuthorName(authorName),
                Books = books,
                Series = new List<Series>()
            };

            foreach (var book in books)
            {
                book.Author = author;
                book.AuthorMetadata = metadata;
            }

            TryEnrichGoogleAuthorBooksFromOpenLibrary(author);

            return author;
        }

        private void TryEnrichGoogleBookFromOpenLibrary(Book book, string isbnOverride = null)
        {
            if (book?.Editions?.Value == null || !book.Editions.Value.Any())
            {
                return;
            }

            var edition = GetPrimaryEdition(book);
            if (edition == null)
            {
                return;
            }

            var isbn = isbnOverride.IsNotNullOrWhiteSpace() ? isbnOverride : edition.Isbn13;
            var data = GetOpenLibraryBookData(isbn);
            if (data == null)
            {
                return;
            }

            ApplyOpenLibraryMetadata(book, edition, data);
        }

        private void TryEnrichGoogleAuthorBooksFromOpenLibrary(Author author)
        {
            var books = author?.Books?.Value;
            if (books == null || !books.Any())
            {
                return;
            }

            var seriesById = new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);
            if (author.Series?.Value != null)
            {
                foreach (var series in author.Series.Value)
                {
                    if (series?.ForeignSeriesId.IsNullOrWhiteSpace() == false)
                    {
                        seriesById[series.ForeignSeriesId] = series;
                    }
                }
            }

            foreach (var book in books)
            {
                var edition = GetPrimaryEdition(book);
                if (edition == null)
                {
                    continue;
                }

                var data = GetOpenLibraryBookData(edition.Isbn13);
                if (data == null)
                {
                    continue;
                }

                ApplyOpenLibraryMetadata(book, edition, data);
                ApplyOpenLibrarySeries(book, author, data, seriesById);
            }

            if (seriesById.Any())
            {
                author.Series = seriesById.Values.ToList();
            }
        }

        private GoogleBooksVolume GetGoogleVolume(string volumeId)
        {
            HttpResponse<GoogleBooksVolume> response;

            try
            {
                var request = BuildGoogleBooksRequest($"volumes/{volumeId}", null);
                request.SuppressHttpError = true;

                response = _httpClient.Get<GoogleBooksVolume>(request);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error fetching Google Books volume {0}", volumeId);
                return null;
            }

            if (response.HasHttpError)
            {
                if (IsGoogleBooksQuotaError(response))
                {
                    throw new NzbDroneClientException(response.StatusCode,
                        "Google Books free tier quota exceeded. Please try again later.");
                }

                return null;
            }

            return response.Resource;
        }

        private Book MapGoogleVolume(GoogleBooksVolume volume)
        {
            if (volume?.VolumeInfo == null || volume.Id.IsNullOrWhiteSpace())
            {
                return null;
            }

            var volumeInfo = volume.VolumeInfo;

            var title = volumeInfo.Title;
            if (title.IsNullOrWhiteSpace())
            {
                title = volumeInfo.Subtitle;
            }

            if (title.IsNullOrWhiteSpace())
            {
                title = "Unknown Title";
            }

            var authorName = volumeInfo.Authors?.FirstOrDefault();
            if (authorName.IsNullOrWhiteSpace())
            {
                authorName = "Unknown Author";
            }

            var authorId = BuildGoogleAuthorId(authorName);
            var authorMetadata = BuildGoogleAuthorMetadata(authorId, authorName);
            TryAddExternalAuthorImage(authorMetadata);
            var bookId = BuildGoogleBookId(volume.Id);

            var edition = new Edition
            {
                ForeignEditionId = bookId,
                TitleSlug = bookId,
                Title = title,
                Language = volumeInfo.Language,
                Overview = volumeInfo.Description ?? string.Empty,
                Format = volumeInfo.PrintType,
                IsEbook = volume.SaleInfo?.IsEbook ?? false,
                Publisher = volumeInfo.Publisher,
                PageCount = volumeInfo.PageCount ?? 0,
                ReleaseDate = ParseGooglePublishedDate(volumeInfo.PublishedDate),
                Isbn13 = GetIndustryIdentifier(volumeInfo.IndustryIdentifiers, "ISBN_13"),
                Asin = GetIndustryIdentifier(volumeInfo.IndustryIdentifiers, "ASIN"),
                Ratings = new Ratings
                {
                    Votes = volumeInfo.RatingsCount ?? 0,
                    Value = volumeInfo.AverageRating ?? 0
                }
            };

            foreach (var image in BuildGoogleImages(volumeInfo.ImageLinks))
            {
                edition.Images.Add(image);
            }

            var link = volumeInfo.InfoLink ?? volumeInfo.PreviewLink;
            if (link.IsNotNullOrWhiteSpace())
            {
                edition.Links.Add(new Links { Url = link, Name = "Google Books" });
            }

            edition.Monitored = true;

            var book = new Book
            {
                ForeignBookId = bookId,
                Title = title,
                TitleSlug = bookId,
                CleanTitle = Parser.Parser.CleanAuthorName(title),
                ReleaseDate = ParseGooglePublishedDate(volumeInfo.PublishedDate),
                Genres = volumeInfo.Categories ?? new List<string>(),
                AnyEditionOk = true,
                Editions = new List<Edition> { edition },
                Author = new Author
                {
                    Metadata = authorMetadata,
                    CleanName = Parser.Parser.CleanAuthorName(authorName)
                },
                AuthorMetadata = authorMetadata
            };

            if (link.IsNotNullOrWhiteSpace())
            {
                book.Links.Add(new Links { Url = link, Name = "Google Books" });
            }

            return book;
        }

        private static Edition GetPrimaryEdition(Book book)
        {
            return book?.Editions?.Value?.FirstOrDefault(x => x.Monitored) ?? book?.Editions?.Value?.FirstOrDefault();
        }

        private void ApplyOpenLibraryMetadata(Book book, Edition edition, OpenLibraryBookData data)
        {
            if (book == null || edition == null || data == null)
            {
                return;
            }

            if (IsPlaceholderTitle(book.Title) && data.WorkTitle.IsNotNullOrWhiteSpace())
            {
                book.Title = data.WorkTitle;
            }

            if (IsPlaceholderTitle(edition.Title) && data.EditionTitle.IsNotNullOrWhiteSpace())
            {
                edition.Title = data.EditionTitle;
            }

            if (!book.ReleaseDate.HasValue)
            {
                book.ReleaseDate = data.FirstPublishDate ?? data.PublishDate;
            }

            if (!edition.ReleaseDate.HasValue)
            {
                edition.ReleaseDate = data.PublishDate ?? data.FirstPublishDate;
            }

            if ((book.Genres == null || !book.Genres.Any()) && data.Subjects?.Any() == true)
            {
                book.Genres = data.Subjects;
            }

            if (edition.Publisher.IsNullOrWhiteSpace() && data.Publisher.IsNotNullOrWhiteSpace())
            {
                edition.Publisher = data.Publisher;
            }

            if (edition.PageCount == 0 && data.PageCount.HasValue)
            {
                edition.PageCount = data.PageCount.Value;
            }

            if (edition.Overview.IsNullOrWhiteSpace() && data.Description.IsNotNullOrWhiteSpace())
            {
                edition.Overview = data.Description;
            }

            if (edition.Language.IsNullOrWhiteSpace() && data.Language.IsNotNullOrWhiteSpace())
            {
                edition.Language = data.Language;
            }

            edition.Images ??= new List<MediaCover.MediaCover>();
            if (!edition.Images.Any())
            {
                foreach (var image in BuildOpenLibraryImages(data))
                {
                    if (edition.Images.Any(x => x.Url.Equals(image.Url, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    edition.Images.Add(image);
                }
            }

            AddOpenLibraryLink(book.Links, data.WorkKey);
            AddOpenLibraryLink(edition.Links, data.EditionKey ?? data.WorkKey);
        }

        private void ApplyOpenLibrarySeries(Book book, Author author, OpenLibraryBookData data, IDictionary<string, Series> seriesById)
        {
            if (book == null || author?.Metadata?.Value == null || data?.Series?.Any() != true)
            {
                return;
            }

            var links = book.SeriesLinks?.Value ?? new List<SeriesBookLink>();
            book.SeriesLinks = links;

            foreach (var seriesTitle in data.Series)
            {
                if (seriesTitle.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var normalizedTitle = seriesTitle.CleanSpaces();
                var seriesId = BuildOpenLibrarySeriesId(author.Metadata.Value.ForeignAuthorId, normalizedTitle);
                if (seriesId.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (!seriesById.TryGetValue(seriesId, out var series))
                {
                    series = new Series
                    {
                        ForeignSeriesId = seriesId,
                        Title = normalizedTitle,
                        ForeignAuthorId = author.Metadata.Value.ForeignAuthorId,
                        LinkItems = new List<SeriesBookLink>()
                    };

                    seriesById[seriesId] = series;
                }

                var seriesLinks = series.LinkItems?.Value ?? new List<SeriesBookLink>();
                series.LinkItems = seriesLinks;

                if (links.Any(l => l.Series?.Value?.ForeignSeriesId == seriesId))
                {
                    continue;
                }

                var link = new SeriesBookLink
                {
                    Book = book,
                    Series = series,
                    IsPrimary = true,
                    Position = null,
                    SeriesPosition = 0
                };

                links.Add(link);
                seriesLinks.Add(link);
            }
        }

        private static string BuildOpenLibrarySeriesId(string authorForeignId, string seriesTitle)
        {
            if (authorForeignId.IsNullOrWhiteSpace() || seriesTitle.IsNullOrWhiteSpace())
            {
                return null;
            }

            var normalized = $"{authorForeignId}:{seriesTitle.Trim().ToLowerInvariant()}";
            return $"{OpenLibrarySeriesPrefix}{Base64UrlEncode(normalized)}";
        }

        private static bool IsPlaceholderTitle(string title)
        {
            return title.IsNullOrWhiteSpace() || title.Equals("Unknown Title", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetOpenLibraryIsbn(List<GoogleBooksIndustryIdentifier> identifiers)
        {
            return GetIndustryIdentifier(identifiers, "ISBN_13") ??
                GetIndustryIdentifier(identifiers, "ISBN_10");
        }

        private OpenLibraryBookData GetOpenLibraryBookData(string isbn)
        {
            var normalizedIsbn = NormalizeOpenLibraryIsbn(isbn);
            if (normalizedIsbn.IsNullOrWhiteSpace())
            {
                return null;
            }

            return _openLibraryBookCache.Get(
                normalizedIsbn,
                () =>
                {
                    try
                    {
                        return LookupOpenLibraryBookData(normalizedIsbn);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(ex, "Open Library lookup failed for ISBN {0}", normalizedIsbn);
                        return null;
                    }
                },
                TimeSpan.FromDays(1));
        }

        private OpenLibraryBookData LookupOpenLibraryBookData(string isbn)
        {
            var request = new HttpRequestBuilder($"https://openlibrary.org/isbn/{isbn}.json")
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get(request);
            if (response.HasHttpError)
            {
                return null;
            }

            var editionJson = JObject.Parse(response.Content);
            var data = new OpenLibraryBookData
            {
                EditionKey = editionJson["key"]?.ToString(),
                EditionTitle = editionJson["title"]?.ToString(),
                Description = GetOpenLibraryDescription(editionJson["description"]),
                PublishDate = ParseOpenLibraryPublishedDate(editionJson["publish_date"]?.ToString()),
                Publisher = GetOpenLibraryPublisher(editionJson["publishers"]),
                PageCount = ParseOpenLibraryPageCount(editionJson["number_of_pages"]),
                Language = ParseOpenLibraryLanguage(editionJson["languages"]),
                EditionCoverIds = ParseOpenLibraryCoverIds(editionJson["covers"]),
                WorkKey = editionJson["works"]?.FirstOrDefault()?["key"]?.ToString()
            };

            if (data.WorkKey.IsNotNullOrWhiteSpace())
            {
                PopulateOpenLibraryWorkData(data);
            }

            data.Subjects = NormalizeOpenLibrarySubjects(data.Subjects);
            data.Series = NormalizeOpenLibrarySeries(data.Series);

            return data;
        }

        private void PopulateOpenLibraryWorkData(OpenLibraryBookData data)
        {
            var request = new HttpRequestBuilder($"https://openlibrary.org{data.WorkKey}.json")
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get(request);
            if (response.HasHttpError)
            {
                return;
            }

            var workJson = JObject.Parse(response.Content);
            data.WorkTitle = workJson["title"]?.ToString();
            if (data.Description.IsNullOrWhiteSpace())
            {
                data.Description = GetOpenLibraryDescription(workJson["description"]);
            }

            data.FirstPublishDate = ParseOpenLibraryPublishedDate(workJson["first_publish_date"]?.ToString());
            data.Subjects = ParseOpenLibraryStringList(workJson["subjects"]);
            data.Series = ParseOpenLibrarySeriesList(workJson["series"]);
            data.WorkCoverIds = ParseOpenLibraryCoverIds(workJson["covers"]);
        }

        private static string NormalizeOpenLibraryIsbn(string isbn)
        {
            if (isbn.IsNullOrWhiteSpace())
            {
                return null;
            }

            var normalized = new string(isbn.Where(c => char.IsDigit(c) || c == 'X' || c == 'x').ToArray());
            if (normalized.Length != 10 && normalized.Length != 13)
            {
                return null;
            }

            return normalized;
        }

        private static string GetOpenLibraryDescription(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            if (token.Type == JTokenType.String)
            {
                return token.ToString();
            }

            if (token.Type == JTokenType.Object)
            {
                return token["value"]?.ToString();
            }

            return null;
        }

        private static string GetOpenLibraryPublisher(JToken token)
        {
            if (token is not JArray publishers)
            {
                return null;
            }

            return publishers.FirstOrDefault()?.ToString();
        }

        private static int? ParseOpenLibraryPageCount(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<int>();
            }

            if (int.TryParse(token.ToString(), out var pages))
            {
                return pages;
            }

            return null;
        }

        private static string ParseOpenLibraryLanguage(JToken token)
        {
            var key = token?.FirstOrDefault()?["key"]?.ToString();
            if (key.IsNullOrWhiteSpace())
            {
                return null;
            }

            return key.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }

        private static DateTime? ParseOpenLibraryPublishedDate(string publishedDate)
        {
            if (publishedDate.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (DateTime.TryParse(publishedDate,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static List<int> ParseOpenLibraryCoverIds(JToken token)
        {
            if (token is not JArray covers)
            {
                return new List<int>();
            }

            var results = new List<int>();
            foreach (var cover in covers)
            {
                if (int.TryParse(cover.ToString(), out var id))
                {
                    results.Add(id);
                }
            }

            return results;
        }

        private static List<string> ParseOpenLibraryStringList(JToken token)
        {
            if (token is not JArray values)
            {
                return new List<string>();
            }

            return values
                .Select(x => x?.ToString())
                .Where(x => x.IsNotNullOrWhiteSpace())
                .ToList();
        }

        private static List<string> ParseOpenLibrarySeriesList(JToken token)
        {
            if (token == null)
            {
                return new List<string>();
            }

            if (token.Type == JTokenType.String)
            {
                return new List<string> { token.ToString() };
            }

            if (token is JArray values)
            {
                return values
                    .Select(x => x?.ToString())
                    .Where(x => x.IsNotNullOrWhiteSpace())
                    .ToList();
            }

            return new List<string>();
        }

        private static List<string> NormalizeOpenLibrarySubjects(IEnumerable<string> subjects)
        {
            if (subjects == null)
            {
                return new List<string>();
            }

            return subjects
                .Select(x => x.CleanSpaces())
                .Where(x => x.IsNotNullOrWhiteSpace())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(OpenLibraryMaxGenres)
                .ToList();
        }

        private static List<string> NormalizeOpenLibrarySeries(IEnumerable<string> series)
        {
            if (series == null)
            {
                return new List<string>();
            }

            return series
                .Select(x => x.CleanSpaces())
                .Where(x => x.IsNotNullOrWhiteSpace())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<MediaCover.MediaCover> BuildOpenLibraryImages(OpenLibraryBookData data)
        {
            var coverIds = data?.EditionCoverIds?.Any() == true ? data.EditionCoverIds : data?.WorkCoverIds;
            if (coverIds?.Any() != true)
            {
                return new List<MediaCover.MediaCover>();
            }

            return coverIds
                .Distinct()
                .Take(OpenLibraryMaxCovers)
                .Select(id => new MediaCover.MediaCover
                {
                    Url = $"https://covers.openlibrary.org/b/id/{id}-L.jpg",
                    CoverType = MediaCoverTypes.Cover
                })
                .ToList();
        }

        private static void AddOpenLibraryLink(List<Links> links, string key)
        {
            if (links == null || key.IsNullOrWhiteSpace())
            {
                return;
            }

            var url = key.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? key
                : $"https://openlibrary.org{key}";

            if (links.Any(x => x.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            links.Add(new Links
            {
                Name = "Open Library",
                Url = url
            });
        }

        private HttpRequest BuildGoogleBooksRequest(string route, Dictionary<string, string> queryParams)
        {
            var builder = _googleBooksRequestBuilder.Create()
                .SetSegment("route", route);

            if (_configService.GoogleBooksApiKey.IsNotNullOrWhiteSpace())
            {
                builder.AddQueryParam("key", _configService.GoogleBooksApiKey);
            }

            if (queryParams != null)
            {
                foreach (var pair in queryParams)
                {
                    builder.AddQueryParam(pair.Key, pair.Value);
                }
            }

            var languageCode = GetUiLanguageCode();
            if (languageCode.IsNotNullOrWhiteSpace())
            {
                builder.AddQueryParam("hl", languageCode);
                builder.SetHeader("Accept-Language", languageCode);
            }

            return builder.Build();
        }

        private static string BuildGoogleBookId(string volumeId)
        {
            return $"{GoogleBookPrefix}{volumeId}";
        }

        private static bool TryParseGoogleBookId(string foreignBookId, out string volumeId)
        {
            volumeId = null;
            if (foreignBookId.IsNullOrWhiteSpace() || !foreignBookId.StartsWith(GoogleBookPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            volumeId = foreignBookId.Substring(GoogleBookPrefix.Length);
            return volumeId.IsNotNullOrWhiteSpace();
        }

        private static string BuildGoogleAuthorId(string authorName)
        {
            return $"{GoogleAuthorPrefix}{Base64UrlEncode(authorName)}";
        }

        private static bool TryParseGoogleAuthorId(string foreignAuthorId, out string authorName)
        {
            authorName = null;
            if (foreignAuthorId.IsNullOrWhiteSpace() || !foreignAuthorId.StartsWith(GoogleAuthorPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var encoded = foreignAuthorId.Substring(GoogleAuthorPrefix.Length);
            return TryBase64UrlDecode(encoded, out authorName);
        }

        private static AuthorMetadata BuildGoogleAuthorMetadata(string authorId, string authorName)
        {
            var metadata = new AuthorMetadata
            {
                ForeignAuthorId = authorId,
                TitleSlug = authorId,
                Name = authorName.CleanSpaces(),
                Status = AuthorStatusType.Continuing
            };

            metadata.SortName = metadata.Name.ToLowerInvariant();
            metadata.NameLastFirst = metadata.Name.ToLastFirst();
            metadata.SortNameLastFirst = metadata.NameLastFirst.ToLowerInvariant();

            return metadata;
        }

        public AuthorExtraMetadata GetAuthorExtraMetadata(string authorName)
        {
            if (authorName.IsNullOrWhiteSpace())
            {
                return new AuthorExtraMetadata();
            }

            var cacheKey = authorName.CleanSpaces().ToLowerInvariant();
            var result = _authorExtrasCache.Get(cacheKey,
                () => LookupAuthorExtraMetadata(authorName),
                TimeSpan.FromDays(AuthorImageCacheDays));

            return result ?? new AuthorExtraMetadata();
        }

        public AuthorExtraMetadata RefreshAuthorExtraMetadata(string authorName)
        {
            if (authorName.IsNullOrWhiteSpace())
            {
                return new AuthorExtraMetadata();
            }

            var cacheKey = authorName.CleanSpaces().ToLowerInvariant();
            _authorExtrasCache.Remove(cacheKey);

            var result = LookupAuthorExtraMetadata(authorName) ?? new AuthorExtraMetadata();
            _authorExtrasCache.Set(cacheKey, result, TimeSpan.FromDays(AuthorImageCacheDays));

            return result;
        }

        private void TryAddExternalAuthorImage(AuthorMetadata metadata)
        {
            if (metadata == null || metadata.Name.IsNullOrWhiteSpace())
            {
                return;
            }

            var hasPoster = metadata.Images.Any(x => x.CoverType == MediaCoverTypes.Poster && x.Url.IsNotNullOrWhiteSpace());
            var needsOverview = metadata.Overview.IsNullOrWhiteSpace();

            if (hasPoster && !needsOverview)
            {
                return;
            }

            var result = GetAuthorExtraMetadata(metadata.Name);

            if (result == null)
            {
                return;
            }

            if (!hasPoster && result.ImageUrl.IsNotNullOrWhiteSpace())
            {
                metadata.Images.Add(new MediaCover.MediaCover
                {
                    Url = result.ImageUrl,
                    CoverType = MediaCoverTypes.Poster
                });
            }

            if (needsOverview && result.Overview.IsNotNullOrWhiteSpace())
            {
                metadata.Overview = result.Overview;
            }

            if (result.Links != null)
            {
                foreach (var link in result.Links)
                {
                    AddLinkIfMissing(metadata, link.Name, link.Url);
                }
            }
        }

        private static void AddLinkIfMissing(AuthorMetadata metadata, string name, string url)
        {
            if (metadata == null || url.IsNullOrWhiteSpace() || name.IsNullOrWhiteSpace())
            {
                return;
            }

            if (metadata.Links.Any(x => x.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            metadata.Links.Add(new Links
            {
                Name = name,
                Url = url
            });
        }

        private AuthorExtraMetadata LookupAuthorExtraMetadata(string authorName)
        {
            var combined = new AuthorExtraMetadata
            {
                Links = new List<Links>()
            };

            try
            {
                MergeAuthorExtras(combined, TryGetWikidataAuthorExtras(authorName));
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Wikidata author image lookup failed for {0}", authorName);
            }

            try
            {
                MergeAuthorExtras(combined, TryGetOpenLibraryAuthorExtras(authorName));
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Open Library author image lookup failed for {0}", authorName);
            }

            try
            {
                MergeAuthorExtras(combined, TryGetWikipediaAuthorExtrasByName(authorName));
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Wikipedia author lookup failed for {0}", authorName);
            }

            var hasImage = combined.ImageUrl.IsNotNullOrWhiteSpace();
            var hasOverview = combined.Overview.IsNotNullOrWhiteSpace();
            var hasLinks = combined.Links != null && combined.Links.Any();

            return (hasImage || hasOverview || hasLinks) ? combined : new AuthorExtraMetadata();
        }

        private static void MergeAuthorExtras(AuthorExtraMetadata target, AuthorExtraMetadata source)
        {
            if (target == null || source == null)
            {
                return;
            }

            if (target.ImageUrl.IsNullOrWhiteSpace() && source.ImageUrl.IsNotNullOrWhiteSpace())
            {
                target.ImageUrl = source.ImageUrl;
            }

            if (target.Overview.IsNullOrWhiteSpace() && source.Overview.IsNotNullOrWhiteSpace())
            {
                target.Overview = source.Overview;
            }

            if (source.Links != null && source.Links.Any())
            {
                target.Links ??= new List<Links>();

                foreach (var link in source.Links)
                {
                    if (link?.Url.IsNullOrWhiteSpace() ?? true)
                    {
                        continue;
                    }

                    if (target.Links.Any(x => x.Url.Equals(link.Url, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    target.Links.Add(link);
                }
            }
        }

        private AuthorExtraMetadata TryGetWikidataAuthorExtras(string authorName)
        {
            var searchRequest = new HttpRequestBuilder("https://www.wikidata.org/w/api.php")
                .AddQueryParam("action", "wbsearchentities")
                .AddQueryParam("search", authorName)
                .AddQueryParam("language", "en")
                .AddQueryParam("format", "json")
                .AddQueryParam("limit", "1")
                .Build();

            searchRequest.AllowAutoRedirect = true;
            searchRequest.SuppressHttpError = true;

            var searchResponse = _httpClient.Get(searchRequest);
            if (searchResponse.HasHttpError)
            {
                return null;
            }

            var searchJson = JObject.Parse(searchResponse.Content);
            var entityId = searchJson["search"]?.FirstOrDefault()?["id"]?.ToString();
            if (entityId.IsNullOrWhiteSpace())
            {
                return null;
            }

            var entityRequest = new HttpRequestBuilder("https://www.wikidata.org/w/api.php")
                .AddQueryParam("action", "wbgetentities")
                .AddQueryParam("ids", entityId)
                .AddQueryParam("props", "claims|sitelinks")
                .AddQueryParam("format", "json")
                .Build();

            entityRequest.AllowAutoRedirect = true;
            entityRequest.SuppressHttpError = true;

            var entityResponse = _httpClient.Get(entityRequest);
            if (entityResponse.HasHttpError)
            {
                return null;
            }

            var entityJson = JObject.Parse(entityResponse.Content);
            var entity = entityJson["entities"]?[entityId];
            var imageName = entity?["claims"]?["P18"]?.FirstOrDefault()?["mainsnak"]?["datavalue"]?["value"]?.ToString();
            string imageUrl = null;

            var links = new List<Links>
            {
                new Links { Name = "Wikidata", Url = $"https://www.wikidata.org/wiki/{entityId}" }
            };

            if (imageName.IsNotNullOrWhiteSpace())
            {
                var fileName = imageName.Replace(' ', '_');
                var encodedFileName = Uri.EscapeDataString(fileName);
                imageUrl = $"https://commons.wikimedia.org/wiki/Special:FilePath/{encodedFileName}";
                links.Add(new Links { Name = "Wikimedia Commons", Url = $"https://commons.wikimedia.org/wiki/File:{encodedFileName}" });
            }

            var wikiTitle = entity?["sitelinks"]?["enwiki"]?["title"]?.ToString();
            WikipediaSummary summary = null;
            string overview = null;
            var hasWikipediaLink = false;
            if (wikiTitle.IsNotNullOrWhiteSpace())
            {
                summary = TryGetWikipediaSummary(wikiTitle);
                if (summary?.Overview.IsNotNullOrWhiteSpace() == true)
                {
                    overview = summary.Overview;
                }

                var wikipediaUrl = summary?.Url;
                if (wikipediaUrl.IsNullOrWhiteSpace())
                {
                    var encodedTitle = Uri.EscapeDataString(wikiTitle.Replace(' ', '_'));
                    wikipediaUrl = $"https://en.wikipedia.org/wiki/{encodedTitle}";
                }

                links.Add(new Links { Name = "Wikipedia", Url = wikipediaUrl });
                hasWikipediaLink = wikipediaUrl.IsNotNullOrWhiteSpace();
            }

            if (imageUrl.IsNullOrWhiteSpace() && summary?.ImageUrl.IsNotNullOrWhiteSpace() == true)
            {
                imageUrl = summary.ImageUrl;
            }

            if (imageUrl.IsNullOrWhiteSpace() && overview.IsNullOrWhiteSpace() && !hasWikipediaLink)
            {
                return null;
            }

            return new AuthorExtraMetadata
            {
                ImageUrl = imageUrl,
                Overview = overview,
                Links = links
            };
        }

        private AuthorExtraMetadata TryGetOpenLibraryAuthorExtras(string authorName)
        {
            var request = new HttpRequestBuilder("https://openlibrary.org/search/authors.json")
                .AddQueryParam("q", authorName)
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get(request);
            if (response.HasHttpError)
            {
                return null;
            }

            var json = JObject.Parse(response.Content);
            var docs = json["docs"] as JArray;
            if (docs == null)
            {
                return null;
            }

            foreach (var doc in docs.Take(OpenLibraryAuthorLookupLimit))
            {
                var linkKey = NormalizeOpenLibraryAuthorKey(doc["key"]?.ToString());
                if (linkKey.IsNotNullOrWhiteSpace())
                {
                    var byKey = TryGetOpenLibraryAuthorExtrasByKey(linkKey);
                    if (byKey?.ImageUrl.IsNotNullOrWhiteSpace() == true ||
                        byKey?.Overview.IsNotNullOrWhiteSpace() == true)
                    {
                        return byKey;
                    }
                }

                var photoToken = doc["photo_id"] ?? doc["photos"]?.FirstOrDefault();
                var photoId = photoToken?.ToString();

                if (photoId.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var imageUrl = photoId.IsNullOrWhiteSpace() ? null : $"https://covers.openlibrary.org/a/id/{photoId}-L.jpg";
                var links = new List<Links>();

                if (linkKey.IsNotNullOrWhiteSpace())
                {
                    links.Add(new Links { Name = "Open Library", Url = $"https://openlibrary.org{linkKey}" });
                }

                return new AuthorExtraMetadata
                {
                    ImageUrl = imageUrl,
                    Overview = null,
                    Links = links
                };
            }

            return null;
        }

        private AuthorExtraMetadata TryGetOpenLibraryAuthorExtrasByKey(string authorKey)
        {
            authorKey = NormalizeOpenLibraryAuthorKey(authorKey);
            if (authorKey.IsNullOrWhiteSpace())
            {
                return null;
            }

            var request = new HttpRequestBuilder($"https://openlibrary.org{authorKey}.json")
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get(request);
            if (response.HasHttpError)
            {
                return null;
            }

            var json = JObject.Parse(response.Content);
            var overview = ParseOpenLibraryAuthorOverview(json["bio"]);
            var photoToken = json["photo_id"] ?? json["photos"]?.FirstOrDefault();
            var photoId = photoToken?.ToString();
            var imageUrl = photoId.IsNullOrWhiteSpace() ? null : $"https://covers.openlibrary.org/a/id/{photoId}-L.jpg";

            if (imageUrl.IsNullOrWhiteSpace() && overview.IsNullOrWhiteSpace())
            {
                return null;
            }

            var links = new List<Links>
            {
                new Links { Name = "Open Library", Url = $"https://openlibrary.org{authorKey}" }
            };

            return new AuthorExtraMetadata
            {
                ImageUrl = imageUrl,
                Overview = overview,
                Links = links
            };
        }

        private AuthorExtraMetadata TryGetWikipediaAuthorExtrasByName(string authorName)
        {
            var summary = TryGetWikipediaSummary(authorName);
            if (summary == null)
            {
                return null;
            }

            var url = summary.Url;
            if (url.IsNullOrWhiteSpace())
            {
                var encoded = Uri.EscapeDataString(authorName.Replace(' ', '_'));
                url = $"https://en.wikipedia.org/wiki/{encoded}";
            }

            return new AuthorExtraMetadata
            {
                ImageUrl = summary.ImageUrl,
                Overview = summary.Overview,
                Links = new List<Links>
                {
                    new Links { Name = "Wikipedia", Url = url }
                }
            };
        }

        private WikipediaSummary TryGetWikipediaSummary(string wikiTitle)
        {
            if (wikiTitle.IsNullOrWhiteSpace())
            {
                return null;
            }

            var encodedTitle = Uri.EscapeDataString(wikiTitle.Replace(' ', '_'));
            var request = new HttpRequestBuilder($"https://en.wikipedia.org/api/rest_v1/page/summary/{encodedTitle}")
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get(request);
            if (response.HasHttpError)
            {
                return null;
            }

            var json = JObject.Parse(response.Content);
            var type = json["type"]?.ToString();
            if (string.Equals(type, "disambiguation", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var extract = NormalizeOverview(json["extract"]?.ToString());
            var url = json["content_urls"]?["desktop"]?["page"]?.ToString();
            var imageUrl = json["thumbnail"]?["source"]?.ToString();

            if (extract.IsNullOrWhiteSpace() && url.IsNullOrWhiteSpace() && imageUrl.IsNullOrWhiteSpace())
            {
                return null;
            }

            return new WikipediaSummary
            {
                Overview = extract,
                ImageUrl = imageUrl,
                Url = url
            };
        }

        private string TryGetOpenLibraryAuthorOverview(string authorKey)
        {
            authorKey = NormalizeOpenLibraryAuthorKey(authorKey);
            if (authorKey.IsNullOrWhiteSpace())
            {
                return null;
            }

            var request = new HttpRequestBuilder($"https://openlibrary.org{authorKey}.json")
                .Build();

            request.AllowAutoRedirect = true;
            request.SuppressHttpError = true;

            var response = _httpClient.Get(request);
            if (response.HasHttpError)
            {
                return null;
            }

            var json = JObject.Parse(response.Content);
            return ParseOpenLibraryAuthorOverview(json["bio"]);
        }

        private static string ParseOpenLibraryAuthorOverview(JToken bioToken)
        {
            if (bioToken == null)
            {
                return null;
            }

            if (bioToken.Type == JTokenType.String)
            {
                return NormalizeOverview(bioToken.ToString());
            }

            return NormalizeOverview(bioToken["value"]?.ToString());
        }

        private static string NormalizeOpenLibraryAuthorKey(string authorKey)
        {
            if (authorKey.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (authorKey.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(authorKey);
                authorKey = uri.AbsolutePath;
            }

            if (authorKey.StartsWith("/authors/", StringComparison.OrdinalIgnoreCase))
            {
                return authorKey;
            }

            if (authorKey.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return authorKey;
            }

            return $"/authors/{authorKey}";
        }

        private static string NormalizeOverview(string overview)
        {
            if (overview.IsNullOrWhiteSpace())
            {
                return overview;
            }

            var cleaned = overview.CleanSpaces();
            if (cleaned.Length > 600)
            {
                cleaned = cleaned.Substring(0, 600).Trim();
            }

            return cleaned;
        }

        private class OpenLibraryBookData
        {
            public string EditionKey { get; set; }
            public string WorkKey { get; set; }
            public string EditionTitle { get; set; }
            public string WorkTitle { get; set; }
            public string Description { get; set; }
            public DateTime? PublishDate { get; set; }
            public DateTime? FirstPublishDate { get; set; }
            public string Publisher { get; set; }
            public int? PageCount { get; set; }
            public string Language { get; set; }
            public List<string> Subjects { get; set; } = new List<string>();
            public List<string> Series { get; set; } = new List<string>();
            public List<int> EditionCoverIds { get; set; } = new List<int>();
            public List<int> WorkCoverIds { get; set; } = new List<int>();
        }

        private class WikipediaSummary
        {
            public string Overview { get; set; }
            public string ImageUrl { get; set; }
            public string Url { get; set; }
        }

        private static List<MediaCover.MediaCover> BuildGoogleImages(GoogleBooksImageLinks imageLinks)
        {
            var images = new List<MediaCover.MediaCover>();
            var url = imageLinks?.Thumbnail ?? imageLinks?.SmallThumbnail;
            url = NormalizeGoogleImageUrl(url);

            if (url.IsNotNullOrWhiteSpace())
            {
                images.Add(new MediaCover.MediaCover
                {
                    Url = url,
                    CoverType = MediaCoverTypes.Cover
                });
            }

            return images;
        }

        private static string NormalizeGoogleImageUrl(string url)
        {
            if (url.IsNullOrWhiteSpace())
            {
                return url;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + url.Substring("http://".Length);
            }

            return url;
        }

        private List<Book> SearchGoogleBooksAuthor(string authorName)
        {
            var results = new List<Book>();

            foreach (var query in BuildGoogleAuthorQueries(authorName))
            {
                var page = SearchGoogleBooksPaged(query, GoogleBooksAuthorMaxResults);
                results.AddRange(page);

                if (results.Count >= GoogleBooksAuthorMinResults)
                {
                    break;
                }
            }

            return results
                .DistinctBy(x => x.ForeignBookId)
                .ToList();
        }

        private static IEnumerable<string> BuildGoogleAuthorQueries(string authorName)
        {
            if (authorName.IsNullOrWhiteSpace())
            {
                yield break;
            }

            yield return $"inauthor:\"{authorName}\"";
            yield return $"inauthor:{authorName}";

            var normalized = NormalizeGoogleAuthorName(authorName);
            if (normalized.IsNotNullOrWhiteSpace() && !normalized.Equals(authorName, StringComparison.OrdinalIgnoreCase))
            {
                yield return $"inauthor:{normalized}";
            }

            var lastName = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (lastName.IsNotNullOrWhiteSpace() && lastName.Length >= 4)
            {
                yield return $"inauthor:{lastName}";
            }
        }

        private static string NormalizeGoogleAuthorName(string authorName)
        {
            if (authorName.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var normalized = new string(authorName
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());

            return normalized.CleanSpaces();
        }

        private static DateTime? ParseGooglePublishedDate(string publishedDate)
        {
            if (publishedDate.IsNullOrWhiteSpace())
            {
                return null;
            }

            var formats = new[] { "yyyy-MM-dd", "yyyy-MM", "yyyy" };
            if (DateTime.TryParseExact(publishedDate,
                    formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(publishedDate,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string GetIndustryIdentifier(List<GoogleBooksIndustryIdentifier> identifiers, string type)
        {
            return identifiers?.FirstOrDefault(x => string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase))?.Identifier;
        }

        private static bool IsGoogleBooksQuotaError(HttpResponse response)
        {
            return response.StatusCode == HttpStatusCode.TooManyRequests ||
                response.StatusCode == HttpStatusCode.Forbidden;
        }

        private static string Base64UrlEncode(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static bool TryBase64UrlDecode(string value, out string decoded)
        {
            decoded = null;

            if (value.IsNullOrWhiteSpace())
            {
                return false;
            }

            var padded = value.Replace('-', '+').Replace('_', '/');
            var mod = padded.Length % 4;
            if (mod == 2)
            {
                padded += "==";
            }
            else if (mod == 3)
            {
                padded += "=";
            }
            else if (mod != 0)
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(padded);
                decoded = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static Edition MapEdition(BookResource resource)
        {
            var edition = new Edition
            {
                ForeignEditionId = resource.ForeignId.ToString(),
                TitleSlug = resource.ForeignId.ToString(),
                Isbn13 = resource.Isbn13,
                Asin = resource.Asin,
                Title = resource.Title.CleanSpaces(),
                Language = resource.Language,
                Overview = resource.Description,
                Format = resource.Format,
                IsEbook = resource.IsEbook,
                Disambiguation = resource.EditionInformation,
                Publisher = resource.Publisher,
                PageCount = resource.NumPages ?? 0,
                ReleaseDate = resource.ReleaseDate,
                Ratings = new Ratings { Votes = resource.RatingCount, Value = (decimal)resource.AverageRating }
            };

            if (resource.ImageUrl.IsNotNullOrWhiteSpace())
            {
                edition.Images.Add(new MediaCover.MediaCover
                {
                    Url = resource.ImageUrl,
                    CoverType = MediaCoverTypes.Cover
                });
            }

            edition.Links.Add(new Links { Url = resource.Url, Name = "Goodreads Book" });

            return edition;
        }

        private static int GetAuthorId(WorkResource b)
        {
            return b.Books.OrderByDescending(x => x.RatingCount * x.AverageRating).FirstOrDefault(x => x.Contributors.Any())?.Contributors.First().ForeignId ?? 0;
        }
    }
}
