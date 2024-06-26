# AsyncIt

AsyncIt is a NuGet package library that allows the automatic generation of synchronous and asynchronous APIs.

It aims to extend user-defined CLR types by automating otherwise manual definitions of reparative and straightforward routines. Thus the development and the consumption of the released API are simplified due to the balanced (close to ideal) ratio of the synchronous and asynchronous API endpoints:

&nbsp;&nbsp;&nbsp;_**Every functionality point has both Async and Sync API endpoints available.**_

This content is an extract of the project's main [Wiki page](https://github.com/oleg-shilo/AsyncIt/wiki). It is highly recommended that you read it as it explains the deep reasons behind this project as well as details of the more concrete usage scenarios.

## Background

AsyncIt is a source generator that is integrated into the .NET build process as a special tool type called "Analyzer". It is invoked by the compiler during the building of the assembly and allows the injection of missing API endpoints based on the present API. Thus if the assembly being built has `GetStatus` but not `GetStatusAsync` then AsyncIt will generate the missing method with a straightforward implementation. Or it can generate the synchronous API if it is not present in the original codebase:

- The API defines synchronous methods only:

  _Original code_

  ```C#
  public partial class DeviceLib
  {
      public static string GetStatus() {. . .}
  }
  ```

  _Code that is fed to the C# compiler_

  ```C#
  public partial class DeviceLib
  {
      public static string GetStatus() {. . .}
  }

  public partial class DeviceLib // AsyncIt generated
  {
      public static Task<string> GetStatusAsync() 
          => TaskRun(() => GetStatus());
  }
  ```

AsyncIt does not do anything fancy. Similar to the `await` keyword it cannot magically convert a synchronous routine into an asynchronous one and vice versa. It simply emits the code that the developer would type manually if he/she decides to use the API the concurrency way that the API author did not participate. 

This is where AsyncIt is placed in the overall .NET concurrency model architecture: 

![image](https://github.com/oleg-shilo/AsyncIt/assets/16729806/dec186b7-706b-4aee-817b-9e7472c46fc9)

## Usage

In order to integrate AsyncIt with your .NET project just add AsyncIt Nuget package. 

```ps
dotnet add package AsyncIt --version 1.0.0-pre
```

That's it. Now you can mark any type you want to generate async/sync methods for, with the `[Async]` attribute (see the details below) and the new source code will be generated and included in the build. 

You can always inspect the generated code in the Visual Studio solution explorer:   

![image](https://github.com/oleg-shilo/AsyncIt/assets/16729806/fabed4b6-3eec-4421-a293-ed10fad4a950)

###  Extending user-defined types

In this scenario, a user type containing sync/async methods is extended by additional source file(s) implementing additional/missing API methods.
The type can be extended either with an additional partial class definition or by the extension methods class.

A typical usage can be illustrated by the code below.

_Async scenario:_
 
```C#
[Async]
public partial class BankService
{
    public partial class OrderService
    {
        public Order GetOrder(int id)
        {...}
    }
}
...
async Task OnButtonClick(object sender, EventArgs args)
{
    Order order = await service.GetOrderAsync(this.OrderId);
    orderLabel.Text = order.Name;
}
```

_Sync scenario:_

```c#
[Async(Interface = Interface.Sync)]
partial class AccountService
{
    public async Task<Account> GetAccountAsync(int id)
    {...}
}
...

static void Main()
{
   var account = new AccountService().GetAccount(333);
   
   File.WriteAllText($"account_{account.Id}.txt", account.Balance.ToString());
}
```

###  Extending external types

_**NOTE: this is still a pending feature scheduled for future release(s), thus the API may change.**_

In this scenario, a user type containing sync/async methods is extended by additional source file(s) implementing additional/missing API methods.
The type can be extended either with an additional partial class definition or by the extension methods class.

A typical usage can be illustrated by the code below.

_Async scenario:_
 
```C#
[AsyncExternal(Assembly = "System.IO", Type="System.IO.Directory")];
...
async Task OnButtonClick(object sender, EventArgs args)
{
    string[] folders = await Directory.GetDirectoriesAsync(workingDir);
    foreach(var path in folders)
      foldersListBox.Add(path);
}
```

_Sync scenario:_

```c#
[AsyncExternal(Assembly = "System.Net", Type="System.Net.Http.HttpClient", Interface = Interface.Sync)];

...

static void Main() 
    => File.WriteAllText(
           "temperature.txt", 
           new HttpClient().GetString("https://www.weather.com/temperature"));
```
