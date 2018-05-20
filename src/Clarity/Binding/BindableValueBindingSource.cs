using System;
using Xamarin.Forms;

namespace Clarity
{
    public class BindableValueBindingSource<T> : IBindingSource<T>
    {
        private readonly BindableValue<T> _bindableValue;
        
        public BindableValueBindingSource(BindableValue<T> bv)
        {
            _bindableValue = bv;
        }
        
        public BindingMode DefaultBindingMode => BindingMode.Default;
        public IDisposable Subscribe(Action<T> onUpdate) => _bindableValue.Subscribe(onUpdate);
        public void SetValue(T val) => _bindableValue.Value = val;
        public T GetValue() => _bindableValue.Value;
    }
}
