using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace AsyncIt.Tests.UnitTests;

public class ParsingTest
{
    [Fact]
    public void CanParseNestedTypeDeclaration()
    {
        var code = """
            public partial class OrderService
            {
                static public Order GetOrder(int id) => null;
            }
            """;

        var type = code.SyntaxNodes().OfType<TypeDeclarationSyntax>().First();

        var metadata = type.GetMetadata();

        Assert.Empty(metadata.Attributes);
        Assert.Equal("public partial class", metadata.Modifiers);
        Assert.Equal("OrderService", metadata.Name);
        Assert.Equal("", metadata.GenericParameters);
    }

    [Fact]
    public void CanParseTypeDeclaration()
    {
        var code = """
            public partial struct OrderService : BaseClass
            {
                static public Order GetOrder(int id) => null;
            }
            """;

        var type = code.SyntaxNodes().OfType<TypeDeclarationSyntax>().First();

        var metadata = type.GetMetadata();

        Assert.Empty(metadata.Attributes);
        Assert.Equal("public partial struct", metadata.Modifiers);
        Assert.Equal("OrderService", metadata.Name);
        Assert.Equal(": BaseClass", metadata.BaseList);
        Assert.Equal("", metadata.GenericParameters);
    }

    [Fact]
    public void CanParseTypeDeclarationWithGenericParams()
    {
        var code = """
            public partial class OrderService<T> where t: StringBuilder
            {
                static public Order GetOrder(int id) => null;
            }
            """;

        var type = code.SyntaxNodes().OfType<TypeDeclarationSyntax>().First();

        var metadata = type.GetMetadata();

        Assert.Empty(metadata.Attributes);
        Assert.Equal("public partial class", metadata.Modifiers);
        Assert.Equal("OrderService", metadata.Name);
        Assert.Equal("<T>", metadata.GenericParameters);
        Assert.Equal("where t: StringBuilder", metadata.GenericParametersConstraints);
    }

    [Fact]
    public void CanParseStructDeclaration()
    {
        var code = """
            public partial struct OrderService
            {
                static public Order GetOrder(int id) => null;
            }
            """;

        var type = code.SyntaxNodes().OfType<TypeDeclarationSyntax>().First();

        var metadata = type.GetMetadata();

        Assert.Empty(metadata.Attributes);
        Assert.Equal("public partial struct", metadata.Modifiers);
        Assert.Equal("OrderService", metadata.Name);
        Assert.Equal("", metadata.GenericParameters);
    }

    [Fact]
    public void CanParseTypeDeclarationWithNamespace()
    {
        var code = """
            namespace Networking
            {
                namespace Logging
                {
                }

                namespace Local
                {
                    public partial class OrderService<T> where T : class

                    {
                        static public    List<T> GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) => null;
                    }
                }
            }
            """;

        var type = code.SyntaxNodes().OfType<TypeDeclarationSyntax>().First();

        var metadata = type.GetMetadata();

        Assert.Empty(metadata.Attributes);
        Assert.Equal("public partial class", metadata.Modifiers);
        Assert.Equal("OrderService", metadata.Name);
        Assert.Equal("<T>", metadata.GenericParameters);
        Assert.Equal("where T : class", metadata.GenericParametersConstraints);

    }

    [Fact]
    public void CanParseTypeDeclarationWithFileScopeNamespace()
    {
        var code = """
            namespace Networking;

            namespace Logging
            {
            }

            namespace Local
            {
                public partial class OrderService<T> where T : class

                {
                    static public    List<T> GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) => null;
                }
            }
            """;

        var type = code.SyntaxNodes().OfType<TypeDeclarationSyntax>().First();

        var metadata = type.GetMetadata();

        Assert.Empty(metadata.Attributes);
        Assert.Equal("public partial class", metadata.Modifiers);
        Assert.Equal("OrderService", metadata.Name);
        Assert.Equal("<T>", metadata.GenericParameters);
        Assert.Equal("where T : class", metadata.GenericParametersConstraints);
    }

    [Fact]
    public void CanParseTypeDeclarationWithAttributes()
    {
        var code = """
            [Async]
            [Serialize, Model]
            public partial class OrderService<T> where T : class
            {
                static public    List<T> GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) => null;
            }
            """;

        var type = code.SyntaxNodes().OfType<TypeDeclarationSyntax>().First();

        var metadata = type.GetMetadata();

        Assert.Equal(3, metadata.Attributes.Count());
        Assert.Contains("Async", metadata.Attributes);
        Assert.Contains("Serialize", metadata.Attributes);
        Assert.Contains("Model", metadata.Attributes);
        Assert.Equal("public partial class", metadata.Modifiers);
        Assert.Equal("OrderService", metadata.Name);
        Assert.Equal("<T>", metadata.GenericParameters);
        Assert.Equal("where T : class", metadata.GenericParametersConstraints);
    }

    [Fact]
    public void CanParseGeneticParamsInMethods()
    {
        var code = """
            partial class OrderService
            {
                static public    List<T> GetOrder<T, T2>(Dictionary<string, Nullable<int>> id, string name) => null;
            }
            """;

        var metadata = code.SyntaxNodes().OfType<TypeDeclarationSyntax>()
                           .First()
                           .Members.OfType<MethodDeclarationSyntax>()
                           .First()
                           .GetMetadata();

        Assert.Equal("static public", metadata.Modifiers);
        Assert.Equal("List<T>", metadata.ReturnType);
        Assert.Equal("GetOrder", metadata.Name);
        Assert.Equal("<T, T2>", metadata.GenericParameters);
        Assert.Equal("(Dictionary<string, Nullable<int>> id, string name)", metadata.Parameters);
        Assert.Equal("(id, name)", metadata.ParametersNames);
    }

    [Fact]
    public void CanReconstructExternalType()
    {
        var code = """
        using System;
        using System.IO;
        using System.Net;
        using System.ComponentModel;
        using System.Runtime.CompilerServices;
        using AsyncIt;

        [assembly: AsyncExternal(typeof(Directory), Interface.Sync)]
        // [assembly: AsyncExternal(typeof(HttpClient))]
        """;

        var doc = code.ToCompiledDoc([
                typeof(object).Assembly.Location,
                typeof(AsyncExternalAttribute).Assembly.Location,
                // typeof(HttpClient).Assembly.Location,
                typeof(Directory).Assembly.Location]);


        ISymbol symbol = doc.SymbolAt(code.IndexOf("Directory") + 3);
        var directoryCode = symbol?.Reconstruct();

        // symbol = doc.SymbolAt(code.IndexOf("HttpClient") + 3);
        // var httpClientCode = symbol?.Reconstruct();

    }
}