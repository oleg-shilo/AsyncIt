// Ignore Spelling: Metadata

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using AsyncIt;
using AsyncIt.Properties;
using System.Collections.ObjectModel;
using System.Collections;


static class Parser
{
    public static string GenerateSourceForExternalType(this string typeInfo, AsyncExternalAttribute attribute = default)
    {
        var code = GenerateExtraCodeForExternalType(typeInfo, attribute);
        return code;
    }

    public static Dictionary<string, string> GenerateSourceForTypes(this SyntaxTree syntaxTree, AsyncAttribute attribute = default)
    {
        var result = new Dictionary<string, string>();

        var types = syntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>();

        foreach (var type in types)
        {
            var info = GenerateExtraCodeForType(type, attribute ?? new AsyncAttribute());
            if (info.type != null)
                result.Add(info.type, info.code);
        }

        return result;
    }

    public static string GenerateExtraCodeForExternalType(this string type, AsyncExternalAttribute asmAttribute)
    {
        TypeMetadata typeMetadata = GetMethadataFromReconstructedCode(type);

        var code = new StringBuilder();

        code.AppendLine("// <auto-generated/>");

        foreach (var ns in typeMetadata.UsingNamespaces)
            code.AppendLine(ns);

        if (!typeMetadata.UsingNamespaces.Contains("using System.Threading.Tasks;"))
            code.AppendLine("using System.Threading.Tasks;");

        var indent = "";

        if (typeMetadata.Namespace.Any())
        {
            code.AppendLine()
                .AppendLine($"namespace {typeMetadata.Namespace}")
                .AppendLine("{");

            indent = "    ";
        }

        var attribute = new AsyncAttribute
        {
            Interface = asmAttribute.Interface,
            NamePattern = asmAttribute.Methods
        };

        if (asmAttribute.Type?.GenericTypeArguments?.Any() == true)
            attribute.TypeGenericArgs = $"<{asmAttribute.Type.GenericTypeArguments.Select(x => x.FullName).JoinBy(", ")}>";

        GenerateExtensionClass(attribute, code, typeMetadata, typeMetadata.Methods, indent);

        if (typeMetadata.Namespace.Any())
            code.AppendLine("}");

        var result = code.ToString().TrimEnd();

        return result;
    }

    public static (string type, string code) GenerateExtraCodeForType(this TypeDeclarationSyntax type, AsyncAttribute attribute)
    {
        var code = new StringBuilder();
        var typeMetadata = type.GetMetadata();
        var methodsMetadata = type.Members.OfType<MethodDeclarationSyntax>()
            .Select(x => x.GetMetadata())
            .Where(x => !x.Attributes.Any(y => y.Contains("Ignore")) &&
                        (x.Modifiers.Contains("public") || x.Modifiers.Contains("internal")));

        if (!typeMetadata.Attributes.Any(x => x.Contains("Async")))
            return (null, null);

        code.AppendLine("// <auto-generated/>");

        foreach (var ns in typeMetadata.UsingNamespaces)
            code.AppendLine(ns);
        code.AppendLine();

        (var header, var indent, var footer) = type.GenerateSourceSkeleton();


        if (header.Any())
            code.AppendLine(header);

        if (attribute.Algorithm == Algorithm.PartialType)
        {
            GeneratePartialType(attribute, code, typeMetadata, methodsMetadata, indent);
        }
        else
        {
            GenerateExtensionClass(attribute, code, typeMetadata, methodsMetadata, indent);
        }

        if (footer.Any())
            code.AppendLine(footer);

        return (typeMetadata.Name, code.ToString().TrimEnd());
    }

