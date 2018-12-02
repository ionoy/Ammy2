using System;

namespace Clarity
{
    public static class Extensions
    {
        public static T AddTo<T>(this T disposable, Disposables disposables) where T : IDisposable
        {
            disposables.Add(disposable);
            return disposable;
        }
    }
}