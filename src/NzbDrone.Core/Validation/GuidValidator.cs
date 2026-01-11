using System;
using FluentValidation;

namespace NzbDrone.Core.Validation
{
    public class GuidValidator<T> : BookdarrPropertyValidator<T, string>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "String is not a valid Guid";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            return Guid.TryParse(value, out _);
        }
    }
}
