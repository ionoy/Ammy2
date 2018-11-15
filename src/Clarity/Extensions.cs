using System;

namespace Clarity
{
    public static class Extensions
    {
        public static void AddTo(this IDisposable disposable, Disposables disposables)
        {
            disposables.Add(disposable);
        }
    }
}