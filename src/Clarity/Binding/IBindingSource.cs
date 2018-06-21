using System;

namespace Clarity
{
    public interface IBindingSource<T>
    {
        ClarityBindingMode DefaultBindingMode { get; }

        IDisposable Subscribe(Action<T> onUpdate);
        void SetValue(T val);
        T GetValue();
    }
}
