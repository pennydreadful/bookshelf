using System;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public class RecycleBinDefaults : IHandle<ApplicationStartedEvent>
    {
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public RecycleBinDefaults(IConfigService configService,
                                  IDiskProvider diskProvider,
                                  IAppFolderInfo appFolderInfo,
                                  Logger logger)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public void Handle(ApplicationStartedEvent message)
        {
            if (_configService.RecycleBin.IsNotNullOrWhiteSpace())
            {
                return;
            }

            var appDataPath = _appFolderInfo.GetAppDataPath();
            var defaultRecycleBin = Path.Combine(appDataPath, "recycle");

            try
            {
                _diskProvider.CreateFolder(defaultRecycleBin);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to create recycle bin folder '{0}'", defaultRecycleBin);
                return;
            }

            _configService.RecycleBin = defaultRecycleBin;
            _logger.Info("Recycle bin set to default '{0}'", defaultRecycleBin);
        }
    }
}
