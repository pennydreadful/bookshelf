using System.Linq;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Validation.Paths
{
    public class AuthorPathValidator<T> : BookdarrPropertyValidator<T, string>
    {
        private readonly IAuthorService _authorService;

        public AuthorPathValidator(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Path '{path}' is already configured for another author";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", value);

            var instanceToValidate = context.InstanceToValidate;
            if (instanceToValidate == null)
            {
                return true;
            }

            dynamic instance = instanceToValidate;
            var instanceId = (int)instance.Id;

            return !_authorService.AllAuthorPaths().Any(s => s.Value.PathEquals(value) && s.Key != instanceId);
        }
    }
}
