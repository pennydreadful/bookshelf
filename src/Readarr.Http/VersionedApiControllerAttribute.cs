using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Readarr.Http
{
    public class VersionedApiControllerAttribute : ApiControllerAttribute, IRouteTemplateProvider, IEnableCorsAttribute
    {
        public const string API_CORS_POLICY = "ApiCorsPolicy";
        public const string CONTROLLER_RESOURCE = "[controller]";

        public VersionedApiControllerAttribute(int version, string resource = CONTROLLER_RESOURCE)
        {
            Resource = resource;
            Template = $"api/v{version}/{resource}";
            PolicyName = API_CORS_POLICY;
        }

        public string Resource { get; }
        public string Template { get; }
        public int? Order => 2;
        public string Name { get; set; }
        public string PolicyName { get; set; }
    }

    public class V1ApiControllerAttribute : VersionedApiControllerAttribute
    {
        public V1ApiControllerAttribute(string resource = "[controller]")
            : base(1, resource)
        {
        }
    }
}
