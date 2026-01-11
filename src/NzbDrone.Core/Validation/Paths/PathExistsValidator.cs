using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation.Paths
{
    public class PathExistsValidator : PropertyValidator<object, string>
    {
        private readonly IDiskProvider _diskProvider;

        public PathExistsValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Path '{path}' does not exist";

        protected override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            context.MessageFormatter.AppendArgument("path", value);

            return _diskProvider.FolderExists(value);
        }
    }
}
