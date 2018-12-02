using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Ammy.VisualStudio.Generation
{
    class MetaGenerator
    {
        public AmmyPageGenerator AmmyPage { get; } = new AmmyPageGenerator();
        public ExtensionsGenerator Extensions { get; } = new ExtensionsGenerator();

        public string Generate()
        {
            var builder = new StringBuilder();

            builder.AppendLine("namespace Ammy {");
            
            builder.AppendLine(
@"
  public abstract partial class AmmyPage : Xamarin.Forms.ContentPage, System.IDisposable
  {
      public Disposables Disposables = new Disposables();
      
      public void Dispose()
      {
          Disposables.Dispose();
      }
       
      public System.Windows.Input.ICommand Command(System.Action function) => new Xamarin.Forms.Command(function);

      public BindableModel<T> CreateBindableModel<T>(T model, System.Action<T, Validator<T>> validate = null)
      {
          var bm = new BindableModel<T>(model, validate);
          Disposables.Add(bm);
          return bm;
      }

      public BindableValue<T> CreateBindableValue<T>(T value, System.Action<T, Validator<T>> validate = null)
      {
          var bv = new BindableValue<T>(value, validate);
          Disposables.Add(bv);
          return bv;
      }

      public System.Threading.Tasks.Task PushAsync(Xamarin.Forms.Page page) => Xamarin.Forms.Application.Current.MainPage.Navigation.PushAsync(page);
      public System.Threading.Tasks.Task PopAsync() => Xamarin.Forms.Application.Current.MainPage.Navigation.PopAsync();
      public System.Threading.Tasks.Task PopToRootAsync() => Xamarin.Forms.Application.Current.MainPage.Navigation.PopToRootAsync();

      public System.Threading.Tasks.Task PushModalAsync(Xamarin.Forms.Page page) => Xamarin.Forms.Application.Current.MainPage.Navigation.PushModalAsync(page);
      public System.Threading.Tasks.Task PopModalAsync() => Xamarin.Forms.Application.Current.MainPage.Navigation.PopModalAsync();
  }
");

            AmmyPage.Generate(builder);
            Extensions.Generate(builder);

            builder.AppendLine("}");
            
            return builder.ToString();
        }
    }
}