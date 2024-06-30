// Ignore Spelling: Metadata

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
                    try
                    {
                        // Debug.Assert(false);

                        (string moduleName, // module name might be needed for extraction of the XML doc from the assembly
                            ISymbol symbol) = cntx.TargetSymbol.GetAsyncExternalInfo();

                        if (symbol is null)
                            return null;

                        var models = new List<ExternalModel>();

                        foreach (AttributeData data in cntx.Attributes)
                        {
                            // if we try to reconstruct AsyncExternalAttribute.Type then we would need top load its all
                            // dependencies so using `ISymbol symbol` from the prev call instead

                            var attr = new AsyncExternalAttribute(null);

                            string argValue = (data.GetAttributeNamedArgValue("Interface") ??
                                               data.GetAttributeNamedArgValue(typeof(Interface).FullName))
                                               ?? attr.Interface.ToString();

                            attr.Interface = argValue.EnumParse<Interface>();

                            argValue = (data.GetAttributeNamedArgValue(nameof(AsyncExternalAttribute.Methods)) ??
                                        data.GetAttributeNamedArgValue("".GetType().FullName))?.Trim('"')
                                        ?? attr.Methods;

                            attr.Methods = argValue;

                            Log.WriteLine($"External: {attr.Interface}, {attr.Methods}");

                            // To be used later for extracting the XML doc

                            // dynamic model = cntx.SemanticModel;
                            // var refs = model.Compilation.References as IEnumerable<dynamic>;
                            // var asmsFiles = refs.Select(x => x.FilePath).Cast<string>().ToList();
                            // var asmPath = asmsFiles.FirstOrDefault(x => x.EndsWith(moduleName));

                            models.Add(new ExternalModel
                            {
                                Attribute = attr,
                                TypeName = symbol.ToString(),
                                FilePath = cntx.TargetNode.SyntaxTree.FilePath,
                                TypeSymbol = symbol
                            });
                        }

                        return models;
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                        return null;
                    }
                });

            context.RegisterSourceOutput(asmPipeline, (cntx, models) =>
            {
                if (models == null)
                    return;

                try
                {
                    // AsyncExternalAttribute is the only attribute that can be applied multiple times
                    // So it is expected that we may have to process the same type more than once.
                    // Thus the output files need to be named properly to avoid file name duplications.
                    var alreadyAddedSources = new List<string>();

                    // Debug.Assert(false);

                    foreach (var model in models)
                    {
                        string externalTypeDefinition = model.TypeSymbol.Reconstruct();

                        var newCode = externalTypeDefinition.GenerateExtraCodeForExternalType(model.Attribute);

                        var fileId = $"{Path.GetFileNameWithoutExtension(model.FilePath)}-{model.TypeName}";
                        var count = alreadyAddedSources.Count(x => x.StartsWith(fileId));

                        var file = $"{fileId}.g.cs";
                        if (count > 0)
                            file = $"{fileId}.{++count}.g.cs";

                        var source = SourceText.From(newCode, Encoding.UTF8);
                        cntx.AddSource(file, source);

                        alreadyAddedSources.Add(file);

                        Log.WriteLine($"External: ouptut {file}");
                    }
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
                    // if (cntx.TargetSymbol.Name?.Contains("NumberService_EM_Sync") == true)
                    //     Debug.Assert(false);

                    var attr = new AsyncAttribute();

                    (attr.Algorithm, attr.Interface) = cntx.TargetSymbol.GetAsyncInfo();

                    Log.WriteLine($"Local: {attr.Algorithm}, {attr.Interface}");
                    try
                    {
                        return new LocalModel
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
                    // if (model?.TypeName?.Contains("NumberService_EM_Sync") == true)
                    //     Debug.Assert(false);

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
        // if (message.Contains("not set"))
        //     Debug.Assert(false);

        message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => $"asyncit> {x}")
            .ToList()
            .ForEach(x => Debug.WriteLine(x));
    }
}