// Modified from
// https://raw.githubusercontent.com/Prowlarr/Prowlarr/refs/heads/develop/src/NzbDrone.Core/Indexers/Definitions/MyAnonamouse.cs

using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.MyAnonaMouse
{
    public class MyAnonaMouseSettingsValidator : AbstractValidator<MyAnonaMouseSettings>
    {
        public MyAnonaMouseSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.MamId).NotEmpty();

            RuleFor(c => c.SeedCriteria).SetValidator(_ => new SeedCriteriaSettingsValidator());
        }
    }

    public class MyAnonaMouseSettings : ITorrentIndexerSettings
    {
        private static readonly MyAnonaMouseSettingsValidator Validator = new MyAnonaMouseSettingsValidator();

        public MyAnonaMouseSettings()
        {
            BaseUrl = "https://www.myanonamouse.net/";
            MamId = "";
            SearchType = (int)MyAnonaMouseSearchType.All;
            SearchInDescription = false;
            SearchInSeries = false;
            SearchInFilenames = false;
            SearchLanguages = System.Array.Empty<int>();
            UseFreeleechWedge = (int)MyAnonaMouseFreeleechWedgeAction.Never;
            MinimumSeeders = IndexerDefaults.MINIMUM_SEEDERS;
        }

        [FieldDefinition(0, Label = "Website URL")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Type = FieldType.Textbox, Label = "Mam Id", HelpText = "Mam Session Id (Created Under Preferences -> Security)")]
        public string MamId { get; set; }

        [FieldDefinition(2, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date Readarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, Label = "Search Type", SelectOptions = typeof(MyAnonaMouseSearchType), HelpText = "Specify the desired search type")]
        public int SearchType { get; set; }

        [FieldDefinition(4, Type = FieldType.Checkbox, Label = "Search in description", HelpText = "Search text in the description")]
        public bool SearchInDescription { get; set; }

        [FieldDefinition(5, Type = FieldType.Checkbox, Label = "Search in series", HelpText = "Search text in the series")]
        public bool SearchInSeries { get; set; }

        [FieldDefinition(6, Type = FieldType.Checkbox, Label = "Search in filenames", HelpText = "Search text in the filenames")]
        public bool SearchInFilenames { get; set; }

        [FieldDefinition(7, Type = FieldType.Select, Label = "Search Languages", SelectOptions = typeof(MyAnonaMouseSearchLanguages), HelpText = "Specify the desired languages. If unspecified, all options are used.")]
        public IEnumerable<int> SearchLanguages { get; set; }

        [FieldDefinition(8, Type = FieldType.Select, Label = "Use Freeleech Wedges", SelectOptions = typeof(MyAnonaMouseFreeleechWedgeAction), HelpText = "Use freeleech wedges to make grabbed torrents personal freeleech")]
        public int UseFreeleechWedge { get; set; }

        [FieldDefinition(9, Type = FieldType.Number, Label = "Minimum Seeders", HelpText = "Minimum number of seeders required.", Advanced = true)]
        public int MinimumSeeders { get; set; }

        [FieldDefinition(10)]
        public SeedCriteriaSettings SeedCriteria { get; set; } = new SeedCriteriaSettings();

        [FieldDefinition(11, Type = FieldType.Checkbox, Label = "Reject Blocklisted Torrent Hashes While Grabbing", HelpText = "If a torrent is blocked by hash it may not properly be rejected during RSS/Search for some indexers, enabling this will allow it to be rejected after the torrent is grabbed, but before it is sent to the client.", Advanced = true)]
        public bool RejectBlocklistedTorrentHashesWhileGrabbing { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
