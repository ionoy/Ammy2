using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Clarity
{
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

        public ClarityBindingMode DefaultBindingMode => ClarityBindingMode.Default;

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

                var dispose = DisposableDummy.Create(() => inpc.PropertyChanged -= propertyChanged);

                _subscriptions.Add((onUpdate, dispose));

                return dispose;
            }

            return DisposableDummy.Create(() => { });
        }

        private void Resubscribe(TModel value, Action<TMember> handler, IDisposable disposable)
        {
            disposable.Dispose();
            Subscribe(value, handler);
        }
    }
}
