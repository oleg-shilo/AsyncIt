// Ignore Spelling: Metadata

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

static class CodeGenerator
{
    public static string GenerateAsyncMethod(this MethodMetadata info)
    {
        var returnType = info.ReturnType == "void" ? "Task" : $"Task<{info.ReturnType}>";

        return
            $"{info.Modifiers} {returnType} {info.Name.TrimEnd("Sync")}Async{info.GenericParameters}{info.Parameters}\n" +
            $"    => Task.Run(() => {info.Name}{info.GenericParameters}{info.ParametersNames});";
    }

    public static string GenerateAsyncExtensionMethod(this MethodMetadata info, TypeMetadata typeMetadata)
    {
        var baseType = $"{typeMetadata.Name}{typeMetadata.GenericParameters}";
        var methodVisibility = GetExtensionMethodVisibility(typeMetadata);
        var returnType = info.ReturnType == "void" ? "Task" : $"Task<{info.ReturnType}>";
        var methodParameters = info.Parameters.Replace("(", $"(this {baseType} instance, ");

        return
            $"{methodVisibility} static {returnType} {info.Name.TrimEnd("Sync")}Async{info.GenericParameters}{methodParameters}\n" +
            $"    => Task.Run(() => instance.{info.Name}{info.GenericParameters}{info.ParametersNames});";
    }
    public static string GenerateSyncMethod(this MethodMetadata info)
    {
        if (!info.IsAsync())
            throw new Exception($"Cannot convert {info.Name} to synchronous method. Only async methods can be converted.");

        var returnType = info.ReturnType.AsyncToSyncReturnType();
        var methodName = info.Name.EndsWith("Async") ? info.Name.TrimEnd("Async") : info.Name + "Sync";
        var waitCall = (returnType == "void" ? "Wait()" : "Result");

        return
            $"{info.Modifiers.DeleteWord("async")} {returnType} {methodName}{info.GenericParameters}{info.Parameters}\n" +
            $"    => {info.Name}{info.GenericParameters}{info.ParametersNames}.{waitCall};";
    }

    public static string GenerateSyncExtensionMethod(this MethodMetadata info, TypeMetadata typeMetadata)
    {
        var baseType = $"{typeMetadata.Name}{typeMetadata.GenericParameters}";
        var methodVisibility = GetExtensionMethodVisibility(typeMetadata);

        if (!info.IsAsync())
            throw new Exception($"Cannot convert {info.Name} to synchronous method. Only async methods can be converted.");

        var returnType = info.ReturnType.AsyncToSyncReturnType();
        var methodParameters = info.Parameters.Replace("(", $"(this {baseType} instance, ");
        var methodName = info.Name.EndsWith("Async") ? info.Name.TrimEnd("Async") : info.Name + "Sync";
        var waitCall = (returnType == "void" ? "Wait()" : "Result");

        return
            $"{methodVisibility} static {returnType} {methodName}{info.GenericParameters}{methodParameters}\n" +
            $"    => instance.{info.Name}{info.GenericParameters}{info.ParametersNames}.{waitCall};";
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