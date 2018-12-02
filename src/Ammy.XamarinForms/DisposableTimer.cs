using System;
using Xamarin.Forms;

namespace Ammy
{
    public class DisposableTimer : IDisposable
    {
        private bool _isDisposed;

        public DisposableTimer(TimeSpan period, Action action)
        {
            Device.StartTimer(period, () => {
                action();
                return !_isDisposed;
            });
        }

        public static DisposableTimer Create(TimeSpan period, Action action)
        {
            return new DisposableTimer(period, action);
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}