    static void GenerateExtensionClass(AsyncAttribute attribute, StringBuilder code, TypeMetadata typeMetadata, IEnumerable<MethodMetadata> methodsMetadata, string indent)
    {
        // typeMetadata.GenericParameters = attribute.TypeGenericArgs;

        code.AppendLine($"{indent}public static partial class {typeMetadata.Name}Extensions".TrimEnd())
                        .AppendLine($"{indent}{{");

        var methodsCode = new StringBuilder();
        foreach (MethodMetadata item in methodsMetadata)
        {
            if (!item.IsMatching(attribute.NamePattern))
                continue;

            var methodImplementation = "";
            switch (attribute.Interface)
            {
                case Interface.Async:
                    if (!item.IsAsync())
                        methodImplementation = item.GenerateAsyncExtensionMethod(typeMetadata);
                    break;
                case Interface.Sync:
                    if (item.IsAsync())
                        methodImplementation = item.GenerateSyncExtensionMethod(typeMetadata);
                    break;
                case Interface.Full:
                    methodImplementation = item.IsAsync()
                        ? item.GenerateSyncExtensionMethod(typeMetadata)
                        : item.GenerateAsyncExtensionMethod(typeMetadata);
                    break;
            }

            if (methodImplementation.Any())
            {
                // Log.WriteLine($"     {item.Name}{item.Parameters}");

                foreach (var line in methodImplementation.Split('\n'))
                    methodsCode.AppendLine($"{indent}{singleIndent}{line}");

                methodsCode.AppendLine();
            }
        }
        code.AppendLine(methodsCode.ToString().TrimEnd());

        code.AppendLine($"{indent}}}");
    }

    static void GeneratePartialType(AsyncAttribute attribute, StringBuilder code, TypeMetadata typeMetadata, IEnumerable<MethodMetadata> methodsMetadata, string indent)
    {
        if (!typeMetadata.Modifiers.Contains("partial"))
            code.AppendLine($"#error For extending {typeMetadata.Name} members with `{nameof(Algorithm.PartialType)}` algorithm " +
                $"you need to declared {typeMetadata.Name} as partial. " +
                $"Alternatively you can specify different algorithm (e.g. {nameof(Algorithm.ExtensionMethods)} in the " +
                $"`{nameof(AsyncAttribute)}` declaration). ");

        code.AppendLine($"{indent}{typeMetadata.Modifiers} {typeMetadata.Name}{typeMetadata.GenericParameters} {typeMetadata.GenericParametersConstraints}".TrimEnd())
            .AppendLine($"{indent}{{");

        var methodsCode = new StringBuilder();
        foreach (MethodMetadata item in methodsMetadata)
        {
            var methodImplementation = "";
            switch (attribute.Interface)
            {
                case Interface.Async:
                    methodImplementation = item.GenerateAsyncMethod(typeMetadata);
                    break;
                case Interface.Sync:
                    methodImplementation = item.GenerateSyncMethod(typeMetadata);
                    break;
                case Interface.Full:
                    methodImplementation = item.IsAsync()
                        ? item.GenerateSyncMethod(typeMetadata)
                        : item.GenerateAsyncMethod(typeMetadata);
                    break;
            }

            foreach (var line in methodImplementation.Split('\n'))
                methodsCode.AppendLine($"{indent}{singleIndent}{line}");

            methodsCode.AppendLine();
        }
        code.AppendLine(methodsCode.ToString().TrimEnd());

        code.AppendLine($"{indent}}}");
    }

    public static string[] GetParentNamespaces(this TypeDeclarationSyntax type)
    {
        var namespaces = new List<string>();

        var parent = type.Parent;

        while (parent != null)
        {
            if (parent is NamespaceDeclarationSyntax ns)
                namespaces.Add(ns.Name.ToString());
            else if (parent is FileScopedNamespaceDeclarationSyntax ns1)
                namespaces.Add(ns1.Name.ToString());

            parent = parent.Parent;
        }

        namespaces.Reverse();

        return namespaces.ToArray();
    }

