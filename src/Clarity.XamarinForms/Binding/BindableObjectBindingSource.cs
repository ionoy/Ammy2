using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace Clarity
{

    public class BindableObjectBindingSource<T> : IBindingSource<T>
    {
        private readonly BindableObject _object;
        private readonly BindableProperty _property;

        public BindableObjectBindingSource(BindableObject bo, BindableProperty bp)
        {
            _object = bo;
            _property = bp;
        }

        public ClarityBindingMode DefaultBindingMode => (ClarityBindingMode)_property.DefaultBindingMode;
        public IDisposable Subscribe(Action<T> onUpdate)
        {
            _object.PropertyChanged += propertyChanged;

            void propertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == _property.PropertyName)
                    onUpdate((T)_object.GetValue(_property));
            }

            return DisposableHelpers.Create(() => _object.PropertyChanged -= propertyChanged);
        }

        public void SetValue(T val) => _object.SetValue(_property, val);
        public T GetValue() => (T)_object.GetValue(_property);
    }
}
