using System;
using FluentValidation;
using FluentValidation.Validators;

namespace NzbDrone.Core.Validation
{
    public class GuidValidator : PropertyValidator<object, string>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "String is not a valid Guid";

        protected override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            return Guid.TryParse(value, out _);
        }
    }
}
