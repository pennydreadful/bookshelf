using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Validation.Paths
{
    public class FileExistsValidator : BookdarrPropertyValidator<object, string>
    {
        private readonly IDiskProvider _diskProvider;

        public FileExistsValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "File '{file}' does not exist";

        public override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            context.MessageFormatter.AppendArgument("file", value);

            return _diskProvider.FileExists(value);
        }
    }
}
