using System;

namespace Ammy
{
    public interface IBindingSource<T>
    {
        AmmyBindingMode DefaultBindingMode { get; }

        IDisposable Subscribe(Action<T> onUpdate);
        void SetValue(T val);
        T GetValue();
    }
}
