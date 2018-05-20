using System;
using Xamarin.Forms;

namespace Clarity
{
    public class BindableValueWithSelectorBindingSource<TOriginal, T> : IBindingSource<T>
    {
        private readonly BindableValue<TOriginal> _bindableValue;
        private readonly Func<TOriginal, T> _selector;

        public BindableValueWithSelectorBindingSource(BindableValue<TOriginal> bv, Func<TOriginal, T> selector)
        {
            _bindableValue = bv;
            _selector = selector;
        }

        public BindingMode DefaultBindingMode => BindingMode.Default;
        public IDisposable Subscribe(Action<T> onUpdate) => _bindableValue.Subscribe(val => onUpdate(_selector(val)));
        public void SetValue(T val) => throw new InvalidOperationException("Can't update bindable value with expression");
        public T GetValue() => _selector(_bindableValue.Value);
    }
}
