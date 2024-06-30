// Ignore Spelling: Metadata

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AsyncIt;

static class Runtime
{
    static Runtime()
    {
        NewLine = new StringBuilder().AppendLine().ToString();
    }

    public static string NewLine;
}

public static class ArrayExtensions
{
    /// <summary>
    /// Converts an array to a tuple.
    /// <para>Based on this beautiful solution: https://stackoverflow.com/questions/49190830/is-it-possible-for-string-split-to-return-tuple</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">The list.</param>
    /// <param name="first">The first.</param>
    /// <param name="rest"></param>
    public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest)
    {
        first = list.Count > 0 ? list[0] : default(T); // or throw
        rest = list.Skip(1).ToList();
    }

    /// <summary>
    /// Converts an array to a tuple.
    /// <para>Based on this beautiful solution: https://stackoverflow.com/questions/49190830/is-it-possible-for-string-split-to-return-tuple</para>
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="first">The first.</param>
    /// <param name="second">The second.</param>
    /// <param name="rest">The rest.</param>
    public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest)
    {
        first = list.Count > 0 ? list[0] : default(T); // or throw
        second = list.Count > 1 ? list[1] : default(T); // or throw
        rest = list.Skip(2).ToList();
    }
}

static class Extensions
{
    public static string ValueByKey(this ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, string name)
        => namedArguments.FirstOrDefault(x => x.Key == name).Value.ToCSharpString();

    public static string ValueByName(this ImmutableArray<TypedConstant> arguments, string name)
    {
        // var args = arguments.Select(x => new
        // {
        //     typeDisplayName = x.Type.ToDisplayString(),
        //     typeName = x.Type.Name,
        //     typeFullName = x.Type.GetFullName()
        // }).ToArray();

        var result = arguments.FirstOrDefault(x => x.Type.ToDisplayString() == name || x.Type.GetFullName() == name);

        return result.IsNull ? null : result.Value?.ToString();
    }

    public static T EnumParse<T>(this string value)
    {
        // ToCSharpString returns full names (e.g. AsyncIt.Algorithm.Inheritance)
        // or "null" if the value is null

        if (value == "null" || !value.HasText())
            return default;
        else
            return (T)Enum.Parse(typeof(T), value.Replace($"{typeof(T).FullName}.", ""));
    }

    public static TSource TakeOutFirst<TSource>(this List<TSource> source)
        => source.TakeOut(1).FirstOrDefault();

    public static IEnumerable<TSource> TakeOut<TSource>(this List<TSource> source, int count)
    {
        // clone the data
        var result = source.Take(count).ToArray();
        foreach (var item in result)
        {
            source.Remove(item);
        }
        return result;
    }

    public static IEnumerable<TSource> TakeOutWhile<TSource>(this List<TSource> source, Func<TSource, bool> predicate)
    {
        // clone the data
        var result = source.TakeWhile(predicate).ToArray();
        foreach (var item in result)
        {
            source.Remove(item);
        }
        return result;
    }

    public static string TrimEnd(this string text, string pattern)
    {
        if (text.EndsWith(pattern))
            return text.Substring(0, text.Length - pattern.Length);
        return text;
    }

    public static string TrimStart(this string text, string pattern)
    {
        if (text.StartsWith(pattern))
            return text.Substring(pattern.Length);
        return text;
    }

    public static string DeleteWord(this string text, string word)
        => text.Replace(word, "").Replace("  ", " ").Trim();

    public static string JoinBy(this IEnumerable<string> arr, string separator) => string.Join(separator, arr);

    public static StringBuilder InsertLine(this StringBuilder builder, int index, string value) => builder.Insert(index, value + Runtime.NewLine);

    internal static bool IsToken(this object element, SyntaxKind expectedKind)
        => ((SyntaxToken)element).IsKind(expectedKind);

    internal static (string assembly, ISymbol symbol) GetAsyncExternalInfo(this ISymbol targetSymbol)
    {
        var attrArguments = targetSymbol.GetAttributes().First(x => x.AttributeClass.Name == nameof(AsyncExternalAttribute)).NamedArguments;
        var constrArguments = targetSymbol.GetAttributes().First(x => x.AttributeClass.Name == nameof(AsyncExternalAttribute)).ConstructorArguments;

        if (constrArguments.Length == 0)
        {
            var attrValue = attrArguments.FirstOrDefault(x => x.Key == nameof(AsyncExternalAttribute.Type)).Value;
            if (attrValue.IsNull)
                return (null, null);

            var assemblyName = ((ISymbol)attrValue.Value).ContainingModule.ToString();
            return (assemblyName, attrValue.Value as ISymbol);
        }
        else
        {
            var attrValue = constrArguments.FirstOrDefault().Value;
            if (attrValue == null)
                return (null, null);

            var className = attrValue.ToString();
            var assemblyName = ((ISymbol)attrValue).ContainingModule.ToString();
            return (assemblyName, attrValue as ISymbol);
        }
    }

