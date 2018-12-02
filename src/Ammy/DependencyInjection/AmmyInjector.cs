using System;

namespace Ammy
{
    public static class AmmyInjector
    {
        public static Func<Type, object> Resolver = DefaultResolver.Resolve;
        public static TType Resolve<TType>() => (TType)Resolver(typeof(TType));
    }
}
