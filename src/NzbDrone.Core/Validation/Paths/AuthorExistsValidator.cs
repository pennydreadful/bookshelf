using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Core.Books;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Validation.Paths
{
    public class AuthorExistsValidator : BookdarrPropertyValidator<object, string>
    {
        private readonly IAuthorService _authorService;

        public AuthorExistsValidator(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "This author has already been added";

        public override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            return _authorService.FindById(value) == null;
        }
    }
}
