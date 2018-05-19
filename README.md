# **Clarity** is an application framework based on Xamarin Forms

## **Main values**

* **Code should be readable.** As few boilerplate as possible. If there is code then it should actually affect the logic of your application. 
* **Code should be reusable.** Do not repeat yourself, make sure to separate common patterns for later reuse. Everything should be reusable. Compare that to XAML where you often see same XML blocks with slightly different values.
* **Code should be local.** The function that saves your entity to the database should be easily accessible from the Button declaration that causes this save.

## **Hello World example**

```cs
public class App : Application {
    public App() => MainPage = new Calculator().MainPage();
}

public class HelloWorld : ClarityBase
{
    public ContentPage MainPage()
    {
        var currentValue = CreateBindableValue("World");

        return ContentPage.Content(
                    StackLayout.Children(
                        Entry.Text(currentValue),
                        Label.Text(currentValue, v => "Hello, " + v)
                    )
                );
    }
}
```
![preview](https://raw.githubusercontent.com/ionoy/Clarity/master/content/helloworld.gif?token=AAkK8AJFMdQZyigKDTrjVUtbYFJz0NcIks5bCUfAwA%3D%3D)

As you can see, both **data** and **presentation** reside in the same method. But don't get your pitchforks up - **IT'S OKAY**. Your **data** still doesn't know anything about the **view** and most of the communication is still done with binding. But now it's much easier to navigate between the view and the code. 

## **Won't it become unreadable very very soon?**

If your method starts growing too fast, you can always abstract stuff away. Like this.

```cs
    public ContentPage MainPage()
    {
        return ContentPage.Content(
                    StackLayout.Children(
                        Header("My application name"),
                        Label.Text("Content"),
                    )
                );
    }

    public Grid Header(string applicationName) 
    {
        return Grid.Children(
            Label.Text(applicationName)
        )
    }
```

Both methods can have separate data and logic. 

## **Commands and interaction**

You can pass labmda function to the `Command` extension method and it will be executed on click.

```cs
    public ContentPage MainPage()
    {
        var counter = CreateBindableValue(0);

        // define local function for Button's Command
        void increment() => counter.Value++;

        return ContentPage.Content(
                    StackLayout.Children(
                        Label.Text(counter),
                        Button.Text("Increment")
                              .Command(increment)
                    )
                );
    }
```

You can still use the good old `ICommand` interface if you want, but you rarely need to.

![preview](https://raw.githubusercontent.com/ionoy/Clarity/master/content/increment.gif?token=AAkK8AJFMdQZyigKDTrjVUtbYFJz0NcIks5bCUfAwA%3D%3D)

## **Validation**

Validation is tied to `BindableValue<T>` and `BindableModel<T>`. 

```cs
    public ContentPage MainPage()
    {
        var summerMonth = CreateBindableValue("", validateSummerMonth);

        void validateSummerMonth(string month, Validator<string> validator) {
            if (month != "June" && month != "July" && month != "August")
                validator.AddError("Month can either be June, July or August");
        }

        return ContentPage.Content(
                    StackLayout.Children(
                        Entry.Text(summerMonth),
                        Label.Text(summerMonth.ValidationMessage)
                    )
                );
    }
```
![preview](https://raw.githubusercontent.com/ionoy/Clarity/master/content/validation.gif?token=AAkK8AJFMdQZyigKDTrjVUtbYFJz0NcIks5bCUfAwA%3D%3D)
