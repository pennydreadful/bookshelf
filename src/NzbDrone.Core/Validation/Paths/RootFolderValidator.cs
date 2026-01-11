using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Validation.Paths
{
    public class RootFolderValidator : BookdarrPropertyValidator<object, string>
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderValidator(IRootFolderService rootFolderService)
        {
            _rootFolderService = rootFolderService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Path '{path}' is already configured as a root folder";

        public override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", value);

            return !_rootFolderService.All().Exists(r => r.Path.PathEquals(value));
        }
    }
}
