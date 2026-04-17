using NzbDrone.Core.MetadataSource.Goodreads;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideSeriesInfo
    {
        SeriesResource GetSeriesInfo(int id, int page, bool useCache = true);
    }
}
