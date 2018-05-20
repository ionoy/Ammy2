using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Clarity
{
    public static class Helpers
    {
        public static T SetPropertyValue<T, TV>(T instance, BindableProperty property, TV value) where T : BindableObject
        {
            instance.SetValue(property, value);
            return instance;
        }

        public static TElement SetPropertyValue<TElement, TProperty>(TElement instance, BindableProperty property, BindableValue<TProperty> value, BindingMode mode = BindingMode.Default) where TElement : BindableObject
        {
            new ClarityBinding<TProperty>(new BindableValueBindingSource<TProperty>(value), new BindableObjectBindingSource<TProperty>(instance, property), mode);
            return instance;
        }

        public static TElement SetPropertyValue<TElement, TProperty, TFrom>(TElement instance, BindableProperty property, BindableValue<TFrom> value, Func<TFrom, TProperty> selector) where TElement : BindableObject
        {
            new ClarityBinding<TProperty>(new BindableValueWithSelectorBindingSource<TFrom, TProperty>(value, selector), new BindableObjectBindingSource<TProperty>(instance, property), BindingMode.OneWay);
            return instance;
        }

        public static TElement SetAttachedValue<TElement, TVal>(TElement instance, BindableProperty property, TVal value) where TElement : BindableObject
        {
            instance.SetValue(property, value);
            return instance;
        }

        public static TLayout Children<TLayout>(this TLayout instance, params View[] children) where TLayout : Layout<View> => instance.Children((IEnumerable<View>)children);

        public static TLayout Children<TLayout>(this TLayout instance, params object[] children) where TLayout : Layout<View>
        {
            foreach (var child in children) {
                if (child is IEnumerable<View> views) {
                    foreach (var view in views)
                        instance.Children.Add(view);
                } else if (child is View view) {
                    instance.Children.Add(view);
                } else if (child != null) {
                    throw new InvalidOperationException("Cannot insert element of type " + child.GetType() + " into Children collection");
                } else throw new InvalidOperationException("Cannot insert null into Children collection");
            }

            return instance;
        }

        public static TLayout Children<TLayout>(this TLayout instance, IEnumerable<View> children) where TLayout : Layout<View>
        {
            foreach (var child in children)
                instance.Children.Add(child);
            return instance;
        }
    }
}
