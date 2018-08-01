using System;
using System.Collections.Generic;
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
            return reference;
        }
    }
}
