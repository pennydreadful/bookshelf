using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentValidation;
using Readarr.Http.ClientSchema;

namespace Readarr.Http.REST
{
    public class ResourceValidator<TResource> : AbstractValidator<TResource>
    {
        public IRuleBuilderOptions<TResource, TProperty> RuleForField<TProperty>(Expression<Func<TResource, IEnumerable<Field>>> fieldListAccessor, string fieldName)
        {
            var parameter = fieldListAccessor.Parameters[0];
            var fieldsExpression = Expression.Invoke(fieldListAccessor, parameter);

            var fieldParameter = Expression.Parameter(typeof(Field), "field");
            var nameMatch = Expression.Equal(
                Expression.Property(fieldParameter, nameof(Field.Name)),
                Expression.Constant(fieldName));
            var predicate = Expression.Lambda<Func<Field, bool>>(nameMatch, fieldParameter);

            var singleOrDefault = typeof(Enumerable)
                .GetMethods()
                .Single(method => method.Name == nameof(Enumerable.SingleOrDefault) && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(Field));
            var selectedField = Expression.Call(singleOrDefault, fieldsExpression, predicate);

            var fieldVariable = Expression.Variable(typeof(Field), "selectedField");
            var assignField = Expression.Assign(fieldVariable, selectedField);
            var isNull = Expression.Equal(fieldVariable, Expression.Constant(null, typeof(Field)));
            var fieldValue = Expression.Property(fieldVariable, nameof(Field.Value));
            var castValue = Expression.Convert(fieldValue, typeof(TProperty));

            var body = Expression.Block(
                new[] { fieldVariable },
                assignField,
                Expression.Condition(isNull, Expression.Default(typeof(TProperty)), castValue));

            var expression = Expression.Lambda<Func<TResource, TProperty>>(body, parameter);

            return RuleFor(expression)
                .OverridePropertyName(fieldName)
                .WithName(fieldName);
        }
    }
}
