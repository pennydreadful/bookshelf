using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NzbDrone.Common.Extensions;

namespace Readarr.Http.Middleware
{
    public class UrlBaseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _urlBase;

        public UrlBaseMiddleware(RequestDelegate next, string urlBase)
        {
            _next = next;
            _urlBase = NormalizeUrlBase(urlBase);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_urlBase.IsNotNullOrWhiteSpace() && context.Request.PathBase.Value.IsNullOrWhiteSpace())
            {
                context.Response.Redirect($"{_urlBase}{context.Request.Path}{context.Request.QueryString}");
                context.Response.StatusCode = 307;

                return;
            }

            await _next(context);
        }

        private static string NormalizeUrlBase(string urlBase)
        {
            if (urlBase.IsNullOrWhiteSpace())
            {
                return null;
            }

            var trimmed = urlBase.Trim();

            if (trimmed.Contains("://") || trimmed.StartsWith("//"))
            {
                return null;
            }

            if (!trimmed.StartsWith("/"))
            {
                trimmed = "/" + trimmed;
            }

            if (trimmed.Length > 1)
            {
                trimmed = trimmed.TrimEnd('/');
            }

            return trimmed;
        }
    }
}
