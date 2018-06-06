using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using Xamarin.Forms;

namespace Clarity
{
    public abstract partial class ClarityPage : ContentPage, IDisposable
    {
        public ClarityPage()
        {
            Content = BuildContent();
        }

        public ConcurrentBag<IDisposable> Disposables = new ConcurrentBag<IDisposable>();
        
        public abstract View BuildContent();

        public void Dispose()
        {
            foreach (var disposable in Disposables)
                disposable.Dispose();
        }

        public ICommand Command(Action function) => new Command(function);

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

        public Task PushAsync(Page page) => Application.Current.MainPage.Navigation.PushAsync(page);
        public Task PopAsync() => Application.Current.MainPage.Navigation.PopAsync();
        public Task PopToRootAsync() => Application.Current.MainPage.Navigation.PopToRootAsync();

        public Task PushModalAsync(Page page) => Application.Current.MainPage.Navigation.PushModalAsync(page);
        public Task PopModalAsync() => Application.Current.MainPage.Navigation.PopModalAsync();

        public TOwner Build<TOwner>() => ClarityInjector.Resolve<TOwner>();
    }
}
