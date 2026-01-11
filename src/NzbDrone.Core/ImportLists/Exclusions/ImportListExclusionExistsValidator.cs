using FluentValidation;
using FluentValidation.Validators;

namespace NzbDrone.Core.ImportLists.Exclusions
{
    public class ImportListExclusionExistsValidator : PropertyValidator<object, string>
    {
        private readonly IImportListExclusionService _importListExclusionService;

        public ImportListExclusionExistsValidator(IImportListExclusionService importListExclusionService)
        {
            _importListExclusionService = importListExclusionService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "This exclusion has already been added.";

        protected override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            return !_importListExclusionService.All().Exists(s => s.ForeignId == value);
        }
    }
}
