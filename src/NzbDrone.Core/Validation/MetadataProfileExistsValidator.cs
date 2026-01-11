using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.Validation
{
    public class MetadataProfileExistsValidator : PropertyValidator<object, int>
    {
        private readonly IMetadataProfileService _profileService;

        public MetadataProfileExistsValidator(IMetadataProfileService profileService)
        {
            _profileService = profileService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Metadata profile does not exist";

        protected override bool IsValid(ValidationContext<object> context, int value)
        {
            if (value == 0)
            {
                return true;
            }

            return _profileService.Exists(value);
        }
    }
}
