using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Diagnostics;
using Readarr.Http;

namespace Readarr.Api.V1.Diagnostics
{
    [V1ApiController("diagnostics")]
    public class DiagnosticsController : Controller
    {
        private readonly IDiagnosticsPushService _diagnosticsPushService;

        public DiagnosticsController(IDiagnosticsPushService diagnosticsPushService)
        {
            _diagnosticsPushService = diagnosticsPushService;
        }

        [HttpGet("status")]
        public DiagnosticsStatusResource GetStatus()
        {
            var status = _diagnosticsPushService.GetStatus();

            return new DiagnosticsStatusResource
            {
                IsDevelop = status.IsDevelop,
                HasToken = status.HasToken,
                Repo = status.Repo
            };
        }

        [HttpPost("ui-events")]
        public IActionResult AddUiEvents([FromBody] List<JsonElement> events)
        {
            if (events == null || events.Count == 0)
            {
                return Accepted();
            }

            _diagnosticsPushService.AppendUiEvents(events.Select(e => e.GetRawText()));
            return Accepted();
        }

        [HttpPost("push")]
        public DiagnosticsPushResultResource PushDiagnostics()
        {
            var result = _diagnosticsPushService.PushDiagnostics();

            return new DiagnosticsPushResultResource
            {
                Success = result.Success,
                Message = result.Message,
                Commit = result.Commit,
                Folder = result.Folder
            };
        }
    }
}
