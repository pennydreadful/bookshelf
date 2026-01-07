using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QualityAllowedByProfileSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public QualityAllowedByProfileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        private static QualityProfileQualityItem FindQualityItem(QualityProfile profile, Quality quality)
        {
            if (profile?.Items == null || quality == null)
            {
                return null;
            }

            foreach (var item in profile.Items)
            {
                if (item.Quality?.Id == quality.Id)
                {
                    return item;
                }

                if (item.Items?.Any(i => i.Quality?.Id == quality.Id) == true)
                {
                    return item;
                }
            }

            return null;
        }

        public virtual Decision IsSatisfiedBy(RemoteBook subject, SearchCriteriaBase searchCriteria)
        {
            _logger.Debug("Checking if report meets quality requirements. {0}", subject.ParsedBookInfo.Quality);

            var profile = subject.Author.QualityProfile.Value;
            var quality = subject.ParsedBookInfo.Quality.Quality;
            var qualityOrGroup = FindQualityItem(profile, quality);

            if (qualityOrGroup == null && quality == Quality.UnknownAudio)
            {
                qualityOrGroup = FindQualityItem(profile, Quality.MP3);
            }

            if (qualityOrGroup == null && quality == Quality.LikelyAudiobook)
            {
                qualityOrGroup = FindQualityItem(profile, Quality.UnknownAudio) ??
                    FindQualityItem(profile, Quality.MP3);
            }

            if (qualityOrGroup == null && quality == Quality.LikelyEbook)
            {
                qualityOrGroup = FindQualityItem(profile, Quality.Unknown);
            }

            if (qualityOrGroup == null || !qualityOrGroup.Allowed)
            {
                _logger.Debug("Quality {0} rejected by Author's quality profile", subject.ParsedBookInfo.Quality);
                return Decision.Reject("{0} is not wanted in profile", subject.ParsedBookInfo.Quality.Quality);
            }

            return Decision.Accept();
        }
    }
}
