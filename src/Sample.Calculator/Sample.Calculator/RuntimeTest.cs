using Ammy;
using LiveSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sample.Calculator
{
    class RuntimeTest : AmmyPage
    {
        public int ReturnInt()
        {
            return 1;
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
