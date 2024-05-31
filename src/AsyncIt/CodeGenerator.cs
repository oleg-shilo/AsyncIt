// Ignore Spelling: Metadata

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

static class CodeGenerator
{
    public static string GenerateAsyncMethod(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        var returnType = methodInfo.ReturnType == "void" ? "Task" : $"Task<{methodInfo.ReturnType}>";
        var methodName = $"{methodInfo.Name.TrimEnd("Sync")}Async";

        (var methodGenericParameters,
         var methodGenericParametersConstraints) = methodInfo.GetMethodGenericParamsInfo(typeInfo);

        return
            $"{methodInfo.Modifiers} {returnType} {methodName}{methodGenericParameters}{methodInfo.Parameters}{methodGenericParametersConstraints}\n" +
            $"    => Task.Run(() => {methodInfo.Name}{methodInfo.GenericParameters}{methodInfo.ParametersNames});";
    }

    public static string GenerateSyncMethod(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        if (!methodInfo.IsAsync())
            throw new Exception($"Cannot convert {methodInfo.Name} to synchronous method. Only async methods can be converted.");

        var returnType = methodInfo.ReturnType.AsyncToSyncReturnType();
        var methodName = methodInfo.Name.EndsWith("Async") ? methodInfo.Name.TrimEnd("Async") : methodInfo.Name + "Sync";
        var waitCall = (returnType == "void" ? "Wait()" : "Result");

        (var methodGenericParameters,
         var methodGenericParametersConstraints) = methodInfo.GetMethodGenericParamsInfo(typeInfo);


        return
            $"{methodInfo.Modifiers.DeleteWord("async")} {returnType} {methodName}{methodGenericParameters}{methodInfo.Parameters}{methodGenericParametersConstraints}\n" +
            $"    => {methodInfo.Name}{methodInfo.GenericParameters}{methodInfo.ParametersNames}.{waitCall};";
    }

    public static string GenerateAsyncExtensionMethod(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        var baseType = $"{typeInfo.Name}{typeInfo.GenericParameters}";
        var methodVisibility = GetExtensionMethodVisibility(typeInfo);
        var returnType = methodInfo.ReturnType == "void" ? "Task" : $"Task<{methodInfo.ReturnType}>";
        var methodParameters = methodInfo.Parameters.Replace("(", $"(this {baseType} instance, ");
        var methodName = $"{methodInfo.Name.TrimEnd("Sync")}Async";

        (var methodGenericParameters, var methodGenericParametersConstraints) = methodInfo.GetMethodGenericParamsInfo(typeInfo);

        return
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

        return
            $"{methodVisibility} static {returnType} {methodName}{methodGenericParameters}{methodParameters}{methodGenericParametersConstraints}\n" +
            $"    => instance.{methodInfo.Name}{methodInfo.GenericParameters}{methodInfo.ParametersNames}.{waitCall};";
    }

    static (string parameters, string constraints) GetMethodGenericParamsInfo(this MethodMetadata methodInfo, TypeMetadata typeInfo)
    {
        var paramsItems =
            typeInfo.GenericParameters.Trim('<', '>').Split(',').Select(x => x.Trim())
              .Concat(
            methodInfo.GenericParameters.Trim('<', '>').Split(',').Select(x => x.Trim()))
              .Distinct()
              .OrderBy(x => x);

        // "type<T1, T3> + method<T1, T2>" => "<T1, T2, T3>"
        var methodGenericParameters = $"<{paramsItems.JoinBy(", ")}>";

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