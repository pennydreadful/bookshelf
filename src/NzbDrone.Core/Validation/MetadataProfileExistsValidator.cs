using FluentValidation;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.Validation
{
    public class MetadataProfileExistsValidator<T> : BookdarrPropertyValidator<T, int>
    {
        private readonly IMetadataProfileService _profileService;

        public MetadataProfileExistsValidator(IMetadataProfileService profileService)
        {
            _profileService = profileService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Metadata profile does not exist";

        public override bool IsValid(ValidationContext<T> context, int value)
        {
            if (value == 0)
            {
                return true;
            }

            return _profileService.Exists(value);
        }
    }
}
