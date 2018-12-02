using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Ammy
{
    public class BindableModel<TModel> : IDisposable
    {
        private TModel _model;
        private readonly ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();
        private readonly ConcurrentBag<IModelBindingSource<TModel>> _modelBindingSources = new ConcurrentBag<IModelBindingSource<TModel>>();
        private Validator<TModel> _validator;
        
        public BindableValue<string> ValidationMessage { get; } = new BindableValue<string>("");
        public BindableValue<bool> ValidationFailed { get; } = new BindableValue<bool>(false);

        public TModel Value {
            get => _model;
            set {
                _model = value;

                foreach (var bindingSource in _modelBindingSources)
                    bindingSource.CurrentModel = value;
            }
        }

        public BindableModel(TModel model, Action<TModel, Validator<TModel>> validate)
        {
            _model = model;

            if (validate != null)
                _validator = new Validator<TModel>(validate);
        }

        public BindableValue<TVal> GetOneWay<TVal>(Expression<Func<TModel, TVal>> memberExpression) => Get(memberExpression);
        public BindableValue<TVal> GetTwoWay<TVal>(Expression<Func<TModel, TVal>> memberExpression) => Get(memberExpression);
        public BindableValue<TVal> Get<TVal>(Expression<Func<TModel, TVal>> memberExpression, Action<TVal, Validator<TVal>> validate = null)
        {
            var modelBindingSource = new ModelPropertyBindingSource<TModel, TVal>(_model, memberExpression);
            var bindableValue = new BindableValue<TVal>(modelBindingSource.GetValue(), validate: validate);

            _disposables.Add(modelBindingSource.Subscribe(val => bindableValue.Value = val));
            _modelBindingSources.Add(modelBindingSource);

            return bindableValue;
        }

        public void Validate()
        {
            if (_validator == null)
                throw new InvalidOperationException("You need to set `validate` parameter in order to call Validate method successfully");

            _validator.Validate(_model, ValidationFailed, ValidationMessage);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();
        }
    }
}
