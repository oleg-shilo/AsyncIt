using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;

namespace AsyncIt.Tests.IntegrationTests;

public class LocalTypes_Sync_Tests : TestBase
{

    [Fact]
    public void GenerateExtensionMethodForMethodNamedWithAsync()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Sync };

        var apiCode = """
            using System;
            using AsyncIt;

            namespace AsyncIt.Tests;
            
            [Async]
            public class OrderService
            {
                public Task<Order> GetOrderAsync(int id) => null;
            }
            """;

        var expectedGeneratedApiCode = """
            // <auto-generated/>
            using System;
            using AsyncIt;

            namespace AsyncIt.Tests
            {
                public static class OrderServiceExtensions
                {
                    public static Order GetOrder(this OrderService instance, int id)
                        => instance.GetOrderAsync(id).Result;
                }
            }
            """;

        var appCode = """
            using System;
            using AsyncIt.Tests;

            var service = new OrderService();
            var order = service.GetOrder(1);
            """;

        var generatedApiCode = apiCode.GenerateExtraSource(attr);

        Assert.Equal(expectedGeneratedApiCode, generatedApiCode);


        var (exitCode, output) = BuildWithDotNet(appCode, apiCode, generatedApiCode);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void GenerateExtensionMethodForMethodNamedWithoutAsync()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Sync };

        var apiCode = """
            using System;
            using AsyncIt;

            namespace AsyncIt.Tests;
            [Async]
            public class OrderService
            {
                public Task<Order> GetOrder(int id) => null;
            }
            """;

        var expectedGeneratedApiCode = """
            // <auto-generated/>
            using System;
            using AsyncIt;

            namespace AsyncIt.Tests
            {
                public static class OrderServiceExtensions
                {
                    public static Order GetOrderSync(this OrderService instance, int id)
                        => instance.GetOrder(id).Result;
                }
            }
            """;

        var appCode = """
            using System;
            using AsyncIt.Tests;

            var service = new OrderService();
            var order = service.GetOrderSync(1);
            """;

        var generatedApiCode = apiCode.GenerateExtraSource(attr);

        Assert.Equal(expectedGeneratedApiCode, generatedApiCode);


        var (exitCode, output) = BuildWithDotNet(appCode, apiCode, generatedApiCode);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void GenerateExtensionMethodsForPublicType()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Sync };
        var code = """
            using System;
            using AsyncIt;

            namespace AsyncIt.Tests;
            
            [Async]
            public class OrderService
            {
                public Task<Order> GetOrderAsync(int id) => null;
                public Task<User> GetUserAsync(int id) => null;
            }
            """;


        var expected = """
            // <auto-generated/>
            using System;
            using AsyncIt;

            namespace AsyncIt.Tests
            {
                public static class OrderServiceExtensions
                {
                    public static Order GetOrder(this OrderService instance, int id)
                        => instance.GetOrderAsync(id).Result;

                    public static User GetUser(this OrderService instance, int id)
                        => instance.GetUserAsync(id).Result;
                }
            }
            """;

        var appCode = """
            using System;
            using AsyncIt.Tests;

            var service = new OrderService();

            var order = service.GetOrder(1);
            var user = service.GetUser(1);
            """;

        var generatedCode = code.GenerateExtraSource(attr);
        Assert.Equal(expected, generatedCode);

        var (exitCode, output) = BuildWithDotNet(appCode, code, generatedCode);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void GenerateSyncExtensionMethodsWithGeneticParams()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Sync };

        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            class OrderService<T1, T2> where T1: class, new()
            {
                static public async Task<List<T>> GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) 
                   where T: class, new() 
                => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            public static class OrderServiceExtensions
            {
                internal static List<T> GetOrderSync<T, T1, T2>(this OrderService<T1, T2> instance, Dictionary<string, Nullable<int>> id, string name) where T1: class, new() where T: class, new()
                    => instance.GetOrder<T, T2>(id, name).Result;
            }
            """, newCode);
    }

    [Fact]
    public void GenerateExtensionMethodsWithVoidMethod()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Sync };
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            public class OrderService
            {
                static public Task GetOrderAsync(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            public static class OrderServiceExtensions
            {
                public static void GetOrder(this OrderService instance, int id)
                    => instance.GetOrderAsync(id).Wait();
            }
            """, newCode);
    }


    [Fact]
    public void GeneratePartialTypeForPublicType()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.PartialType, Interface = Interface.Sync };
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            partial public class OrderService
            {
                static public Task<Order> GetOrderAsync(int id) => null;
                static public Task<User> GetUserAsync(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            partial public class OrderService
            {
                static public Order GetOrder(int id)
                    => GetOrderAsync(id).Result;

                static public User GetUser(int id)
                    => GetUserAsync(id).Result;
            }
            """, newCode);
    }

    [Fact]
    public void GeneratePartialTypeForMethodNamedWithAsync()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.PartialType, Interface = Interface.Sync };
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            partial public class OrderService
            {
                static public async Task<Order> GetOrderAsync(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            partial public class OrderService
            {
                static public Order GetOrder(int id)
                    => GetOrderAsync(id).Result;
            }
            """, newCode);
    }

    [Fact]
    public void GeneratePartialTypeForMethodNamedWithoutAsync()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.PartialType, Interface = Interface.Sync };
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            partial public class OrderService
            {
                static public Task<Order> GetOrder(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            partial public class OrderService
            {
                static public Order GetOrderSync(int id)
                    => GetOrder(id).Result;
            }
            """, newCode);
    }

    [Fact]
    public void GeneratePartialTypeWithGeneticParams()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.PartialType, Interface = Interface.Sync };

        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            partial class OrderService<T1, T2> where T1: class, new()
            {
                static public async Task<List<T>> GetOrder<T, T1>(Dictionary<string, Nullable<int>> id, string name) 
                    where T: class, new()
                => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            partial class OrderService<T1, T2> where T1: class, new()
            {
                static public List<T> GetOrderSync<T, T1>(Dictionary<string, Nullable<int>> id, string name) where T: class, new()
                    => GetOrder<T, T1>(id, name).Result;
            }
            """, newCode);
    }

    [Fact]
    public void GeneratePartialTypeWithVoidMethods()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.PartialType, Interface = Interface.Sync };

        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            partial class OrderService
            {
                static public Task GetOrderAsync<T, T2>(Dictionary<string, Nullable<int>> id, string name) {};
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr);

        Assert.Equal(
            """
            // <auto-generated/>
            partial class OrderService
            {
                static public void GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name)
                    => GetOrderAsync<T, T2>(id, name).Wait();
            }
            """, newCode.First().Value);
    }
}

