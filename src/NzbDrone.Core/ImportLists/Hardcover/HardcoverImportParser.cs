using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.Exceptions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Hardcover
{
    public class HardcoverImportParser : IParseImportListResponse
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ImportListResponse _importListResponse;

        public IList<ImportListItemInfo> ParseResponse(ImportListResponse importListResponse)
        {
            _importListResponse = importListResponse;

            var items = new List<ImportListItemInfo>();

            if (!PreProcess(_importListResponse))
            {
                return items;
            }

            if (_importListResponse.Content.IsNullOrWhiteSpace())
            {
                Logger.Warn("Hardcover Parser: Response content is empty");
                return items;
            }

            var root = JsonConvert.DeserializeObject<JToken>(_importListResponse.Content);

            var dataToken = GetItemsToken(root);
            var tokenList = dataToken.ToList();

            foreach (var entry in tokenList)
            {
                var item = ParseEntry(entry);

                if (item != null)
                {
                    items.Add(item);
                }
            }

            Logger.Debug("Hardcover: Parsed {0} items from response", items.Count);
            return items;
        }

        private IEnumerable<JToken> GetItemsToken(JToken root)
        {
            if (root == null)
            {
                return Enumerable.Empty<JToken>();
            }

            // GraphQL shape: data.me[].lists[].list_books[].book
            var me = root["data"]?["me"];
            if (me != null && me.Type == JTokenType.Array)
            {
                var books = new List<JToken>();
                foreach (var meItem in me.Children())
                {
                    var lists = meItem["lists"];
                    if (lists != null && lists.Type == JTokenType.Array)
                    {
                        foreach (var list in lists.Children())
                        {
                            var listBooks = list["list_books"];
                            if (listBooks != null && listBooks.Type == JTokenType.Array)
                            {
                                foreach (var listBook in listBooks.Children())
                                {
                                    var book = listBook["book"];
                                    if (book != null)
                                    {
                                        books.Add(book);
                                    }
                                }
                            }
                        }
                    }
                }

                Logger.Debug("Hardcover: Extracted {0} books from response", books.Count);
                return books;
            }

            // Fallback: edges structure
            var edges = root["data"]?["list"]?["list_books"]?["edges"];
            if (edges != null && edges.Type == JTokenType.Array)
            {
                return edges.Children().Select(e => e["node"]);
            }

            // Fallbacks for older/rest responses
            if (root.Type == JTokenType.Array)
            {
                return root.Children();
            }

            return root["data"]?.Children()
                   ?? root["books"]?.Children()
                   ?? root["items"]?.Children()
                   ?? Enumerable.Empty<JToken>();
        }

        private ImportListItemInfo ParseEntry(JToken entry)
        {
            var title = entry.Value<string>("title")
                        ?? entry.Value<string>("name")
                        ?? entry.Value<string>("book_title");

            // Try various author field structures
            var author = entry.Value<string>("author")
                         ?? entry.Value<string>("author_name");

            // Hardcover uses contributions[].author.name
            if (author.IsNullOrWhiteSpace())
            {
                var contributions = entry["contributions"];
                if (contributions != null && contributions.Type == JTokenType.Array)
                {
                    var firstContribution = contributions.Children().FirstOrDefault();
                    author = firstContribution?["author"]?.Value<string>("name");
                }
            }

            // Fallback to authors array
            if (author.IsNullOrWhiteSpace())
            {
                var authorToken = entry["authors"]?.Children().FirstOrDefault();
                author = authorToken?.Value<string>("name")
                          ?? authorToken?.Value<string>("full_name");
            }

            // Get Hardcover book ID
            var bookId = entry.Value<string>("id") ?? entry.Value<int?>("id")?.ToString();

            // Get author ID from contributions
            string authorId = null;
            var contribsToken = entry["contributions"];
            if (contribsToken != null && contribsToken.Type == JTokenType.Array)
            {
                var firstContrib = contribsToken.Children().FirstOrDefault();
                if (firstContrib != null)
                {
                    var authorToken = firstContrib["author"];
                    authorId = authorToken?.Value<string>("id") ?? authorToken?.Value<int?>("id")?.ToString();
                }
            }

            if (title.IsNullOrWhiteSpace() && author.IsNullOrWhiteSpace())
            {
                return null;
            }

            Logger.Trace("Hardcover: Parsed '{0}' by '{1}' (bookId={2}, authorId={3})", title, author, bookId, authorId);

            return new ImportListItemInfo
            {
                Book = title,
                Author = author,
                AuthorGoodreadsId = authorId,
                BookGoodreadsId = bookId
            };
        }

        protected virtual bool PreProcess(ImportListResponse importListResponse)
        {
            if (importListResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ImportListException(importListResponse, "Import List API call resulted in an unexpected StatusCode [{0}]", importListResponse.HttpResponse.StatusCode);
            }

            if (importListResponse.HttpResponse.Headers.ContentType != null &&
                importListResponse.HttpResponse.Headers.ContentType.Contains("text/html"))
            {
                throw new ImportListException(importListResponse, "Import List responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}
