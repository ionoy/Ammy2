using Clarity;
using LiveSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sample.Calculator
{
    class RuntimeTest : ClarityPage
    {
        public int ReturnInt()
        {
            return 20;
        }

        public override View BuildContent()
        {
            var labelText = CreateBindableValue(ReturnInt());

            return StackLayout.Children(
                Label.Text(labelText, t => t.ToString()),
                Label.Text("Hello"));
        }
    }
}
