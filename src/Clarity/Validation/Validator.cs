using System;
using System.Collections.Generic;

namespace Clarity
{

    public class Validator<TVal>
    {
        public bool _isSuccess;
        public IReadOnlyList<string> Errors => _errors;

        private readonly Action<TVal, Validator<TVal>> _validate;
        private List<string> _errors = new List<string>();

        public Validator(Action<TVal, Validator<TVal>> validate)
        {
            _validate = validate;
        }

        public void AddError(string error)
        {
            _errors.Add(error);
            _isSuccess = false;
        }
       
        public void Reset()
        {
            _errors.Clear();
            _isSuccess = true;
        }

        public void Validate(TVal value, BindableValue<bool> validationFailed, BindableValue<string> validationMessage)
        {
            _validate(value, this);

            validationFailed.Value = !_isSuccess;
            validationMessage.Value = string.Join(Environment.NewLine, _errors);
        }
    }
}
