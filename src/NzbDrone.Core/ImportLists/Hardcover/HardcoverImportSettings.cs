using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Hardcover
{
    public class HardcoverImportSettingsValidator : AbstractValidator<HardcoverImportSettings>
    {
        public HardcoverImportSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.ApiKey).NotEmpty();
            RuleFor(c => c.ListIds).NotEmpty();
        }
    }

    public class HardcoverImportSettings : IImportListSettings
    {
        private static readonly HardcoverImportSettingsValidator Validator = new ();

        public HardcoverImportSettings()
        {
            BaseUrl = "https://api.hardcover.app";
            ListIds = Array.Empty<string>();
        }

        [FieldDefinition(0, Label = "Base URL", HelpText = "Hardcover API base URL")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "API Key", Privacy = PrivacyLevel.ApiKey, HelpText = "Hardcover personal API key (from Settings > API)")]
        public string ApiKey { get; set; }

        [FieldDefinition(2, Type = FieldType.Select, SelectOptionsProviderAction = "getLists", Label = "List", HelpText = "Choose a list from your Hardcover account to sync")]
        public IEnumerable<string> ListIds { get; set; }

        public string ListId => ListIds?.FirstOrDefault();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
