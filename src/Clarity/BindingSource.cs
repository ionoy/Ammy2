using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using Xamarin.Forms;

namespace Clarity
{
    public interface IBindingSource<T>
    {
        BindingMode DefaultBindingMode { get; }

        IDisposable Subscribe(Action<T> onUpdate);
        void SetValue(T val);
        T GetValue();
    }

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

    public interface IModelBindingSource<TModel>
    {
        TModel CurrentModel { get; set; }
    }

    public class ModelPropertyBindingSource<TModel, TMember> : IModelBindingSource<TModel>, IBindingSource<TMember>
    {
        private readonly Func<TModel, TMember> _getter;
        private readonly Action<TModel, TMember> _setter;
        private readonly string _memberName;
        private readonly ConcurrentBag<ValueTuple<Action<TMember>, IDisposable>> _subscriptions = new ConcurrentBag<ValueTuple<Action<TMember>, IDisposable>>();

        private TModel _model;
        public TModel CurrentModel {
            get => _model;
            set {
                _model = value;

                foreach (var (handler, disposable) in _subscriptions)
                    Resubscribe(value, handler, disposable);
            }
        }

        public BindingMode DefaultBindingMode => BindingMode.Default;

        public ModelPropertyBindingSource(TModel currentModel, Expression<Func<TModel, TMember>> memberExpression)
        {
            _getter = memberExpression.GetGetter(out var memberName);
            _setter = memberExpression.GetSetter();
            _memberName = memberName;

            CurrentModel = currentModel;
        }

        public TMember GetValue() => _getter(CurrentModel);
        public void SetValue(TMember val) => _setter(CurrentModel, val);

        public IDisposable Subscribe(Action<TMember> onUpdate) => Subscribe(CurrentModel, onUpdate);

        private IDisposable Subscribe(TModel model, Action<TMember> onUpdate)
        {
            if (CurrentModel is INotifyPropertyChanged inpc) {
                inpc.PropertyChanged += propertyChanged;

                void propertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == _memberName)
                        onUpdate(_getter(CurrentModel));
                }

                var dispose = DisposableHelpers.Create(() => inpc.PropertyChanged -= propertyChanged);

                _subscriptions.Add((onUpdate, dispose));

                return dispose;
            }

            return DisposableHelpers.Create(() => { });
        }

        private void Resubscribe(TModel value, Action<TMember> handler, IDisposable disposable)
        {
            disposable.Dispose();
            Subscribe(value, handler);
        }
    }

    public class BindableObjectBindingSource<T> : IBindingSource<T>
    {
        private readonly BindableObject _object;
        private readonly BindableProperty _property;

        public BindableObjectBindingSource(BindableObject bo, BindableProperty bp)
        {
            _object = bo;
            _property = bp;
        }

        public BindingMode DefaultBindingMode => _property.DefaultBindingMode;
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
