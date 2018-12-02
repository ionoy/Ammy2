using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ammy
{
    public static class ExpressionHelpers
    {
        public static Func<TModel, TVal> GetGetter<TModel, TVal>(this Expression<Func<TModel, TVal>> getter, out string memberName)
        {
            var me = getter.Body as MemberExpression;
            if (me == null)
                throw new InvalidOperationException("Please provide a valid property getter expression, like `model => model.Property`, instead of " + getter);

            memberName = me.Member.Name;

            if (me.Member is FieldInfo field)
                return model => (TVal)field.GetValue(model);

            if (me.Member is PropertyInfo property)
                return model => (TVal)property.GetValue(model);

            throw new Exception("Only fields and properties are allowed in member expression " + getter);
        }

        public static Action<TModel, TVal> GetSetter<TModel, TVal>(this Expression<Func<TModel, TVal>> getter)
        {
            var me = getter.Body as MemberExpression;
            if (me == null)
                throw new InvalidOperationException("Please provide a valid property getter expression, like `model => model.Property`, instead of " + getter);

            if (me.Member is FieldInfo field)
                return (model, value) => field.SetValue(model, value);

            if (me.Member is PropertyInfo property)
                return (model, value) => property.SetValue(model, value);

            throw new Exception("Only fields and properties are allowed in member expression " + getter);
        }
    }
}
