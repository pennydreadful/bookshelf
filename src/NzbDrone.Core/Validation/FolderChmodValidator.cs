using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation
{
    public class FolderChmodValidator : PropertyValidator<object, string>
    {
        private readonly IDiskProvider _diskProvider;

        public FolderChmodValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Must contain a valid Unix permissions octal";

        protected override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            return _diskProvider.IsValidFolderPermissionMask(value);
        }
    }
}
