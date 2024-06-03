using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncIt.Tests.SystemTests;
public class System_Tests : TestBase
{
    [Fact]
    public void ShouldIgnorePrivateAndProtectedMethods()
    {
        CreateProjectFile("test.csproj");
        SaveFile(asyncAsm);
        // SaveFile(thisAsm);

        SaveExtendableCode("OrderService.cs", """
            using System;
            using System.Diagnostics;
            using System.Net;
            using System.Xml.Linq;
            using AsyncIt;

            namespace AsyncIt.Tests;

            [Async]
            public partial class OrderService
            {
                public Order GetOrder1(int id) => null;
                internal Order GetOrder2(int id) => null;
                private Order GetOrder3(int id) => null;
                protected Order GetOrder4(int id) => null;
                Order GetOrder5(int id) => null;
            }
            """);

        SaveCodeFile("Program.cs", """
            using System;
            using AsyncIt.Tests;


            partial class Program
            {
                static async Task Main()
                {
                    OrderService service = new();
                    var order = await service.GetOrder1Async(1);
                }
            }
            """);

        SaveCodeFile("Order.cs", """
            using System;
            using AsyncIt.Tests;

            public class Order
            {
                public int Id { get; set; }
                public string? Name { get; set; }
                public string? Date { get; set; }
                public string? Status { get; set; }
            }
            """);

        (var exitCode, var output) = ExecuteBackgroundProcess("dotnet.exe", "build");

        Assert.Equal(0, exitCode);

    }
}