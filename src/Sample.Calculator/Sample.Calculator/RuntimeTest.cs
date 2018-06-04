using Clarity;
using Sample.Calculator.Clarity.Runtime;
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
            var res = ClarityRuntime.GetUpdate(typeof(RuntimeTest), nameof(ReturnInt));
            if (res != null) return (int)ClarityRuntime.Execute(res, this);

            return 15;
        }

        public void VoidMethod()
        {
            var res = ClarityRuntime.GetUpdate(typeof(RuntimeTest), nameof(ReturnInt));
            if (res != null) ClarityRuntime.ExecuteVoid(res, this);
        }

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
