using System.Linq;
using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Validation.Paths
{
    public class AuthorAncestorValidator : BookdarrPropertyValidator<object, string>
    {
        private readonly IAuthorService _authorService;

        public AuthorAncestorValidator(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Path '{path}' is an ancestor of an existing author";

        public override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", value);

            return !_authorService.AllAuthorPaths().Any(s => value.IsParentPath(s.Value));
        }
    }
}
