// Ignore Spelling: Metadata

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: InternalsVisibleTo("AsyncIt.Tests")]

// Reading
// One of the best about Async: https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
// Source IncrementalGenerator https://andrewlock.net/exploring-dotnet-6-part-9-source-generator-updates-incremental-generators/

// TODO:
// [x] Async.Local.PartialClass
// [ ] Sync.Local.PartialClass
// [x] Async.Local.ExtensionClass
// [ ] Sync.Local.ExtensionClass
// [ ] Async.External.ExtensionClass
// [ ] Sync.External.ExtensionClass

// need to migrate to IIncrementalGenerator

namespace AsyncIt
{
    [Generator]
    public class AsyncExtensionsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Add the marker attribute as we do not share this assembly for referencing
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource("AsyncAttribute.g.cs", Properties.Resources.Attributes));

            // prepare a filter and a transform delegate types that are marked with the `Async` attribute
            var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName("AsyncIt.AsyncAttribute",
                predicate: (node, cancellation) => node is TypeDeclarationSyntax,
                transform: (cntx, cancellation) =>
                {
                    // Debug.Assert(false);

                    var attrArguments = cntx.TargetSymbol.GetAttributes().First(x => x.AttributeClass.Name == "AsyncAttribute").NamedArguments;

                    var attr = new AsyncAttribute();
                    attr.Algorithm = attrArguments.Arg("Algorithm").EnumParse<Algorithm>();
                    attr.Interface = attrArguments.Arg("Interface").EnumParse<Interface>();

                    Log.WriteLine($"{attr.Algorithm}, {attr.Interface}");
                    try
                    {
                        return new Model
                        {
                            Namespace = cntx.TargetSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
                            Attribute = attr,
                            TypeName = cntx.TargetSymbol.Name,
                            FilePath = cntx.TargetNode.SyntaxTree.FilePath,
                            SyntaxNode = cntx.TargetNode as TypeDeclarationSyntax
                        };
                    }
                    catch
                    {
                        return null;
                    }
                });

            // register the generated source delegate
            context.RegisterSourceOutput(pipeline, (cntx, model) =>
            {
                try
                {
                    // Debug.Assert(false);

                    var result = model.SyntaxNode.GenerateExtensionSource(model.Attribute);
                    var file = $"{Path.GetFileNameWithoutExtension(model.FilePath)}.{model?.Namespace ?? "global"}.{model.TypeName}.g.cs";

                    cntx.AddSource(file, SourceText.From(result.code, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.Message);
                }
            });
        }
    }
}

class Log
{
    public static void WriteLine(string message)
    {
        message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => $"async-me> {x}")
            .ToList()
            .ForEach(x => Debug.WriteLine(x));
    }
}