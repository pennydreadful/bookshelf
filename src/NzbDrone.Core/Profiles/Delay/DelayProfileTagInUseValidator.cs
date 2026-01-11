using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Profiles.Delay
{
    public class DelayProfileTagInUseValidator<T> : BookdarrPropertyValidator<T, HashSet<int>>
    {
        private readonly IDelayProfileService _delayProfileService;

        public DelayProfileTagInUseValidator(IDelayProfileService delayProfileService)
        {
            _delayProfileService = delayProfileService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "One or more tags is used in another profile";

        public override bool IsValid(ValidationContext<T> context, HashSet<int> value)
        {
            if (value == null || value.Empty())
            {
                return true;
            }

            var instanceToValidate = context.InstanceToValidate;
            if (instanceToValidate == null)
            {
                return true;
            }

            dynamic instance = instanceToValidate;
            var instanceId = (int)instance.Id;

            return _delayProfileService.All().None(d => d.Id != instanceId && d.Tags.Intersect(value).Any());
        }
    }
}
