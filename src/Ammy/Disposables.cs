using System;
using System.Collections.Concurrent;

namespace Ammy
{
    public class Disposables : IDisposable
    {
        private readonly ConcurrentStack<IDisposable> _innerList = new ConcurrentStack<IDisposable>();

        public void Add(IDisposable diposable)
        {
            _innerList.Push(diposable);
        }
        
        public void Dispose()
        {
            while (_innerList.TryPop(out var disposable) && disposable != null)
                disposable.Dispose();
        }
    }
}
