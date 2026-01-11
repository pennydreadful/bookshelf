using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Organizer
{
    public static class FileNameValidation
    {
        internal static readonly Regex OriginalTokenRegex = new Regex(@"(\{original[- ._](?:title|filename)\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static readonly Regex PartTokenRegex = new Regex(@"\{[^}]*Part(?:Number|Count)(?::[^}]*)?\}",
                                                                  RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IRuleBuilderOptions<T, string> ValidBookFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.NotEmpty();
            ruleBuilder.SetValidator(new IllegalCharactersValidator<T>());

            return ruleBuilder.SetValidator(new ValidStandardTrackFormatValidator<T>());
        }

        public static IRuleBuilderOptions<T, string> ValidAuthorFolderFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.NotEmpty();
            ruleBuilder.SetValidator(new IllegalCharactersValidator<T>());

            return ruleBuilder.Matches(FileNameBuilder.AuthorNameRegex).WithMessage("Must contain Author name");
        }
    }

    public class ValidStandardTrackFormatValidator<T> : BookdarrPropertyValidator<T, string>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Must contain Book Title AND PartNumber, OR Original Title";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            if (value == null)
            {
                return false;
            }

            var hasBookTitle = FileNameBuilder.BookTitleRegex.IsMatch(value);
            var hasPartToken = FileNameBuilder.PartRegex.IsMatch(value) || FileNameValidation.PartTokenRegex.IsMatch(value);

            return (hasBookTitle && hasPartToken) ||
                   FileNameValidation.OriginalTokenRegex.IsMatch(value);
        }
    }

    public class IllegalCharactersValidator<T> : BookdarrPropertyValidator<T, string>
    {
        private readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

        protected override string GetDefaultMessageTemplate(string errorCode) => "Contains illegal characters: {InvalidCharacters}";

        public override bool IsValid(ValidationContext<T> context, string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return true;
            }

            var invalidCharacters = _invalidPathChars.Where(i => value!.IndexOf(i) >= 0).ToList();
            if (invalidCharacters.Any())
            {
                context.MessageFormatter.AppendArgument("InvalidCharacters", string.Join("", invalidCharacters));
                return false;
            }

            return true;
        }
    }
}
