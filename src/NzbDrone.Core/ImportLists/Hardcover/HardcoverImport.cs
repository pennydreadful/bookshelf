using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Hardcover
{
    public class HardcoverImport : HttpImportListBase<HardcoverImportSettings>
    {
        private readonly IHardcoverProxy _hardcoverProxy;

        public override string Name => "Hardcover";

        public override ImportListType ListType => ImportListType.Hardcover;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);
        public override int PageSize => 200;

        public override ProviderMessage Message => new ProviderMessage(
            "Books from your Hardcover lists will be matched by title and author name against your configured metadata source.",
            ProviderMessageType.Info);

        public HardcoverImport(IHttpClient httpClient,
                               IImportListStatusService importListStatusService,
                               IConfigService configService,
                               IParsingService parsingService,
                               IHardcoverProxy hardcoverProxy,
                               Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _hardcoverProxy = hardcoverProxy;
        }

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new HardcoverImportRequestGenerator
            {
                Settings = Settings,
                PageSize = PageSize
            };
        }

        public override IParseImportListResponse GetParser()
        {
            return new HardcoverImportParser();
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action.Equals("getLists", StringComparison.OrdinalIgnoreCase))
            {
                // Return empty options if API key is not set yet (user hasn't entered it)
                if (Settings.ApiKey.IsNullOrWhiteSpace())
                {
                    return new { options = new List<object>() };
                }

                var options = _hardcoverProxy.GetLists(Settings)
                    .OrderBy(l => l.DisplayName, StringComparer.InvariantCultureIgnoreCase)
                    .Select(l => new
                    {
                        Value = l.Id ?? l.Slug ?? l.Name,
                        Name = l.DisplayName
                    });

                return new { options };
            }

            if (action.Equals("auth", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Only validate BaseUrl and ApiKey for auth
                    Settings.Validate().Filter("BaseUrl", "ApiKey").ThrowOnError();

                    var options = _hardcoverProxy.GetLists(Settings)
                        .OrderBy(l => l.DisplayName, StringComparer.InvariantCultureIgnoreCase)
                        .Select(l => new
                        {
                            Value = l.Id ?? l.Slug ?? l.Name,
                            Name = l.DisplayName
                        });

                    _logger.Info("Hardcover authentication succeeded for {0}", Settings.BaseUrl);

                    return new
                    {
                        success = true,
                        options
                    };
                }
                catch (HttpException ex)
                {
                    _logger.Warn(ex, "Hardcover authentication failed (HTTP {0}) for {1}", ex.Response.StatusCode, Settings.BaseUrl);
                    return new
                    {
                        success = false,
                        error = $"Authentication failed (HTTP {(int)ex.Response.StatusCode})"
                    };
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Hardcover authentication failed for {0}", Settings.BaseUrl);
                    return new
                    {
                        success = false,
                        error = "Authentication failed. Check API key and logs."
                    };
                }
            }

            return base.RequestAction(action, query);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(_hardcoverProxy.Test(Settings));
        }
    }
}
