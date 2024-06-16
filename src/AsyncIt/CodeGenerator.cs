// Ignore Spelling: Metadata

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

static class CodeGenerator
{
    internal static bool SuppressXmlDocGeneration = false; // for controlling XML generation during testing

    public static string GenerateXmlDoc(string context, MethodMetadata method, TypeMetadata type, bool force = false)
    {
        if (!SuppressXmlDocGeneration || force)
        {
            var typeGenericParams = type.GenericParameters.Replace("<", "{").Replace(">", "}");
            var methodGenericParams = method.GenericParameters.Replace("<", "{").Replace(">", "}");
            var methodParams = method.Parameters;

            if (method.ParametersNames.HasText())
            {
                // method.Parameters = "(T1 arg1, T2 arg2, T3 arg3, T4 arg4)";
                // method.ParametersNames = "(arg1, arg2, arg3, arg4)";

                var names = method.ParametersNames.Trim('(', ')').Split(',').Select(x => x.Trim());

                foreach (var item in names)
                {
                    var pattern = $" {item},";
                    var replacement = ",";

                    if (names.Last() == item)
                    {
                        pattern = $" {item})";
                        replacement = ")";
                    }

                    methodParams = methodParams.Replace(pattern, replacement);
                }
            }

            return $"/// <summary>\n" +
                   $"/// The {context} version of " +
                       $"<see cref=\"{type.Name}{typeGenericParams}.{method.Name}{methodGenericParams}{methodParams}\"/>.\n" +
                   $"/// </summary>\n";
        }
        return "";
    }

    public static string GenerateAsyncMethod(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        var returnType = methodInfo.ReturnType == "void" ? "Task" : $"Task<{methodInfo.ReturnType}>";
        var methodName = $"{methodInfo.Name.TrimEnd("Sync")}Async";

        var constraints = (methodInfo.GenericParametersConstraints.HasText() ? $" {methodInfo.GenericParametersConstraints.Trim()}" : "");

        var methodCode = GenerateXmlDoc("asynchronous", methodInfo, typeInfo) +

            $"{methodInfo.Modifiers} {returnType} {methodName}{methodInfo.GenericParameters}{methodInfo.Parameters}{constraints}\n" +
            $"    => Task.Run(() => {methodInfo.Name}{methodInfo.GenericParameters}{methodInfo.ParametersNames});";

        return methodCode;
    }

    public static string GenerateSyncMethod(this MethodMetadata info, TypeMetadata typeInfo)
    {
        if (!info.IsAsync())
            throw new Exception($"Cannot convert {info.Name} to synchronous method. Only async methods can be converted.");

        var returnType = info.ReturnType.AsyncToSyncReturnType();
        var methodName = info.Name.EndsWith("Async") ? info.Name.TrimEnd("Async") : info.Name + "Sync";
        var waitCall = (returnType == "void" ? "Wait()" : "Result");

        var constraints = (info.GenericParametersConstraints.HasText() ? $" {info.GenericParametersConstraints.Trim()}" : "");

        var methodCode = GenerateXmlDoc("synchronous", info, typeInfo) +

            $"{info.Modifiers.DeleteWord("async")} {returnType} {methodName}{info.GenericParameters}{info.Parameters}{constraints}\n" +
            $"    => {info.Name}{info.GenericParameters}{info.ParametersNames}.{waitCall};";

        return methodCode;
    }

    public static string GenerateAsyncExtensionMethod(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        var baseType = $"{typeInfo.Name}{typeInfo.GenericParameters}";
        var methodVisibility = GetExtensionMethodVisibility(typeInfo);
        var returnType = methodInfo.ReturnType == "void" ? "Task" : $"Task<{methodInfo.ReturnType}>";
        var methodParameters = methodInfo.Parameters.Replace("(", $"(this {baseType} instance, ");
        var methodName = $"{methodInfo.Name.TrimEnd("Sync")}Async";

        (var methodGenericParameters, var methodGenericParametersConstraints) = methodInfo.GetMethodGenericParamsInfo(typeInfo);

        return GenerateXmlDoc("asynchronous", methodInfo, typeInfo) +
            $"{methodVisibility} static {returnType} {methodName}{methodGenericParameters}{methodParameters}{methodGenericParametersConstraints}\n" +
            $"    => Task.Run(() => instance.{methodInfo.Name}{methodInfo.GenericParameters}{methodInfo.ParametersNames});";
    }

    public static string GenerateSyncExtensionMethod(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        var baseType = $"{typeInfo.Name}{typeInfo.GenericParameters}";
        var methodVisibility = GetExtensionMethodVisibility(typeInfo);

        if (!methodInfo.IsAsync())
            throw new Exception($"Cannot convert {methodInfo.Name} to synchronous method. Only async methods can be converted.");

        var returnType = methodInfo.ReturnType.AsyncToSyncReturnType();
        var methodParameters = methodInfo.Parameters.Replace("(", $"(this {baseType} instance, ");
        var methodName = methodInfo.Name.EndsWith("Async") ? methodInfo.Name.TrimEnd("Async") : methodInfo.Name + "Sync";
        var waitCall = (returnType == "void" ? "Wait()" : "Result");

        (var methodGenericParameters, var methodGenericParametersConstraints) = methodInfo.GetMethodGenericParamsInfo(typeInfo);

        return GenerateXmlDoc("synchronous", methodInfo, typeInfo) +
            $"{methodVisibility} static {returnType} {methodName}{methodGenericParameters}{methodParameters}{methodGenericParametersConstraints}\n" +
            $"    => instance.{methodInfo.Name}{methodInfo.GenericParameters}{methodInfo.ParametersNames}.{waitCall};";
    }

    static (string parameters, string constraints) GetMethodGenericParamsInfo(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        if (!typeInfo.GenericParameters.HasText() && !methodInfo.GenericParameters.HasText())
            return ("", "");

        var paramsItems = typeInfo.GenericParameters?.Trim('<', '>').Split(',').Select(x => x.Trim()) ?? new string[0];
        var paramsItems2 = methodInfo.GenericParameters?.Trim('<', '>').Split(',').Select(x => x.Trim()) ?? new string[0];

        // "type<T1, T3> + method<T1, T2>" => "<T1, T2, T3>"
        var methodGenericParameters = $"<{paramsItems.Concat(paramsItems2).Where(x => x.HasText()).Distinct().OrderBy(x => x).JoinBy(", ")}>";

        var methodGenericParametersConstraints = "";
        if (typeInfo.GenericParametersConstraints.HasText())
            methodGenericParametersConstraints += " " + typeInfo.GenericParametersConstraints;

        if (methodInfo.GenericParametersConstraints.HasText())
            methodGenericParametersConstraints += " " + methodInfo.GenericParametersConstraints;

        return (methodGenericParameters, methodGenericParametersConstraints);
    }

    static string GetExtensionMethodVisibility(TypeMetadata typeMetadata)
    {
        // The type of arg in `Method( this Type arg,...` must be the same visibility as the type in typeMetadata
        // But only applicable visibility for the extension method is either `public` or `internal`.
        // Thus only check the visibility of the typeMetadata for being `public` or not.
        // If typeMetadata is `protected` or `private` then the extension method should be `internal`
        // (lowest possible visibility) and C# complier will generate a nice compile error for the user to address.

        // typeMetadata.Modifiers: "internal class", "public class", "class" etc.

        var methodVisibility = "public";

        if (!typeMetadata.Modifiers.Contains("public"))
            methodVisibility = "internal";
        return methodVisibility;
    }
}