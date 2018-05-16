using System;
using System.Collections.Concurrent;
using Xamarin.Forms;

namespace Clarity
{
    public class BindableValue<TVal> : IDisposable
    {
        private readonly ConcurrentDictionary<Action<TVal>, object> _changeHandlers = new ConcurrentDictionary<Action<TVal>, object>();
        private readonly ConcurrentDictionary<IDisposable, object> _bindings = new ConcurrentDictionary<IDisposable, object>();
        private TVal _value;
        private Validator<TVal> _validator;

        public TVal Value {
            get => _value;
            set {
                _value = value;

                foreach (var changeHandler in _changeHandlers)
                    changeHandler.Key(value);
            }
        }

        public BindableValue<string> ValidationMessage { get; } = new BindableValue<string>("");
        public BindableValue<bool> ValidationFailed { get; } = new BindableValue<bool>(false);

        public BindableValue(TVal value, Action<TVal, Validator<TVal>> validate = null)
        {
            _value = value;

            if (validate != null)
                _validator = new Validator<TVal>(validate);
        }

        public void SetValueSilent(TVal value) => _value = value;

        public IDisposable Subscribe(Action<TVal> newValue)
        {
            _changeHandlers[newValue] = null;

            return DisposableHelpers.Create(() => _changeHandlers.TryRemove(newValue, out var _));
        }

        public void AddBinding(IDisposable binding) => _bindings[binding] = null;

        public void Validate()
        {
            if (_validator == null)
                throw new InvalidOperationException("You need to set `validate` parameter in order to call Validate method successfully");

            _validator.Validate(_value, ValidationFailed, ValidationMessage);
        }

        public void Dispose()
        {
            foreach (var binding in _bindings)
                binding.Key.Dispose();
        }
    }
}
