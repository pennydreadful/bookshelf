using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentValidation;


namespace Readarr.Http.Validation
{
    public static class RuleBuilderExtensions
    {
        public static IRuleBuilderOptions<T, int> ValidId<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder.GreaterThan(0);
        }

        public static IRuleBuilderOptions<T, int> IsZero<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder.Equal(0);
        }

        public static IRuleBuilderOptions<T, string> HaveHttpProtocol<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Matches("^http(s)?://", RegexOptions.IgnoreCase).WithMessage("must start with http:// or https://");
        }

        public static IRuleBuilderOptions<T, string> NotBlank<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.NotNull().NotEmpty();
        }

        public static IRuleBuilderOptions<T, IEnumerable<TProp>> EmptyCollection<T, TProp>(this IRuleBuilder<T, IEnumerable<TProp>> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new EmptyCollectionValidator<TProp>());
        }

        public static IRuleBuilderOptions<T, int> IsValidRssSyncInterval<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new RssSyncIntervalValidator());
        }
    }
}