    public static TypeMetadata GetMetadata(this TypeDeclarationSyntax type)
    {
        var usingNamespaces = type.SyntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(x => x.ToString())
                .ToArray();

        var tokens = type
            .ChildTokens()
            .Select(x => new { Text = x.ToString(), Element = (object)x, x.SpanStart })
            .Where(x => x.SpanStart < type.OpenBraceToken.SpanStart);

        var nodes = type
            .ChildNodes()
            .Select(x => new { Text = x.ToString(), Element = (object)x, x.SpanStart })
            .Where(x => x.SpanStart < type.OpenBraceToken.SpanStart);

        var all = nodes.Concat(tokens).OrderBy(x => x.SpanStart).ToList();

        var attributeList = all.TakeOutWhile(x => (x.Element is AttributeListSyntax)).Select(x => x.Element as AttributeListSyntax);
        var modifiers = all.TakeOutWhile(x => !x.Element.IsToken(SyntaxKind.IdentifierToken));
        var name = all.TakeOutFirst();
        var baseList = all.Select(x => x.Element).OfType<BaseListSyntax>().FirstOrDefault()?.ToString();
        var genericParameters = all.Select(x => x.Element).OfType<TypeParameterListSyntax>().FirstOrDefault()?.ToString() ?? "";
        var genericParametersConstraints = all.Select(x => x.Element)
                                              .OfType<TypeParameterConstraintClauseSyntax>()
                                              .Select(x => x.ToString())
                                              .JoinBy(" ");

        var metadata = new TypeMetadata();
        metadata.UsingNamespaces = usingNamespaces;
        metadata.Attributes = attributeList.GetAttributes();
        metadata.Modifiers = modifiers.Select(x => x.Text).JoinBy(" ");
        metadata.Name = name.Text ?? "";
        metadata.BaseList = baseList ?? "";
        metadata.GenericParameters = genericParameters ?? "";
        metadata.GenericParametersConstraints = genericParametersConstraints ?? "";

        return metadata;
    }

    internal static string[] GetAttributes(this IEnumerable<AttributeListSyntax> attributeList)
        => attributeList.SelectMany(x => x.Attributes).Select(x => x.ToFullString()).ToArray();


    internal static object[] GetAttributeNodes(this IEnumerable<AttributeListSyntax> attributeList)
        => attributeList.SelectMany(x => x.Attributes).ToArray();


    public static MethodMetadata GetMetadata(this MethodDeclarationSyntax method)
    {
        var tokens = method
            .ChildTokens()
            .Select(x => new { Text = x.ToString(), Element = (object)x, x.SpanStart }).ToArray();

        var nodes = method
            .ChildNodes()
            .TakeWhile(p => !(p is BlockSyntax) && !(p is ArrowExpressionClauseSyntax))
            .Select(x => new { Text = x.ToString(), Element = (object)x, x.SpanStart });

        var all = nodes.Concat(tokens).OrderBy(x => x.SpanStart).ToList();

        // use TakeOutWhile to keep track of taken items

        var attributeList = all.TakeOutWhile(x => (x.Element is AttributeListSyntax)).Select(x => x.Element as AttributeListSyntax);
        var modifiers = all.TakeOutWhile(x => x.Element is SyntaxToken);
        var retureType = all.TakeOutFirst();
        var signature = all.TakeOutWhile(x => !(x.Element is ParameterListSyntax));
        var parameters = all.TakeOutFirst();
        var genericParametersConstraints = all.Where(x => x.Element is TypeParameterConstraintClauseSyntax).Select(x => x.Text).JoinBy(" ");
        var invokParameters = (parameters.Element as ParameterListSyntax).Parameters
                                                                         .Select(x => x.GetLastToken().Text);

        var metadata = new MethodMetadata();

        metadata.Attributes = attributeList.GetAttributes();
        metadata.Modifiers = modifiers.Select(x => x.Text).JoinBy(" ") ?? "";
        metadata.ReturnType = retureType.Text ?? "";
        metadata.Name = signature.FirstOrDefault()?.Text ?? "";
        metadata.GenericParameters = signature.Skip(1).FirstOrDefault()?.Text ?? "";
        metadata.GenericParametersConstraints = genericParametersConstraints ?? "";
        metadata.Parameters = parameters.Text ?? "";
        metadata.ParametersNames = $"({invokParameters.JoinBy(", ")})";

        return metadata;
    }

