using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace AsyncIt.Tests;
public class HttpClientTest : Attribute, IDisposable
{
    public HttpClientTest() { }
    public HttpClientTest(HttpMessageHandler handler) { }
    public HttpClientTest(HttpMessageHandler handler, bool disposeHandler) { }

    public Uri? BaseAddress { get; set; }
    public static IWebProxy DefaultProxy { get; set; }
    public HttpRequestHeaders DefaultRequestHeaders { get; }

    public void CancelPendingRequests() { }
    public Task<HttpResponseMessage> DeleteAsync(string? requestUri, List<string>? items) { return null; }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

class OrderService<T>
{
    public async Task<List<T>> GetOrder<T2, T3>(Dictionary<string, Nullable<int>> id, string name) => null;
}
public static class OrderServiceExtensions
{
    internal static List<T> GetOrderSync<T2, T3, T>(this OrderService<T> instance, Dictionary<string, Nullable<int>> id, string name)
        => instance.GetOrder<T2, T3>(id, name).Result;
}

public class SimpleClassTest
{
    public void Post1(string arg1) { }
    public string Send1(string arg1, string arg2) { return ""; }

    public void Post2(string arg1) { }
    public string Send2(string arg1, string arg2) { return ""; }

    public Task Post1Async(string arg1) { return null; }
    public Task<string> Send1Async(string arg1, string arg2) { return Task.Run(() => ""); }
    public Task Post3Async(string arg1) { return null; }
    public Task<string> Send3Async(string arg1, string arg2) { return Task.Run(() => ""); }

}

public class MixedGenericClassTest<T1, T2> : Attribute where T1 : class, new()
{
    public void Post<T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4) { }
    public Task<T4> SendAsync<T3, T4>(T1 arg1, T2 arg2, T3 arg3) where T4 : new()
        => new Task<T4>(() => new T4());
}

public static class MixedGenericClassTestExtensions
{
    /// <summary>
    /// <see cref="MixedGenericClassTest{T1, T2}.Post{T3, T4}(T1, T2, T3, T4)"/>"/>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <param name="instance"></param>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    /// <param name="arg3"></param>
    /// <param name="arg4"></param>
    /// <returns></returns>
    public static Task PostAsync<T1, T2, T3, T4>(this MixedGenericClassTest<T1, T2> instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where T1 : class, new()
        => Task.Run(() => instance.Post<T3, T4>(arg1, arg2, arg3, arg4));
    public static T4 Send<T1, T2, T3, T4>(this MixedGenericClassTest<T1, T2> instance, T1 arg1, T2 arg2, T3 arg3) where T1 : class, new() where T4 : new()
        => instance.SendAsync<T3, T4>(arg1, arg2, arg3).Result;
}

public class GenericClassTest : Attribute
{
    public void Post<T1, T2>(T1 arg1) { }
    public Task<T1> SendAsync<T1, T2>(T1 arg1, T2 arg2) where T1 : class, new()
    { return Task.Run(() => new T1()); }
}

public static class GenericClassTestExtensions
{
    public static Task PostAsync<T1, T2>(this GenericClassTest instance, T1 arg1)
        => Task.Run(() => instance.Post<T1, T2>(arg1));

    public static T1 Send<T1, T2>(this GenericClassTest instance, T1 arg1, T2 arg2) where T1 : class, new()
        => instance.SendAsync<T1, T2>(arg1, arg2).Result;
}
