using System.Collections.Generic;
using FluentValidation.Validators;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Validation;

namespace Readarr.Http.Validation
{
    public class EmptyCollectionValidator<T> : BookdarrPropertyValidator<object, IEnumerable<T>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Collection Must Be Empty";

        public override bool IsValid(ValidationContext<object> context, IEnumerable<T> value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Empty();
        }
    }
}
