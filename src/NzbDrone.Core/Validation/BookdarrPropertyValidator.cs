using FluentValidation.Validators;

namespace NzbDrone.Core.Validation
{
    public abstract class BookdarrPropertyValidator<T, TProperty> : PropertyValidator<T, TProperty>
    {
        public override string Name => GetType().Name;
    }
}
