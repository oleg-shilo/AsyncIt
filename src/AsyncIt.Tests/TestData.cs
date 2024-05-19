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
