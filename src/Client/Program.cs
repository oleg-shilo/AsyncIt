using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncIt;

//
// [assembly: AsyncExternal(typeof(HttpClient))]
[assembly: AsyncExternal(typeof(List<int>))]

// [assembly: AsyncExternal(typeof(Directory), Interface.Sync)]

namespace ConsoleApp;

static class HttpClientExtensions
{
    public static string GetString(this HttpClient client, string url)
        => client.GetStringAsync(url).Result;

    public static Task<string> GetStringConcurrent(this HttpClient client, string url)
        => Task.Run(() => client.GetString(url));
}

// [AsyncAsm(Class = typeof(HttpClient))]
partial class Program
{
    static async Task Main(string[] args)
    {
        var ttt = new List<List<int>>();

        HttpClient client = new();
        client.GetStringAsync("https://www.google.com").Wait();

        // OrderService service = new();
        // var order = await service.GetOrderAsync(1);
        Console.WriteLine("starting");

        var svc = new AccountService();
        var result = await svc.GetAccountAsync(1);
        Console.WriteLine("ending");

        // var svc1 = new AccountService1();
        // await svc1.GetAccountAsync(1);
        // Console.WriteLine("ending");

        // HelloFrom("Generated sdfsdfsdaCode");
    }

    static partial void HelloFrom(string name);
}

public class Account
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

[Async(Algorithm.ExtensionMethods, Interface.Sync)]
partial class NumberService_EM_Sync
{
    public async Task<int> GetNumberAsync(int id)
    {
        Task.Delay(500).Wait();
        return id;
    }
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

[Async(Algorithm.ExtensionMethods, Interface.Sync)]
partial class AccountService2
{
    public async Task<Account> GetAccountAsync(int id)
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

partial class OrderService
{
    public Task<Order> GetOrderAsync(int id) => new Task<Order>(() => this.GetOrder(id));
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

// [Async]
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