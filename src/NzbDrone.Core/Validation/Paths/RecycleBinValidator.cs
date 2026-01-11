using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Validation.Paths
{
    public class RecycleBinValidator<T> : BookdarrPropertyValidator<T, string>
    {
        private readonly IConfigService _configService;

        public RecycleBinValidator(IConfigService configService)
        {
            _configService = configService;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Path '{path}' is {relationship} configured recycle bin folder";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            var recycleBin = _configService.RecycleBin;

            if (value == null || recycleBin.IsNullOrWhiteSpace())
            {
                return true;
            }

            var folder = value;
            context.MessageFormatter.AppendArgument("path", folder);

            if (recycleBin.PathEquals(folder))
            {
                context.MessageFormatter.AppendArgument("relationship", "set to");

                return false;
            }

            if (recycleBin.IsParentPath(folder))
            {
                context.MessageFormatter.AppendArgument("relationship", "child of");

                return false;
            }

            return true;
        }
    }
}
