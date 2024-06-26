using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncIt.Tests.IntegrationTests;
public class Orchestration_Tests : TestBase
{
    [Fact]
    public void ShouldIgnorePrivateAndProtectedMethods()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            public partial struct OrderService
            {
                public Order GetOrder1(int id) => null;
                internal Order GetOrder2(int id) => null;
                private Order GetOrder3(int id) => null;
                protected Order GetOrder4(int id) => null;
                Order GetOrder5(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes().First().Value;

        Assert.Equal("""
            // <auto-generated/>

            public partial struct OrderService
            {
                public Task<Order> GetOrder1Async(int id)
                    => Task.Run(() => GetOrder1(id));

                internal Task<Order> GetOrder2Async(int id)
                    => Task.Run(() => GetOrder2(id));
            }
            """, newCode);
    }

    [Fact]
    public void ShouldIgnoreNonMarkedClasses()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            
            public partial struct OrderService1Ignorable
            {
                static public Order GetOrder(int id) => null;
                static public User GetUser(int id) => null;
            }

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
    public void ProcessModuleUsingStatements()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            using System.ComponentModel;
            using AsyncIt;
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
            using System.ComponentModel;
            using AsyncIt;

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
    public void ShouldIgnoreMarkedMethods()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            public partial struct OrderService
            {
                [Ignore, IgnoreAgain] [Sync(extension: true)]
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
                static public Task<User> GetUserAsync(int id)
                    => Task.Run(() => GetUser(id));
            }
            """, newCode);
    }

    [Fact]
    public void ShouldGenerateXmlDocForGenericExtendedMethod()
    {
        var method = new MethodMetadata() { Name = "Post" };
        var type = new TypeMetadata() { Name = "Http" };

        type.GenericParameters = "<T1, T2>";
        method.GenericParameters = "<T3, T4>";
        method.Parameters = "(T1 arg1, T2 arg2, T3 arg3, T4 arg4)";
        method.ParametersNames = "(arg1, arg2, arg3, arg4)";

        var xml = CodeGenerator.GenerateXmlDoc("synchronous", method, type, force: true);

        Assert.Equal("""
            /// <summary>
            /// The synchronous version of <see cref="Http{T1, T2}.Post{T3, T4}(T1, T2, T3, T4)"/>.
            /// </summary>
            
            """.Replace("\r\n", "\n"),
            xml);

    }
    [Fact]
    public void ShouldGenerateXmlDocForSimpleExtendedMethod()
    {
        var method = new MethodMetadata() { Name = "Post" };
        var type = new TypeMetadata() { Name = "Http" };

        var xml = CodeGenerator.GenerateXmlDoc("asynchronous", method, type, force: true);

        Assert.Equal("""
            /// <summary>
            /// The asynchronous version of <see cref="Http.Post"/>.
            /// </summary>
            
            """.Replace("\r\n", "\n"),
            xml);
    }

    [Fact]
    public void ShouldRaiseErrorIfClassIsNotPartial()
    {
        SyntaxTree code = CSharpSyntaxTree.ParseText("""
            [Async]
            public class OrderService
            {
                static public Order GetOrder(int id) => null;
                static public User GetUser(int id) => null;
            }
            """);

        var newCode = code.GenerateSourceForTypes().First().Value.Split('\n', '\r');
        Assert.True(newCode.Any(x => x.StartsWith("#error")) == true);
    }
}
