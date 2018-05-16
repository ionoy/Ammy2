using System;
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

        public static TLayout Children<TLayout>(this TLayout instance, params View[] children) where TLayout : Layout<View>
        {
            foreach (var child in children)
                instance.Children.Add(child);
            return instance;
        }
    }
}
