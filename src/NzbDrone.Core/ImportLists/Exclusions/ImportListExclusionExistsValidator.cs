using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Exclusions
{
    public class ImportListExclusionExistsValidator : BookdarrPropertyValidator<object, string>
    {
        private readonly IImportListExclusionService _importListExclusionService;

        public ImportListExclusionExistsValidator(IImportListExclusionService importListExclusionService)
        {
            _importListExclusionService = importListExclusionService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "This exclusion has already been added.";

        public override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            return !_importListExclusionService.All().Exists(s => s.ForeignId == value);
        }
    }
}
