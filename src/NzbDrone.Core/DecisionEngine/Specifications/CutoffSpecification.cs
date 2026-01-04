using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class CutoffSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly Logger _logger;
        private readonly ICustomFormatCalculationService _formatService;

        public CutoffSpecification(UpgradableSpecification upgradableSpecification,
                                   ICustomFormatCalculationService formatService,
                                   Logger logger)
        {
            _upgradableSpecification = upgradableSpecification;
            _formatService = formatService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteBook subject, SearchCriteriaBase searchCriteria)
        {
            var qualityProfile = subject.Author.QualityProfile.Value;
            var targetMediaType = GetMediaType(subject);

            var files = subject.Books.SelectMany(b => b.BookFiles.Value);
            if (targetMediaType != BookFileMediaType.Unknown)
            {
                files = files.Where(f => GetMediaType(f) == targetMediaType);
            }

            foreach (var file in files)
            {
                // Get a distinct list of all current track qualities for a given book
                var currentQualities = new List<QualityModel> { file.Quality };

                _logger.Debug("Comparing file quality with report. Existing files contain {0}", currentQualities.ConcatToString());

                var customFormats = _formatService.ParseCustomFormat(file);

                if (!_upgradableSpecification.CutoffNotMet(qualityProfile,
                                                           currentQualities,
                                                           customFormats,
                                                           subject.ParsedBookInfo.Quality))
                {
                    _logger.Debug("Cutoff already met by existing files, rejecting.");

                    var qualityCutoffIndex = qualityProfile.GetIndex(qualityProfile.Cutoff);
                    var qualityCutoff = qualityProfile.Items[qualityCutoffIndex.Index];

                    return Decision.Reject("Existing files meets cutoff: {0}", qualityCutoff);
                }
            }

            return Decision.Accept();
        }

        private static BookFileMediaType GetMediaType(RemoteBook subject)
        {
            return MediaFileExtensions.GetMediaTypeForQuality(subject?.ParsedBookInfo?.Quality?.Quality);
        }

        private static BookFileMediaType GetMediaType(BookFile file)
        {
            if (file == null)
            {
                return BookFileMediaType.Unknown;
            }

            if (file.MediaType != BookFileMediaType.Unknown)
            {
                return file.MediaType;
            }

            return MediaFileExtensions.GetMediaTypeForPath(file.Path);
        }
    }
}
