using NLog;
using NzbDrone.Common;

namespace NzbDrone.Host
{
    public interface IUtilityModeRouter
    {
        void Route(ApplicationModes applicationModes);
    }

    public class UtilityModeRouter : IUtilityModeRouter
    {
        private readonly IConsoleService _consoleService;
        private readonly Logger _logger;

        public UtilityModeRouter(IConsoleService consoleService,
                                 Logger logger)
        {
            _consoleService = consoleService;
            _logger = logger;
        }

        public void Route(ApplicationModes applicationModes)
        {
            _logger.Info("Application mode: {0}", applicationModes);
            _consoleService.PrintHelp();
        }
    }
}
