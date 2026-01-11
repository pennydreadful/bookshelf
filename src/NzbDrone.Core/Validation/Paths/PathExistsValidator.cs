using FluentValidation;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Validation.Paths
{
    public class PathExistsValidator<T> : BookdarrPropertyValidator<T, string>
    {
        private readonly IDiskProvider _diskProvider;

        public PathExistsValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Path '{path}' does not exist";

        public override bool IsValid(ValidationContext<T> context, string value)
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
