using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Ammy
{
    public static class DefaultResolver
    {
        enum RegistrationType { Type, Constructor, Instance }
        enum RegistrationMethod { Transient, Singleton }

        private static ConcurrentDictionary<Type, object> _singletons = new ConcurrentDictionary<Type, object>();
        private static ConcurrentDictionary<Type, (RegistrationType, RegistrationMethod, object)> _registrations = new ConcurrentDictionary<Type, (RegistrationType, RegistrationMethod, object)>();

        public static void RegisterTransient<TBase, TDerived>() => _registrations[typeof(TBase)] = (RegistrationType.Type, RegistrationMethod.Transient, typeof(TDerived));
        public static void RegisterTransient<TType>(Func<TType> constructor) => _registrations[typeof(TType)] = (RegistrationType.Constructor, RegistrationMethod.Transient, constructor);
        public static void RegisterTransient<TType>(TType instance) => _registrations[typeof(TType)] = (RegistrationType.Instance, RegistrationMethod.Transient, instance);

        public static void RegisterSingleton<TBase, TDerived>() => _registrations[typeof(TBase)] = (RegistrationType.Type, RegistrationMethod.Singleton, typeof(TDerived));
        public static void RegisterSingleton<TType>(Func<TType> constructor) => _registrations[typeof(TType)] = (RegistrationType.Constructor, RegistrationMethod.Singleton, constructor);
        public static void RegisterSingleton<TType>(TType instance) => _registrations[typeof(TType)] = (RegistrationType.Instance, RegistrationMethod.Singleton, instance);

        public static TType Resolve<TType>() => (TType)Resolve(typeof(TType));

        public static object Resolve(Type type)
        {
            object result;

            if (_registrations.TryGetValue(type, out var value)) {
                var (regType, regMethod, obj) = value;

                if (regMethod == RegistrationMethod.Singleton && _singletons.TryGetValue(type, out result))
                    return result;

                if (regType == RegistrationType.Type) {
                    result = BuildWithReflection((Type)obj);
                } else if (regType == RegistrationType.Constructor) {
                    result = ((Delegate)obj).DynamicInvoke();
                } else if (regType == RegistrationType.Instance) {
                    result = obj;
                } else {
                    throw new InvalidOperationException();
                }

                if (regMethod == RegistrationMethod.Singleton)
                    _singletons[type] = result;

                return result;
            } else {
                if (type.IsValueType)
                    return Activator.CreateInstance(type);

                if (TryCreateInstance(type, out result))
                    return result;

                return null;
            }
        }

        private static bool TryCreateInstance(Type type, out object result)
        {
            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null) {
                result = Activator.CreateInstance(type);
                return true;
            }
            result = null;
            return false;
        }

        private static object BuildWithReflection(Type type)
        {
            var ctors = type.GetConstructors();

            if (ctors.Length > 1)
                throw new Exception($"Type {type.Name} should only have one constructor");

            if (ctors.Length == 0) {
                return Activator.CreateInstance(type);
            } else {
                var ctor = ctors[0];
                var resolvedParameters = ctor.GetParameters()
                                             .Select(p => Resolve(p.ParameterType))
                                             .ToArray();
                return ctor.Invoke(resolvedParameters);
            }
        }
    }
}
