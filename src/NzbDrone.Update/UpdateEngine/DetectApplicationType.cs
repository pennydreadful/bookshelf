using NzbDrone.Common.Processes;

namespace NzbDrone.Update.UpdateEngine
{
    public interface IDetectApplicationType
    {
        AppType GetAppType();
    }

    public class DetectApplicationType : IDetectApplicationType
    {
        private readonly IProcessProvider _processProvider;

        public DetectApplicationType(IProcessProvider processProvider)
        {
            _processProvider = processProvider;
        }

        public AppType GetAppType()
        {
            if (_processProvider.Exists(ProcessProvider.READARR_CONSOLE_PROCESS_NAME))
            {
                return AppType.Console;
            }

            return AppType.Normal;
        }
    }
}
