using FluentValidation.Validators;

using FluentValidation;

namespace Readarr.Http.Validation
{
    public class RssSyncIntervalValidator : PropertyValidator<object, int>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Must be between 10 and 120 or 0 to disable";

        protected override bool IsValid(ValidationContext<object> context, int value)
        {
            if (value == 0)
            {
                return true;
            }

            return value is >= 10 and <= 120;
        }
    }
}
