using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using Readarr.Http.Extensions;
using Readarr.Http.Frontend.Mappers;

namespace Readarr.Http.Frontend
{
    [Authorize(Policy="UI")]
    [ApiController]
    public class StaticResourceController : Controller
    {
        private readonly IEnumerable<IMapHttpRequestsToDisk> _requestMappers;
        private readonly Logger _logger;
        private readonly IConfigFileProvider _configFileProvider;

        public StaticResourceController(IEnumerable<IMapHttpRequestsToDisk> requestMappers,
            IConfigFileProvider configFileProvider,
            Logger logger)
        {
            _requestMappers = requestMappers;
            _configFileProvider = configFileProvider;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult LoginPage()
        {
            return MapResource("login");
        }

        [EnableCors("AllowGet")]
        [AllowAnonymous]
        [HttpGet("content/{**path:regex(^(?!/*api/).*)}")]
        public IActionResult IndexContent([FromRoute] string path)
        {
            return MapResource("Content/" + path);
        }

        [AllowAnonymous]
        [HttpGet("")]
        [HttpGet("/{**path:regex(^(?!(api|feed)/).*)}")]
        public IActionResult Index([FromRoute] string path)
        {
            if (!User.Identity.IsAuthenticated && !IsAuthenticationBypassAllowed(HttpContext))
            {
                var returnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
                var loginUrl = $"{_configFileProvider.UrlBase}/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
                return Redirect(loginUrl);
            }

            return MapResource(path);
        }

        private bool IsAuthenticationBypassAllowed(HttpContext context)
        {
            if (_configFileProvider.AuthenticationMethod == AuthenticationType.None)
            {
                return true;
            }

            if (_configFileProvider.AuthenticationRequired == AuthenticationRequiredType.DisabledForLocalAddresses &&
                IPAddress.TryParse(context.GetRemoteIP(), out var ipAddress))
            {
                if (ipAddress.IsLocalAddress() ||
                    (_configFileProvider.TrustCgnatIpAddresses && ipAddress.IsCgnatIpAddress()))
                {
                    return true;
                }
            }

            return false;
        }

        private IActionResult MapResource(string path)
        {
            path = "/" + (path ?? "");

            var mapper = _requestMappers.SingleOrDefault(m => m.CanHandle(path));

            if (mapper != null)
            {
                var result = mapper.GetResponse(path);

                if (result != null)
                {
                    if ((result as FileResult)?.ContentType == "text/html")
                    {
                        Response.Headers.DisableCache();
                    }

                    return result;
                }

                return NotFound();
            }

            _logger.Warn("Couldn't find handler for {0}", path);

            return NotFound();
        }
    }
}
