using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Validation
{
    public class FolderValidator : PropertyValidator<object, string>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Invalid Path: '{path}'";

        protected override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            context.MessageFormatter.AppendArgument("path", value);

            return value.IsPathValid(PathValidationType.CurrentOs);
        }
    }
}
