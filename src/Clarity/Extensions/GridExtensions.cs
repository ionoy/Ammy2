using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Clarity
{
    public static partial class GridExtensions
    {
        public static Grid Rows(this Grid instance, int rowCount, params GridLength[] heights)
        {
            var rows = new RowDefinitionCollection();
            for (int i = 0; i < rowCount; i++) {
                var rd = new RowDefinition();
                if (heights.Length > i)
                    rd.Height = heights[i];
                rows.Add(rd);
            }
            instance.RowDefinitions = rows;
            return instance;
        }

        public static Grid Cols(this Grid instance, int colCount, params GridLength[] widths)
        {
            var cols = new ColumnDefinitionCollection();
            for (int i = 0; i < colCount; i++) {
                var rd = new ColumnDefinition();
                if (widths.Length > i)
                    rd.Width = widths[i];
                cols.Add(rd);
            }
            instance.ColumnDefinitions = cols;
            return instance;
        }
    }
}
