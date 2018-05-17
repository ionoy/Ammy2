using System;

namespace Clarity
{
    public static class ClarityInjector
    {
        public static Func<Type, object> Resolver = DefaultResolver.Resolve;
        public static TType Resolve<TType>() => (TType)Resolver(typeof(TType));
    }
}
