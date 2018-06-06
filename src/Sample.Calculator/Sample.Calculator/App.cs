using Clarity;
using LiveSharp;
using Xamarin.Forms;

namespace Sample.Calculator
{
    public class App : Application {
        public App()
        {
            MainPage = new RuntimeTest();

            LiveSharpContext.AddUpdateHandler<ClarityPage>(ctx => {
                if (ctx.MethodName == "BuildContent()")
                {
                    ctx.ExecuteWithResult<ClarityPage, View>((instance, result) => {
                        Device.BeginInvokeOnMainThread(() => instance.Content = result);
                    });
                }
            });
        }
    } 
}