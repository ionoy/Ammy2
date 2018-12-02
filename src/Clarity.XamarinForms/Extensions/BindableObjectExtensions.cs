using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Xamarin.Forms;

namespace Clarity
{
    public static partial class BindableObjectExtensions
    {
        public static TElement Grid_RowCol<TElement>(this TElement instance, int row, int col) where TElement : BindableObject
        {
            instance.SetValue(Grid.RowProperty, row);
            instance.SetValue(Grid.ColumnProperty, col);
            return instance;
        }

        public static TElement Init<TElement>(this TElement instance, Action<TElement> initializer) where TElement : BindableObject
        {
            initializer(instance);
            return instance;
        }

        public static TElement As<TElement>(this TElement instance, out TElement reference) where TElement : BindableObject
        {
            reference = instance;
            var btn = new Button();
            //btn.Subscribe(nameof(btn.Clicked), Handler, )
            return reference;
        }
        
        //public static void Handler(object i, int a)
        //{

        //}

        //public static TElement Subscribe<TElement, T0, T1>(this TElement instance, string eventName, Delegate handler, Disposables disposables) where TElement : BindableObject
        //{
        //    var instanceType = instance.GetType();
        //    var eventInfo = instanceType.GetEvent(eventName);
        //    var @delegate = CreateDelegateFromExpression(eventInfo.EventHandlerType, (object i, object a) => handler(i, a));

        //    eventInfo.AddEventHandler(instance, @delegate);
        //    disposables.Add(DisposableDummy.Create(() => eventInfo.RemoveEventHandler(instance, @delegate)));

        //    return instance;
        //}

        //private static Delegate CreateDelegateFromExpression<TEventArgs>(Type delegateType, Expression<Action<object, TEventArgs>> handlerExpression)
        //{
        //    var parameters = GetInvokeParameters(delegateType);
        //    var castedParameters = parameters.OfType<Expression>();
        //    var body = Expression.Invoke(handlerExpression, castedParameters);
        //    var listener = Expression.Lambda(delegateType, body, parameters);

        //    return listener.Compile();
        //}

        private static ParameterExpression[] GetInvokeParameters(Type eventHandlerType)
        {
            var invokeMethod = eventHandlerType.GetMethod("Invoke");
            var parameters = invokeMethod.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
            return parameters;
        }
    }
}
