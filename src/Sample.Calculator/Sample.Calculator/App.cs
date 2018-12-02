using System.Linq;
using Ammy;
using LiveSharp;
using LiveSharp.Runtime;
using Xamarin.Forms;

[assembly:LiveSharpInjectRuleBaseClass("Xamarin.Forms.ContentPage", "Xamarin.Forms.ContentView")]

namespace Sample.Calculator
{
    public class App : Application {
        [LiveSharpStart]
        public App()
        {
            MainPage = new Calculator();

            LiveSharpContext.AddUpdateHandler(ctx => {
                var instances = ctx.UpdatedMethods
                                   .SelectMany(method => method.Instances)
                                   .Distinct()
                                   .ToArray();

                Device.BeginInvokeOnMainThread(() => {
                    foreach (var instance in instances) {
                        if (instance is ContentPage page)
                            page.Content = (View)instance?.GetType().GetMethod("BuildContent")?.Invoke(instance, null);
                        else if (instance is ContentView view)
                            view.Content = (View)instance?.GetType().GetMethod("BuildContent")?.Invoke(instance, null);
                    }
                });
            });
        }
    } 
}