    internal static (Algorithm algorithm, Interface @interface) GetAsyncInfo(this ISymbol targetSymbol)
    {
        var attrArguments = targetSymbol.GetAttributes().First(x => x.AttributeClass.Name == nameof(AsyncAttribute)).NamedArguments;
        var constrArguments = targetSymbol.GetAttributes().First(x => x.AttributeClass.Name == nameof(AsyncAttribute)).ConstructorArguments;

        if (constrArguments.Length == 0)
        {
            var algorithmValue = attrArguments.ValueByKey(nameof(AsyncAttribute.Algorithm)).EnumParse<Algorithm>();
            var interfaceValue = attrArguments.ValueByKey(nameof(AsyncAttribute.Interface)).EnumParse<Interface>();

            return (algorithmValue, interfaceValue);
        }
        else
        {
            var algorithm = constrArguments.ValueByName(typeof(Algorithm).FullName).EnumParse<Algorithm>();
            var @interface = constrArguments.ValueByName(typeof(Interface).FullName).EnumParse<Interface>();

            return (algorithm, @interface);
        }
    }

    internal static string GetParamValue(this AttributeData attrData, string name)
    {
        var attrArguments = attrData.NamedArguments;
        var constrArguments = attrData.ConstructorArguments;

        if (constrArguments.Length == 0)
        {
            return attrArguments.ValueByKey(name);
        }
        else
        {
            return constrArguments.ValueByName(name);
        }
    }

    // internal static T GetAttributeNamedArg<T>(this AttributeData attrData, string name = null)
    // {
    //     var attrValue = attrData.GetAttributeNamedArgValue(name ?? typeof(T).FullName);
    //     if (typeof(T).IsEnum)
    //     {
    //         return attrValue.EnumParse<T>();
    //     }

    //     return default(T);
    // }

    internal static string GetAttributeNamedArgValue(this AttributeData attrData, string name = null)
    {
        var attrArguments = attrData.NamedArguments;
        var constrArguments = attrData.ConstructorArguments;

        if (constrArguments.Length == 0)
        {
            return attrArguments.ValueByKey(name);
        }
        else
        {
            return constrArguments.ValueByName(name);
        }
    }

    internal static string AsyncToSyncReturnType(this string returnType)
        => returnType == "Task"
            ? "void"
            : returnType
                  .TrimStart("Task<")
                  .TrimStart("System.Task<")
                  .TrimEnd(">");

    internal static (string name, string genericParams) GetNameInfo(this MethodMetadata info)
        => (info.Name, info.GenericParameters.HasText() ? $"<{info.GenericParameters}>" : "");

    internal static bool IsMatching(this MethodMetadata info, string pattern)
    {
        if (pattern == "*")
            return true;

        foreach (var item in pattern.Split(',').Select(x => x.Trim()).Distinct().Where(x => x.HasText()))
        {
            if (info.Name == item)
                return true;
        }
        return false;
    }

    internal static bool IsAsync(this MethodMetadata info)
        => info.ReturnType == "Task"
            ||
           ((info.ReturnType.StartsWith("Task<") || info.ReturnType.StartsWith("System.Task<"))
             && info.ReturnType.EndsWith(">"));

    public static bool HasAny<T>(this IEnumerable<T> items)
        => items != null && items.Any();

    public static bool HasText(this string text)
    {
        return !string.IsNullOrEmpty(text);
    }

    public static T To<T>(this object obj)
    {
        return (T)obj;
    }

    public static bool OneOf(this string text, params string[] items)
    {
        return items.Any(x => x == text);
    }

    public static string IndentLinesBy(this string text, int indentLevel, string linePreffix = "")
    {
        return string.Join(Runtime.NewLine, text.Replace("\r", "")
                                                .Split('\n')
                                                    .Select(x => x.IndentBy(indentLevel, linePreffix))
                                                    .ToArray());
    }
}