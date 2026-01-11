using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Validation;

namespace Readarr.Api.V1.Profiles.Quality
{
    public static class QualityItemsValidator
    {
        public static IRuleBuilderOptions<T, IList<QualityProfileQualityItemResource>> ValidItems<T>(this IRuleBuilder<T, IList<QualityProfileQualityItemResource>> ruleBuilder)
        {
            ruleBuilder.NotEmpty();
            ruleBuilder.SetValidator(new AllowedValidator<T>());
            ruleBuilder.SetValidator(new QualityNameValidator<T>());
            ruleBuilder.SetValidator(new GroupItemValidator<T>());
            ruleBuilder.SetValidator(new ItemGroupIdValidator<T>());
            ruleBuilder.SetValidator(new UniqueIdValidator<T>());
            ruleBuilder.SetValidator(new UniqueQualityIdValidator<T>());
            ruleBuilder.SetValidator(new AllQualitiesValidator<T>());

            return ruleBuilder.SetValidator(new ItemGroupNameValidator<T>());
        }
    }

    public class AllowedValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Must contain at least one allowed quality";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            return value != null && value.Any(c => c.Allowed);
        }
    }

    public class GroupItemValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Groups must contain multiple qualities";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            if (value == null)
            {
                return false;
            }

            return !value.Any(i => i.Name.IsNotNullOrWhiteSpace() && i.Items.Count <= 1);
        }
    }

    public class QualityNameValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Individual qualities should not be named";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            if (value == null)
            {
                return false;
            }

            return !value.Any(i => i.Name.IsNotNullOrWhiteSpace() && i.Quality != null);
        }
    }

    public class ItemGroupNameValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Groups must have a name";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            if (value == null)
            {
                return false;
            }

            return !value.Any(i => i.Quality == null && i.Name.IsNullOrWhiteSpace());
        }
    }

    public class ItemGroupIdValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Groups must have an ID";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            if (value == null)
            {
                return false;
            }

            return !value.Any(i => i.Quality == null && i.Id == 0);
        }
    }

    public class UniqueIdValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Groups must have a unique ID";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            if (value == null)
            {
                return false;
            }

            var ids = value.Where(i => i.Id > 0).Select(i => i.Id);
            var groupedIds = ids.GroupBy(i => i);

            return groupedIds.All(g => g.Count() == 1);
        }
    }

    public class UniqueQualityIdValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Qualities can only be used once";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            if (value == null)
            {
                return false;
            }

            var qualityIds = new HashSet<int>();

            foreach (var item in value)
            {
                if (item.Id > 0)
                {
                    foreach (var quality in item.Items)
                    {
                        if (qualityIds.Contains(quality.Quality.Id))
                        {
                            return false;
                        }

                        qualityIds.Add(quality.Quality.Id);
                    }
                }
                else
                {
                    if (qualityIds.Contains(item.Quality.Id))
                    {
                        return false;
                    }

                    qualityIds.Add(item.Quality.Id);
                }
            }

            return true;
        }
    }

    public class AllQualitiesValidator<T> : BookdarrPropertyValidator<T, IList<QualityProfileQualityItemResource>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Must contain all qualities";

        public override bool IsValid(ValidationContext<T> context, IList<QualityProfileQualityItemResource> value)
        {
            if (value == null)
            {
                return false;
            }

            var qualityIds = new HashSet<int>();

            foreach (var item in value)
            {
                if (item.Id > 0)
                {
                    foreach (var quality in item.Items)
                    {
                        qualityIds.Add(quality.Quality.Id);
                    }
                }
                else
                {
                    qualityIds.Add(item.Quality.Id);
                }
            }

            var allQualityIds = NzbDrone.Core.Qualities.Quality.All;

            foreach (var quality in allQualityIds)
            {
                if (!qualityIds.Contains(quality.Id))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
