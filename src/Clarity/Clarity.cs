using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using Xamarin.Forms;

namespace Clarity
{
    public partial class ClarityBase : IDisposable
    {
        public ConcurrentBag<IDisposable> Disposables = new ConcurrentBag<IDisposable>();

        public ICommand Command(Action function) => new Command(function);

        public void Dispose()
        {
            foreach (var disposable in Disposables)
                disposable.Dispose();
        }

        public BindableModel<T> CreateBindableModel<T>(T model, Action<T, Validator<T>> validate = null)
        {
            var bm = new BindableModel<T>(model, validate);
            Disposables.Add(bm);
            return bm;
        }

        public BindableValue<T> CreateBindableValue<T>(T value, Action<T, Validator<T>> validate = null)
        {
            var bv = new BindableValue<T>(value, validate);
            Disposables.Add(bv);
            return bv;
        }
    }
}
