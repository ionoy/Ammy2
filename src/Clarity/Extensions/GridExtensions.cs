using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Clarity.Extensions
{
    public static partial class GridExtensions
    {
        public static RowDefinitionCollection Rows()
        {
            return new RowDefinitionCollection() {
                new RowDefinition() { }
            };
        }
    }
}
