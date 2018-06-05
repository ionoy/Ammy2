using Clarity;
using LiveSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sample.Calculator
{
    class RuntimeTest : ClarityBase
    {
        delegate double CalcOp(double accu, double val);
        
        public int ReturnInt()
        {
            return 10;
        }

        [LiveSharp]
        public ContentPage MainPage()
        {
            var initOp = new CalcOp((_, val) => val);
            var labelText = CreateBindableValue(0);

            Device.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                labelText.Value = ReturnInt();
                return true;
            });

            return ContentPage.Content(
                       Label.Text(labelText, t => t.ToString())
                   );
        }
    }
}
