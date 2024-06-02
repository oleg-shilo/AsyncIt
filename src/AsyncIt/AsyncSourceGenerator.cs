// Ignore Spelling: Metadata

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: InternalsVisibleTo("AsyncIt.Tests")]

// Reading
// One of the best about Async:
//    https://devblogs.microsoft.com/dotnet/configureawait-faq/
//   
// Source IncrementalGenerator:
//    https://andrewlock.net/exploring-dotnet-6-part-9-source-generator-updates-incremental-generators/
//    https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview

// TODO:
// [x] Async.Local.PartialClass
// [x] Sync.Local.PartialClass
// [x] Async.Local.ExtensionClass
// [x] Sync.Local.ExtensionClass
// [ ] Async.External.ExtensionClass
// [ ] Sync.External.ExtensionClass

// need to migrate to IIncrementalGenerator

namespace AsyncIt
{
    [Generator]
    public class AsyncExtensionsGenerator : IIncrementalGenerator
    {
        public void InitializeAssemblyGenerator(IncrementalGeneratorInitializationContext context)
        {
            var asmPipeline = context.SyntaxProvider.ForAttributeWithMetadataName(typeof(AsyncExternalAttribute).FullName,
                predicate: (node, cancellation) => true,
                transform: (cntx, cancellation) =>
                {
                    // Debug.Assert(false);

                    (string typeName,
                    string moduleName,
                    ISymbol symbol) = cntx.TargetSymbol.GetAsyncExternalInfo();
                    // var ttt = Reconstructions.GetSymbol(cntx.TargetNode, cntx.SemanticModel);

                    var sw = Stopwatch.StartNew();
                    var code = Syntaxer.Reconstruct(symbol);

                    var time = sw.Elapsed.ToString();

                    Log.WriteLine($"Reconstruction time: {time}");

                    dynamic model = cntx.SemanticModel;
                    var refs = model.Compilation.References as IEnumerable<dynamic>;
                    var asmsFiles = refs.Select(x => x.FilePath).Cast<string>().ToList();

                    var asmPath = asmsFiles.FirstOrDefault(x => x.EndsWith(moduleName));


                    return new Model
                    {
                        // SyntaxNode = cntx.TargetNode as TypeDeclarationSyntax
                    };
                });

            context.RegisterSourceOutput(asmPipeline, (cntx, model) =>
            {
                try
                {
                    // Debug.Assert(false);


                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            });
        }
        public void InitializeTypeGenerator(IncrementalGeneratorInitializationContext context)
        {

            var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(typeof(AsyncAttribute).FullName,
                predicate: (node, cancellation) => node is TypeDeclarationSyntax,
                transform: (cntx, cancellation) =>
                {
                    // Debug.Assert(false);

                    var attr = new AsyncAttribute();

                    (attr.Algorithm, attr.Interface) = cntx.TargetSymbol.GetAsyncInfo();

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

                    var result = model.SyntaxNode.GenerateExtraCodeForType(model.Attribute);
                    var file = $"{Path.GetFileNameWithoutExtension(model.FilePath)}.{model?.Namespace ?? "global"}.{model.TypeName}.g.cs";

                    cntx.AddSource(file, SourceText.From(result.code, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            });
        }
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Add the marker attribute as we do not share this assembly for referencing
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource("AsyncAttribute.g.cs", Properties.Resources.Attributes));

            InitializeAssemblyGenerator(context);
            InitializeTypeGenerator(context);
        }
    }
}

class Log
{
    public static void WriteLine(string message)
    {
        if (message.Contains("not set"))
            Debug.Assert(false);

        message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => $"async-me> {x}")
            .ToList()
            .ForEach(x => Debug.WriteLine(x));
    }
}