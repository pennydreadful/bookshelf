using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Readarr.Http.REST
{
    public class RestResourceBodyBindingConvention : IActionModelConvention
    {
        private static readonly string[] BodyMethods = { "POST", "PUT", "PATCH" };

        public void Apply(ActionModel action)
        {
            var hasBodyMethod = action.Selectors
                .SelectMany(selector => selector.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                    .SelectMany(constraint => constraint.HttpMethods) ?? Enumerable.Empty<string>())
                .Any(method => BodyMethods.Contains(method, StringComparer.OrdinalIgnoreCase));

            if (!hasBodyMethod)
            {
                return;
            }

            foreach (var parameter in action.Parameters)
            {
                if (!typeof(RestResource).IsAssignableFrom(parameter.ParameterType))
                {
                    continue;
                }

                parameter.BindingInfo ??= new BindingInfo();

                if (parameter.BindingInfo.BindingSource == null)
                {
                    parameter.BindingInfo.BindingSource = BindingSource.Body;
                }
            }
        }
    }
}
