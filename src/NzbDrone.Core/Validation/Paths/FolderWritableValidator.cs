using System;
using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation.Paths
{
    public class FolderWritableValidator : PropertyValidator<object, string>
    {
        private readonly IDiskProvider _diskProvider;

        public FolderWritableValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Folder '{path}' is not writable by user '{user}'";

        protected override bool IsValid(ValidationContext<object> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            context.MessageFormatter.AppendArgument("path", value);
            context.MessageFormatter.AppendArgument("user", Environment.UserName);

            return _diskProvider.FolderWritable(value);
        }
    }
}
