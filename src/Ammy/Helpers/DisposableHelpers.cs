using System;

namespace Ammy
{
    public class DisposableDummy : IDisposable
    {
        private readonly Action _action;
        public DisposableDummy(Action action) => _action = action;
        public static IDisposable Create(Action action) => new DisposableDummy(action);
        public void Dispose() => _action();
    }
}
