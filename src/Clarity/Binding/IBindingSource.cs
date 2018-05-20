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
}
