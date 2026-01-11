using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Validation;

namespace Readarr.Http.Validation
{
    public class EmptyCollectionValidator<TParent, TItem> : BookdarrPropertyValidator<TParent, IEnumerable<TItem>>
    {
        protected override string GetDefaultMessageTemplate(string errorCode) => "Collection Must Be Empty";

        public override bool IsValid(ValidationContext<TParent> context, IEnumerable<TItem> value)
        {
            if (value == null)
            {
                return true;
            }

            return value.Empty();
        }
    }
}
