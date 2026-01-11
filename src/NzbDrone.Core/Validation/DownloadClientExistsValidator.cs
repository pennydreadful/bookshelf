using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Core.Download;

namespace NzbDrone.Core.Validation
{
    public class DownloadClientExistsValidator : PropertyValidator<object, int>
    {
        private readonly IDownloadClientFactory _downloadClientFactory;

        public DownloadClientExistsValidator(IDownloadClientFactory downloadClientFactory)
        {
            _downloadClientFactory = downloadClientFactory;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Download Client does not exist";

        protected override bool IsValid(ValidationContext<object> context, int value)
        {
            if (value == 0)
            {
                return true;
            }

            return _downloadClientFactory.Exists(value);
        }
    }
}
