using System;

namespace Clarity
{
    public class ClarityBinding<TTo> : IDisposable
    {
        protected ClarityBindingMode _mode;
        protected IBindingSource<TTo> _from;
        protected IBindingSource<TTo> _to;
        
        private IDisposable _firstDisposable;
        private IDisposable _secondDisposable;

        public ClarityBinding(IBindingSource<TTo> from, IBindingSource<TTo> to, ClarityBindingMode mode, bool initialSet = true)
        {
            _from = from;
            _to = to;
            _mode = mode;

            if (_mode == ClarityBindingMode.Default)
                _mode = _to.DefaultBindingMode;

            _firstDisposable = from.Subscribe(val => to.SetValue(val));

            if (_mode != ClarityBindingMode.OneWay)
                _secondDisposable = to.Subscribe(val => from.SetValue(val));

            if (initialSet)
                to.SetValue(from.GetValue());
        }

        public void Dispose()
        {
            _firstDisposable?.Dispose();
            _secondDisposable?.Dispose();
        }
    }
}
