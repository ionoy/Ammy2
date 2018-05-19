using Clarity;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Sample.Calculator
{
    public class App : Application {
        public App() => MainPage = new NavigationPage(new Calculator().MainPage());
    }

    public class Calculator : ClarityBase
    {
        delegate double CalcOp(double accu, double val);
        CalcOp _initOp = (_, val) => val;

        public ContentPage MainPage()
        {
            var current = CreateBindableValue(0.0);
            var queuedVal = 0.0;
            var queuedOp = _initOp;
            var operatorQueued = false;

            void appendNumber(int number)
            {
                if (operatorQueued) {
                    current.Value = number;
                    operatorQueued = false;
                } else {
                    current.Value = current.Value * 10 + number;
                }
            }
            void applyOperator(CalcOp op)
            {
                queuedVal = current.Value = queuedOp(queuedVal, current.Value);
                queuedOp = op;
                operatorQueued = true;
            }
            void clear()
            {
                queuedVal = 0;
                queuedOp = _initOp;
                current.Value = 0;
            }

            return ContentPage.Content(
                       Grid.Rows(6).Cols(4)
                           .Padding(new Thickness(10))
                           .Children(
                               Label.Grid_ColumnSpan(4)
                                    .FontSize(32).FontAttributes(FontAttributes.Bold)
                                    .HorizontalTextAlignment(TextAlignment.End)
                                    .VerticalTextAlignment(TextAlignment.Center)
                                    .Text(current, v => v.ToString()),
                               BuildNumberPad(appendNumber),
                               Button.Text("/").Grid_RowCol(1, 3).Command(() => applyOperator((accu, val) => accu / val)),
                               Button.Text("*").Grid_RowCol(2, 3).Command(() => applyOperator((accu, val) => accu * val)),
                               Button.Text("-").Grid_RowCol(3, 3).Command(() => applyOperator((accu, val) => accu - val)),
                               Button.Text("+").Grid_RowCol(4, 3).Command(() => applyOperator((accu, val) => accu + val)),
                               Button.Text("C").Grid_RowCol(5, 0).Command(clear),
                               Button.Text("=").Grid_RowCol(5, 1).Command(() => applyOperator((accu, _) => accu)).Grid_ColumnSpan(3)
                           )
                   );
        }

        IEnumerable<Button> BuildNumberPad(Action<int> appendNumber)
        {
            yield return Button.Grid_Row(4)
                               .Grid_ColumnSpan(3)
                               .Command(() => appendNumber(0))
                               .Text("0");

            for (int i = 1; i < 10; i++) {
                var number = i;
                yield return Button.Grid_RowCol(4 - ((i + 2) / 3), (i + 2) % 3)
                                   .Command(() => appendNumber(number))
                                   .Text(i.ToString());
            }
        }
    }
}