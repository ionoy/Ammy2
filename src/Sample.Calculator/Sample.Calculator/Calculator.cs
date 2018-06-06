using Clarity;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Sample.Calculator
{
    using CalcOp = Func<double, double, double>;

    public class Calculator : ClarityPage
    {
        public override View BuildContent()
        {
            var initOp = new CalcOp((_, val) => val);
            var currentValue = CreateBindableValue(0.0);
            var queuedValue = 0.0;
            var previousOp = initOp;
            var isOperatorQueued = false;
            
            void appendNumber(int number)
            {
                if (isOperatorQueued) {
                    currentValue.Value = number;
                    isOperatorQueued = false;
                } else {
                    currentValue.Value = currentValue.Value * 10 + number;
                }
            }
            void applyOperator(CalcOp op)
            {
                queuedValue = currentValue.Value = previousOp(queuedValue, currentValue.Value);
                previousOp = op;
                isOperatorQueued = true;
            }
            void clear()
            {
                queuedValue = 0;
                previousOp = initOp;
                currentValue.Value = 0;
            }

            return Grid.Rows(6).Cols(4)
                       .Padding(new Thickness(10))
                       .Children(
                           Label.Grid_ColumnSpan(4)
                               .FontSize(32).FontAttributes(FontAttributes.Bold)
                               .HorizontalTextAlignment(TextAlignment.End)
                               .VerticalTextAlignment(TextAlignment.Center)
                               .Text(currentValue, v => v.ToString()),
                           BuildNumberPad(appendNumber),
                           Button.Text("/").Grid_RowCol(1, 3).Command(() => applyOperator((accu, val) => accu / val)),
                           Button.Text("*").Grid_RowCol(2, 3).Command(() => applyOperator((accu, val) => accu * val)),
                           Button.Text("-").Grid_RowCol(3, 3).Command(() => applyOperator((accu, val) => accu - val)),
                           Button.Text("+").Grid_RowCol(4, 3).Command(() => applyOperator((accu, val) => accu + val)),
                           Button.Text("C").Grid_RowCol(5, 0).Command(clear),
                           Button.Text("=").Grid_RowCol(5, 1).Command(() => applyOperator((accu, _) => accu)).Grid_ColumnSpan(3)
                       );
        }

        IEnumerable<Button> BuildNumberPad(Action<int> appendNumber) =>
            Enumerable.Range(0, 10).Select(i => {
                return Button.Grid_RowCol(i == 0 ? 4 : 4 - ((i + 2) / 3),
                                          i == 0 ? 0 : (i + 2) % 3)
                             .Grid_ColumnSpan(i == 0 ? 3 : 1)
                             .Command(() => appendNumber(i))
                             .Text(i.ToString());
            });
    }
}