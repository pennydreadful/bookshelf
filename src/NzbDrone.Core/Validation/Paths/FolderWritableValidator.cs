using System;
using FluentValidation;
using NzbDrone.Common.Disk;

namespace NzbDrone.Core.Validation.Paths
{
    public class FolderWritableValidator<T> : BookdarrPropertyValidator<T, string>
    {
        private readonly IDiskProvider _diskProvider;

        public FolderWritableValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Folder '{path}' is not writable by user '{user}'";

        public override bool IsValid(ValidationContext<T> context, string value)
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
