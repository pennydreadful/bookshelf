using FluentValidation;
using NzbDrone.Core.Validation;

namespace Readarr.Http.Validation
{
    public class RssSyncIntervalValidator<T> : BookdarrPropertyValidator<T, int>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Must be between 10 and 120 or 0 to disable";

        public override bool IsValid(ValidationContext<T> context, int value)
        {
            if (value == 0)
            {
                return true;
            }

            return value is >= 10 and <= 120;
        }
    }
}
