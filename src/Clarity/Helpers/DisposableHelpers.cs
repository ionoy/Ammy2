using System;

namespace Clarity
{

    public static class DisposableHelpers
    {
        public class DisposableDummy : IDisposable
        {
            private readonly Action _action;
            public DisposableDummy(Action action) => _action = action;
            public void Dispose() => _action();
        }

        public static IDisposable Create(Action action) => new DisposableDummy(action);
    }

}
