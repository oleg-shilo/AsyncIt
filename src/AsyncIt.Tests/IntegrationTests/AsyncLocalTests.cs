using System;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncIt.Tests.IntegrationTests;

public class AsyncLocalTests
{


    [Fact]
    public void GeneratePartialTypeForNestedTypeWithNestedNamespace()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            using System;

            namespace MyCompany;

            namespace Banking
            {
                public partial class BankService
                {
                    [Async]
                    public partial class OrderService
                    {
                        static public Order GetOrder(int id) => null;
                    }
                }
            }
            """);

        var newCode = code.GenerateSourceForTypes();

        Assert.Equal(
            """
            // <auto-generated/>
            using System;
            namespace MyCompany
            {
                namespace Banking
                {
                    public partial class BankService
                    {
                        public partial class OrderService
                        {
                            static public Task<Order> GetOrderAsync(int id)
                                => Task.Run(() => GetOrder(id));
                        }
                    }
                }
            }
            """, newCode.First().Value);
    }

    [Fact]
    public void GeneratePartialTypeForTypeWithGeneticParams()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            partial class OrderService<T>
            {
                static public    List<T> GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes();

        Assert.Equal(
            """
            // <auto-generated/>
            partial class OrderService<T>
            {
                static public Task<List<T>> GetOrderAsync<T, T2>(Dictionary<string, Nullable<int>> id, string name)
                    => Task.Run(() => GetOrder<T, T2>(id, name));
            }
            """, newCode.First().Value);
    }

    [Fact]
    public void GeneratePartialTypeForVoidMethods()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            partial class OrderService
            {
                static public void GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) {};
            }
            """);

        var newCode = code.GenerateSourceForTypes();

        Assert.Equal(
            """
            // <auto-generated/>
            partial class OrderService
            {
                static public Task GetOrderAsync<T, T2>(Dictionary<string, Nullable<int>> id, string name)
                    => Task.Run(() => GetOrder<T, T2>(id, name));
            }
            """, newCode.First().Value);
    }




    [Fact]
    public void GeneratePartialTypeForPublicType()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            public partial struct OrderService
            {
                static public Order GetOrder(int id) => null;
                static public User GetUser(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes().First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            public partial struct OrderService
            {
                static public Task<Order> GetOrderAsync(int id)
                    => Task.Run(() => GetOrder(id));

                static public Task<User> GetUserAsync(int id)
                    => Task.Run(() => GetUser(id));
            }
            """, newCode);
    }



    [Fact]
    public void GenerateExtensionMethodsForPublicType()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods };
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            public class OrderService
            {
                static public Order GetOrder(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            public static class OrderServiceExtensions
            {
                public static Task<Order> GetOrderAsync(this OrderService instance, int id)
                    => Task.Run(() => instance.GetOrder(id));
            }
            """, newCode);
    }


    [Fact]
    public void GenerateExtensionMethodsForInternalType()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods };
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            internal class OrderService
            {
                static public Order GetOrder(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            public static class OrderServiceExtensions
            {
                internal static Task<Order> GetOrderAsync(this OrderService instance, int id)
                    => Task.Run(() => instance.GetOrder(id));
            }
            """, newCode);
    }

    [Fact]
    public void GenerateExtensionMethodsForInternalType2()
    {
        var attr = new AsyncAttribute { Algorithm = Algorithm.ExtensionMethods };
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            class OrderService
            {
                static public Order GetOrder(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes(attr).First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            public static class OrderServiceExtensions
            {
                internal static Task<Order> GetOrderAsync(this OrderService instance, int id)
                    => Task.Run(() => instance.GetOrder(id));
            }
            """, newCode);
    }

    [Fact]
    public void GeneratePartialTypeForTypeWithExpandedAttribute()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async(Algorithm = Algorithm.Inheritance)]
            public partial struct OrderService
            {
                static public Order GetOrder(int id) => null;
                static public User GetUser(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes().First().Value;

        Assert.Equal(
            """
            // <auto-generated/>
            public partial struct OrderService
            {
                static public Task<Order> GetOrderAsync(int id)
                    => Task.Run(() => GetOrder(id));

                static public Task<User> GetUserAsync(int id)
                    => Task.Run(() => GetUser(id));
            }
            """, newCode);
    }
}
