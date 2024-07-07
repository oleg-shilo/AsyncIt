using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncIt;
using ClientServices;

// generating sync equivalent for all async methods found in a type
[assembly: AsyncExternal(typeof(HttpClient), Interface.Sync)]

// generating async equivalent for all sync methods found in a type
[assembly: AsyncExternal(typeof(DirectoryInfo), Interface.Async)]

// generating async/sync equivalent for all methods found in a type
[assembly: AsyncExternal(typeof(Downloader), Interface.Full)]

// generating sync methods for multiple async methods matching the specified names
// [assembly: AsyncExternal(Type = typeof(HttpClient), Interface = Interface.Sync, Methods = "GetByteArrayAsync,GetStreamAsync")]

// generating sync methods for multiple specific methods; Using nameof() to avoid typos.
// [assembly: AsyncExternal(typeof(HttpClient), Interface.Sync,
//     $"{nameof(HttpClient.GetStringAsync)}, " +
//     $"{nameof(HttpClient.GetStreamAsync)}, " +
//     $"{nameof(HttpClient.GetStringAsync)}, " +
//     $"{nameof(HttpClient.GetByteArrayAsync)}")]

// generating sync methods for a single specific method
// [assembly: AsyncExternal(typeof(HttpClient), Interface.Sync, "GetStringAsync")]

// generating sync methods for a single specific method (alternative attribute syntax)
// [assembly: AsyncExternal(Type = typeof(HttpClient), Interface = Interface.Sync, Methods = "GetStringAsync")]

namespace ConsoleApp;

partial class Program
{
    static async Task Main(string[] args)
    {
        var fileContent = await new Downloader().DownloadFileAsync(url: "https://www.google.com");

        var info = new DirectoryInfo(@".\");
        var dirs = await info.GetDirectoriesAsync("*", SearchOption.AllDirectories);
        foreach (var dir in dirs)
        {
            Console.WriteLine(dir);
        }

        HttpClient client = new();
        var html = client.GetString("https://www.google.com");

        var service = new OrderService();
        var order = await service.GetOrderAsync(1);

        var svc = new AccountService();
        var result = await svc.GetAccountAsync(1);
    }
}

public class Account
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

[Async(Algorithm.ExtensionMethods)]
partial class AccountService
{
    public Account GetAccount(int id)
    {
        Task.Delay(3000).Wait();
        return new Account
        {
            Id = id,
            Name = "User Name",
        };
    }
}

class Order
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Date { get; set; }
    public string? Status { get; set; }
}

class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

partial struct UserService
{
    public User GetUser<T>(int id)
    {
        Task.Delay(1000).Wait();
        return new User
        {
            Id = id,
            Name = "User Name",
        };
    }

    public User GetUser(string name) => new();

    public User GetUserAsync<T>(Dictionary<string, Nullable<int>> id, string name) => GetUser<T>(1);

    static public List<T> GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) => new();

    static public Task<List<T>> GetOrderAsync<T, T2>(Dictionary<string, Nullable<int>> id, string name)
        => Task.Run(() => GetOrder<T, T2>(id, name));

    async static Task InvokeAsync()
    {
        await GetOrderAsync<int, string>(new(), "name");
    }
}

[Async]
partial class OrderService
{
    public Order GetOrder(int id)
    {
        Task.Delay(1000).Wait();
        return new Order
        {
            Id = id,
            Name = "Order Name",
            Date = "Order Date",
            Status = "Order Status"
        };
    }

    public Order GetOrder(string name)
    {
        Task.Delay(1000).Wait();
        return new Order
        {
            Id = 1,
            Name = name,
            Date = "Order Date",
            Status = "Order Status"
        };
    }

    public Order GetOrder(DateTime date)
    {
        Task.Delay(1000).Wait();
        return new Order
        {
            Id = 1,
            Name = "Order Name",
            Date = date.ToShortDateString(),
            Status = "Order Status"
        };
    }

    public Order GetOrder(bool status)
    {
        Task.Delay(1000).Wait();
        return new Order
        {
            Id = 1,
            Name = "Order Name",
            Date = "Order Date",
            Status = status ? "Completed" : "Pending"
        };
    }

    public Order GetOrder(int Id, string name, DateTime date, bool status)
    {
        Task.Delay(1000).Wait();
        return new Order
        {
            Id = Id,
            Name = name,
            Date = date.ToShortDateString(),
            Status = status ? "Completed" : "Pending"
        };
    }
}