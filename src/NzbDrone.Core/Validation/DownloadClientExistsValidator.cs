using FluentValidation;
using NzbDrone.Core.Download;

namespace NzbDrone.Core.Validation
{
    public class DownloadClientExistsValidator<T> : BookdarrPropertyValidator<T, int>
    {
        private readonly IDownloadClientFactory _downloadClientFactory;

        public DownloadClientExistsValidator(IDownloadClientFactory downloadClientFactory)
        {
            _downloadClientFactory = downloadClientFactory;
        }

        protected override string GetDefaultMessageTemplate(string errorCode) => "Download Client does not exist";

        public override bool IsValid(ValidationContext<T> context, int value)
        {
            if (value == 0)
            {
                return true;
            }

            return _downloadClientFactory.Exists(value);
        }
    }
}
