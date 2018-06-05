using Clarity;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Sample.Calculator
{
    public class App : Application {
        public App()
        {
            MainPage = new RuntimeTest().MainPage();
        }
    } 
}