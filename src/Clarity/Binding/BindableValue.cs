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
        private BindableValue<string> _validationMessage;
        private BindableValue<bool> _validationFailed;

        public TVal Value {
            get => _value;
            set {
                if (object.Equals(_value, value))
                    return;

                _value = value;

                foreach (var changeHandler in _changeHandlers)
                    changeHandler.Key(value);

                Validate(false);
            }
        }

        public BindableValue<string> ValidationMessage => _validationMessage ?? new BindableValue<string>("");
        public BindableValue<bool> ValidationFailed => _validationFailed ?? new BindableValue<bool>(false);
        
        public BindableValue(TVal value, Action<TVal, Validator<TVal>> validate = null)
        {
            _value = value;

            if (validate != null) {
                _validationMessage = new BindableValue<string>("");
                _validationFailed = new BindableValue<bool>(false);
                _validator = new Validator<TVal>(validate);
            }
        }
        
        public IDisposable Subscribe(Action<TVal> newValue)
        {
            _changeHandlers[newValue] = null;

            return DisposableHelpers.Create(() => _changeHandlers.TryRemove(newValue, out var _));
        }

        public void AddBinding(IDisposable binding) => _bindings[binding] = null;

        public void Validate(bool throwIfNoValidator = true)
        {
            if (_validator != null) {
                _validator.Reset();
                _validator.Validate(_value, _validationFailed, _validationMessage);
            } else if (throwIfNoValidator)
                throw new InvalidOperationException("You need to set `validate` parameter in order to call Validate method successfully");
        }

        public void Dispose()
        {
            foreach (var binding in _bindings)
                binding.Key.Dispose();
        }
    }
}
