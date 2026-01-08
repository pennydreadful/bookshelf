using NLog;
using NzbDrone.Core.Lifecycle.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Lifecycle
{
    public interface ILifecycleService
    {
        void Shutdown();
        void Restart();
    }

    public class LifecycleService : ILifecycleService, IExecute<ShutdownCommand>, IExecute<RestartCommand>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public LifecycleService(IEventAggregator eventAggregator,
                                Logger logger)
        {
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void Shutdown()
        {
            _logger.Info("Shutdown requested.");
            _eventAggregator.PublishEvent(new ApplicationShutdownRequested());

        }

        public void Restart()
        {
            _logger.Info("Restart requested.");

            _eventAggregator.PublishEvent(new ApplicationShutdownRequested(true));

        }

        public void Execute(ShutdownCommand message)
        {
            Shutdown();
        }

        public void Execute(RestartCommand message)
        {
            Restart();
        }
    }
}
