# AsyncIt

AsyncIt is a NuGet package library that allows the automatic generation of synchronous and asynchronous APIs.

It aims to extend user-defined CLR types by automating otherwise manual definitions of reparative and straightforward routines. Thus the development and the consumption of the released API are simplified due to the balanced (close to ideal) ratio of the synchronous and asynchronous API endpoints:

&nbsp;&nbsp;&nbsp;_**Every functionality point has both Async and Sync API endpoints available.**_

This content is an extract of the project's main [Wiki page](https://github.com/oleg-shilo/AsyncIt/wiki). It is highly recommended that you read it as it explains the deep reasons behind this project as well as details of the more concrete usage scenarios.

## Background

AsyncIt is a source generator that is integrated into the .NET build process as a special tool type called "Analyzer". It is invoked by the compiler during the building the assembly and allow injection of of missing API end points based on the present API. Thus if the assembly being built has `GetStatus` but not `GetStatusAsync` then AsyncIt will generate the missing method with a straightforward implementation. Or it can generate the synchronous API if it is not present in the original codebase:

- Asynchronous API is not present.
  _Original code_

  ```C#
  public partial class DeviceLib
  {
      public static string GetStatus() {. . .}
  }
  ```

  _Code to be compiled_

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

### "Partial Class"

_Generating additional asynchronous methods for the given class:_

<table style="width:100%">
<tr><td> User Code</td> <td> Additional generated code </td></tr>
<tr><td> 

```C#
[Async]
public partial class BankService
{
    public partial class OrderService
    {
        public Order GetOrder(int id) 
        {...}
```

</td>
<td>

```C#
public partial class BankService
{
    public partial class OrderService
    {
        public Task<Order> GetOrderAsync(int id)
            => Task.Run(() => GetOrder(id));
```

</td>
</tr>
</table>

_Generating additional synchronous methods for the given class:_

<table style="width:100%">
<tr><td> User Code</td> <td> Additional generated code </td></tr>
<tr><td> 

```C#
[Async(Interface = Interface.Sync)]
partial class AccountService
{
    public async Task<Account> GetAccountAsync(int id)
    {...}
```

</td>
<td>

```C#
partial class AccountService
{
    public Account GetAccount(int id)
        => GetAccountAsync(id).Result;
```

</td>
</tr>
</table>

_Generating additional synchronous and asynchronous methods for the given class:_ 

<table style="width:100%">
<tr><td> User Code</td> <td> Additional generated code </td></tr>
<tr><td> 

```C#
[Async(Interface = Interface.Full)]
partial class BankService
{
    public async Task<Account> GetAccountAsync(int id)
    {...}
    public User GetUser(string name) 
    {...}
```

</td>
<td>

```C#
partial class BankService
{
    public Account GetAccount(int id)
        => GetAccountAsync(id).Result;

    public Task<User> GetUserAsync(string name)
        => Task.Run(() => GetUser(name));
```

</td>
</tr>
</table>

### "Extension Methods"

_Generating additional asynchronous methods for the given class:_

<table >
<tr><td> User Code</td> <td> Additional generated code </td></tr>
<tr><td> 

```C#
[Async(Algorithm = Algorithm.ExtensionMethods)]
public partial class BankService
{
    public class OrderService
    {
        public Order GetOrder(int id) 
        {...}
```

</td>
<td>

```C#
public static class BankServiceExtensions
{
    public static class OrderService
    {
        public static Task<Order> 
            GetOrderAsync(this OrderService service, int id)
                => Task.Run(() => service.GetOrder(id));
```

</td>
</tr>
</table>

_Generating additional synchronous methods for the given class:_ 

<table >
<tr><td> User Code</td> <td> Additional generated code </td></tr>
<tr><td> 

```C#
[Async(
    Algorithm = Algorithm.ExtensionMethods, 
    Interface = Interface.Sync)]
partial class AccountService
{
    public async Task<Account> GetAccountAsync(int id)
    {...}
```

</td>
<td>

```C#
public static class AccountServiceExtensions
{
    public static Account 
        GetAccount(this AccountService service, int id)
            => service.GetAccountAsync(id).Result;
```

</td>
</tr>
</table>

_Generating additional synchronous and asynchronous methods for the given class:_

<table style="width:100%">
<tr><td> User Code</td> <td> Additional generated code </td></tr>
<tr><td> 

```C#
[Async(
    Algorithm = Algorithm.ExtensionMethods, 
    Interface = Interface.Full)]
partial class BankService
{
    public async Task<Account> GetAccountAsync(int id)
    {...}
    public User GetUser(string name) 
    {...}
```

</td>
<td>

```C#
public static class BankServiceExtensions
{
    public static Account 
        GetAccount(this AccountService service, int id)
            => service.GetAccountAsync(id).Result;

    public Task<User> 
        GetUserAsync(this AccountService service, string name)
            => Task.Run(() => service.GetUser(name));
```

</td>
</tr>
</table>

###  Extending external types

_**NOTE: this is still a pending feature scheduled for future release(s), thus the API may change.**_

In this scenario, a user type containing sync/async methods is extended by additional source file(s) implementing additional/missing API methods.
The type can be extended either with an additional partial class definition or by the extension methods class.

A typical usage can be illustrated by the code below.

_Async scenario:_
 
```C#
[AsyncExternal(Name = "System.IO", Type="System.IO.Directory")];
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
[AsyncExternal(Name = "System.Net", Type="System.Net.Http.HttpClient", Interface = Interface.Sync)];

...

static void Main() 
    => File.WriteAllText(
           "temperature.txt", 
           new HttpClient().GetString("https://www.weather.com/temperature"));
```
