using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Validation.Paths
{
    public class AuthorExistsValidator : PropertyValidator<object, string>
    {
        private readonly IAuthorService _authorService;

        public AuthorExistsValidator(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "This author has already been added";

        protected override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            return _authorService.FindById(value) == null;
        }
    }
}
