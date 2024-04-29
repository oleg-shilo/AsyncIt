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

static class Extensions
{
    public static string Arg(this ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, string name)
        => namedArguments.FirstOrDefault(x => x.Key == name).Value.ToCSharpString();

    public static T EnumParse<T>(this string value)
    {
        // ToCSharpString returns full names (e.g. AsyncIt.Algorithm.Inheritance)
        // or "null" if the value is null

        if (value == "null")
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

#pragma warning disable RS1035 // Do not use APIs banned for analyzers

    public static StringBuilder InsertLine(this StringBuilder builder, int index, string value) => builder.Insert(index, value + Environment.NewLine);

#pragma warning restore RS1035 // Do not use APIs banned for analyzers

    internal static bool IsToken(this object element, SyntaxKind expectedKind)
        => ((SyntaxToken)element).IsKind(expectedKind);
    internal static string AsyncToSyncReturnType(this string returnType)
        => returnType == "Task"
            ? "void"
            : returnType
                  .TrimStart("Task<")
                  .TrimStart("System.Task<")
                  .TrimEnd(">");
    internal static bool IsAsync(this MethodMetadata info)
        => info.ReturnType == "Task"
            ||
           ((info.ReturnType.StartsWith("Task<") || info.ReturnType.StartsWith("System.Task<"))
             && info.ReturnType.EndsWith(">"));
}