    static TypeMetadata GetMethadataFromReconstructedCode(string type)
    {
        /*
        using System;
        using System.Collections.Generic;

        ===
        namespace System.IO
        {
        ===
            public class GenericClassTest2<t, t2> : Attribute
                where t: class, new()
        ===
            {
                public static | DirectoryInfo |CreateDirectory|(string path|, int index|)|;*/

        var (usingNamespaces, (
            namespaceDeclaration, (
            typeDeclaration,
            methodsDeclaration, _))) = type.Split(new[] { "===" }, StringSplitOptions.None);

        var declarationParts = typeDeclaration.Trim().Split('\n').Select(x => x.Trim()).ToArray();

        var typeConstraints = declarationParts.Count() > 1 ?
            declarationParts.Last() :
            null;

        var (typeName, inheritance, _) = declarationParts.First().Trim().Split(':').Select(x => x.Trim()).ToArray();

        var (rawTypeName, genericParams, _) = typeName.Split("<".ToCharArray(), 2);
        rawTypeName = rawTypeName.Split(' ').Last();

        if (inheritance?.Any() == true)
            inheritance = $" : {inheritance}";

        if (genericParams?.Any() == true)
            genericParams = $"<{genericParams}";// to restore the generic parameters after split by ':' 

        var typeMetadata = new TypeMetadata
        {
            UsingNamespaces = usingNamespaces.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries),
            Attributes = new string[0],
            Modifiers = "public",
            Namespace = namespaceDeclaration.Trim('{', '\n', '\r').Split(' ').Last(),
            Name = rawTypeName,
            GenericParameters = genericParams ?? "",
            GenericParametersConstraints = typeConstraints ?? ""
        };

        typeMetadata.Methods = methodsDeclaration.Trim(' ', '\n', '\r', '{', '}')
            .Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Any() && x.EndsWith(";")) // methods only
            .Select(x =>
            {
                // public | Task<T1> |SendAsync<T1, T2>|(T1 arg1|, T2 arg2|) where T1: class, new()|;

                var parts = x.Split('|').Select(y => y.Trim()).ToArray();

                var (rawName, generParam, _) = parts[2].Split("<".ToCharArray(), 2);
                if (generParam.HasText())
                    generParam = $"<{generParam}";

                var parameters = parts.Skip(3)
                    .TakeWhile(y =>
                        !y.EndsWith(")") &&
                        !y.Replace(" ", "").StartsWith(")where"))
                    .Select(y => y.Trim('(', ',').Trim())
                    .Where(y => y.Any());

                var constrtaints = parts.Skip(3)
                    .SkipWhile(y => !y.Replace(" ", "").StartsWith(")where"))
                    .JoinBy("")
                    .TrimStart(')')
                    .TrimEnd(';')
                    .Trim();


                return new MethodMetadata
                {
                    Modifiers = parts[0],
                    ReturnType = parts[1],
                    GenericParameters = generParam ?? "",
                    GenericParametersConstraints = constrtaints ?? "",
                    Name = rawName,
                    Parameters = $"({parameters.JoinBy(", ")})",
                    ParametersNames = $"({parameters.Select(y => y.Split(' ').Last()).JoinBy(", ")})"
                };
            })
            .Where(x => x != null).ToArray();
        return typeMetadata;
    }

    const string singleIndent = "    ";
    public static (string header, string bodyIndent, string footer) GenerateSourceSkeleton(this TypeDeclarationSyntax type)
    {
        // not using Roslyn formatting because it  may introduce some unwanted performance overhead and new 
        // dependencies. Doing the formatting manually for a fully deterministic code generation like here
        // is an adequate approach.
        var header = new StringBuilder();
        var footer = new StringBuilder();

        var statements = new List<string>();

        var parent = type.Parent;

        while (parent != null)
        {
            if (parent is NamespaceDeclarationSyntax ns)
                statements.Add($"namespace {ns.Name}");

            if (parent is FileScopedNamespaceDeclarationSyntax fsns)
                statements.Add($"namespace {fsns.Name}");

            if (parent is TypeDeclarationSyntax ts)
            {
                var metadata = ts.GetMetadata();
                statements.Add($"{metadata.Modifiers} {metadata.Name}{metadata.GenericParameters}{metadata.GenericParametersConstraints}".TrimEnd());
            }

            parent = parent.Parent;
        }

        statements.Reverse();

        string indent = "";
        foreach (var item in statements)
        {
            header.AppendLine($"{indent}{item}")
                  .AppendLine($"{indent}{{");

            footer.InsertLine(0, $"{indent}}}");

            indent += singleIndent;

        }

        var hCode = header.ToString().TrimEnd();
        var fCode = footer.ToString().TrimEnd();

        return (hCode, indent, fCode);
    }

}
