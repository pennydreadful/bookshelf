using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Validation
{
    public class QualityProfileExistsValidator : PropertyValidator<object, int>
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileExistsValidator(IQualityProfileService qualityProfileService)
        {
            _qualityProfileService = qualityProfileService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Quality Profile does not exist";

        protected override bool IsValid(ValidationContext<object> context, int value)
        {
            if (value == 0)
            {
                return true;
            }

            return _qualityProfileService.Exists(value);
        }
    }
}
