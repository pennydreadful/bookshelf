using NLog;
using NzbDrone.Common.Processes;

namespace NzbDrone.Update.UpdateEngine
{
    public interface ITerminateNzbDrone
    {
        void Terminate(int processId);
    }

    public class TerminateNzbDrone : ITerminateNzbDrone
    {
        private readonly IProcessProvider _processProvider;
        private readonly Logger _logger;

        public TerminateNzbDrone(IProcessProvider processProvider, Logger logger)
        {
            _processProvider = processProvider;
            _logger = logger;
        }

        public void Terminate(int processId)
        {
            _logger.Info("Killing all running processes");

            _processProvider.KillAll(ProcessProvider.READARR_CONSOLE_PROCESS_NAME);
            _processProvider.KillAll(ProcessProvider.READARR_PROCESS_NAME);

            _processProvider.Kill(processId);
        }
    }
}
