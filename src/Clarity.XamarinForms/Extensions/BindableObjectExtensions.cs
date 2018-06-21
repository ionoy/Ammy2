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
    }
}
