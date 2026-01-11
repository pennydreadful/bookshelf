using System.Collections.Generic;
using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Common.Extensions;

namespace Readarr.Http.Validation
{
    public class EmptyCollectionValidator<T> : PropertyValidator<object, IEnumerable<T>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Collection Must Be Empty";

        protected override bool IsValid(ValidationContext<object> context, IEnumerable<T> value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Empty();
        }
    }
}
