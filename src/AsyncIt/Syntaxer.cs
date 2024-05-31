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
using AsyncIt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class Syntaxer
{
    public static string Reconstruct(this ISymbol symbol, string header = "")
    {
        var code = new StringBuilder();
        code.Append(header);

        int indent = 0;

        INamedTypeSymbol rootType = symbol.GetRootType();  //itself if it is a type or containing type if a member

        var usedNamespaces = rootType.UsedNamespaces();
        if (usedNamespaces.HasAny())
        {
            code.AppendLine(usedNamespaces.Select(x => $"using {x};").JoinBy(Runtime.NewLine));
            code.AppendLine();
        }
        code.AppendLine("===");

        string nmsp = rootType.GetNamespace();
        if (nmsp.HasAny())
        {
            code.AppendLine(("namespace " + nmsp).IndentBy(indent));
            code.AppendLine("{".IndentBy(indent++));
        }
        code.AppendLine("===");

        var parentClasses = rootType.GetParentClasses();
        foreach (var parent in parentClasses) //nested classes support
            code.AppendLine($"public {parent.TypeKind.ToString().ToLower()} ".IndentBy(indent) + parent.Name)
                .AppendLine("{".IndentBy(indent++));

        var type = rootType.ToReflectedCode(false);
        type = type.IndentLinesBy(indent);
        code.AppendLine(type);

        code.AppendLine("===");

        //<doc>\r\n<declaration>

        if (!rootType.IsDelegate())
        {
            code.AppendLine("{".IndentBy(indent++));

            string currentGroup = null;

            var members = rootType.GetMembers()
                                  .OrderBy(x => x.GetDisplayGroup())
                                  .ThenByDescending(x => x.DeclaredAccessibility)
                                  .ThenBy(x => rootType.IsEnum() ? "" : x.Name);

            foreach (var item in members)
            {
                string memberInfo = item.ToReflectedCode(false, usePartials: true, memberSeparator: "|");
                // !!!! exclude operators and constructors
                if (memberInfo.HasAny())
                {
                    if (currentGroup != null && item.GetDisplayGroup() != currentGroup)
                        code.AppendLine();
                    currentGroup = item.GetDisplayGroup();

                    var info = memberInfo.IndentLinesBy(indent);

                    // #pragma warning disable RS1024 // Compare symbols correctly
                    //                     if (symbol == item || (symbol is IMethodSymbol && (symbol as IMethodSymbol).ReducedFrom == item))
                    // #pragma warning restore RS1024 // Compare symbols correctly
                    //                         startPosition = code.Length + info.LastLineStart();

                    code.AppendLine(info);
                }
            }

            code.AppendLine("}".IndentBy(--indent));
        }

        foreach (var item in parentClasses)
            code.AppendLine("}".IndentBy(--indent));

        if (nmsp.HasAny()) code.AppendLine("}".IndentBy(--indent));

        return code.ToString().Trim();
    }
    public static string ReconstructAsSource(this ISymbol symbol, string header = "")
    {
        var code = new StringBuilder();
        code.Append(header);

        int indent = 0;

        INamedTypeSymbol rootType = symbol.GetRootType();  //itself if it is a type or containing type if a member

        var usedNamespaces = rootType.UsedNamespaces();
        if (usedNamespaces.HasAny())
        {
            code.AppendLine(usedNamespaces.Select(x => $"using {x};").JoinBy(Runtime.NewLine));
            code.AppendLine();
        }

        string nmsp = rootType.GetNamespace();
        if (nmsp.HasAny())
        {
            code.AppendLine(("namespace " + nmsp).IndentBy(indent));
            code.AppendLine("{".IndentBy(indent++));
        }

        var parentClasses = rootType.GetParentClasses();
        foreach (var parent in parentClasses) //nested classes support
            code.AppendLine($"public {parent.TypeKind.ToString().ToLower()} ".IndentBy(indent) + parent.Name)
                .AppendLine("{".IndentBy(indent++));

        var type = rootType.ToReflectedCode(false);
        type = type.IndentLinesBy(indent);
        code.AppendLine(type);

        //<doc>\r\n<declaration>

        if (!rootType.IsDelegate())
        {
            code.AppendLine("{".IndentBy(indent++));

            string currentGroup = null;

            var members = rootType.GetMembers()
                                  .OrderBy(x => x.GetDisplayGroup())
                                  .ThenByDescending(x => x.DeclaredAccessibility)
                                  .ThenBy(x => rootType.IsEnum() ? "" : x.Name);

            foreach (var item in members)
            {
                string memberInfo = item.ToReflectedCode(false, usePartials: true);

                if (memberInfo.HasAny())
                {
                    if (currentGroup != null && item.GetDisplayGroup() != currentGroup)
                        code.AppendLine();
                    currentGroup = item.GetDisplayGroup();

                    var info = memberInfo.IndentLinesBy(indent);

                    // #pragma warning disable RS1024 // Compare symbols correctly
                    //                     if (symbol == item || (symbol is IMethodSymbol && (symbol as IMethodSymbol).ReducedFrom == item))
                    // #pragma warning restore RS1024 // Compare symbols correctly
                    //                         startPosition = code.Length + info.LastLineStart();

                    code.AppendLine(info);
                }
            }

            code.AppendLine("}".IndentBy(--indent));
        }

        foreach (var item in parentClasses)
            code.AppendLine("}".IndentBy(--indent));

        if (nmsp.HasAny()) code.AppendLine("}".IndentBy(--indent));

        return code.ToString().Trim();
    }
}

static class RoslynExtensions
{
    public static string IndentBy(this string text, int indentLevel, string linePreffix = "")
    {
        var indent = new string(' ', indentLevel * 4);
        return indent + linePreffix + text;
    }



    public static int LastLineStart(this string text)
    {
        var start = text.LastIndexOf('\n'); //<doc>\r\n<declaration>
        if (start != -1)
            return start + Runtime.NewLine.Length;
        return 0;
    }




    static string ToMethodCode(this IMethodSymbol symbol, string modifiers, string memberSeparator = "")
    {
        var code = new StringBuilder(150);

        if (symbol.ContainingType.IsInterface())
            modifiers = "";

        string returnTypeAndName;
        var returnType = symbol.OriginalDefinition.ReturnType.ToMinimalString();

        if (symbol.IsConstructor())
            returnTypeAndName = $"{memberSeparator}{symbol.ContainingType.Name}";        // Printer
        else if (symbol.IsDestructor())
            returnTypeAndName = $"{memberSeparator}~{symbol.ContainingType.Name}";       // ~Printer
        else if (symbol.IsOperator())
            returnTypeAndName = $"{returnType} {memberSeparator}{symbol.GetDisplayName()}";  // operator T? (T value);
        else if (symbol.IsConversion())
            returnTypeAndName = $"{symbol.GetDisplayName()} {returnType}";  //  implicit operator DBBool(bool x)
        else
            returnTypeAndName = $"{returnType} {memberSeparator}{symbol.Name}";//int GetIndex

        code.Append($"{modifiers.Trim()} ")                                 // public static
            .Append(returnTypeAndName)                                      // int GetIndex
            .Append(symbol.TypeParameters.ToDeclarationString())            // <T, T2>
            .Append(memberSeparator)
            .Append(symbol.GetParametersString(memberSeparator))            //(int position, int value)
            .Append(symbol.TypeParameters.GetConstrains(singleLine: true))  // where T: class
            .Append(memberSeparator)
            .Append(";");

        return code.ToString();
    }


    public static string ToReflectedCode(this ISymbol symbol, bool includeDoc = true, bool usePartials = false, string memberSeparator = "")
    {

        var code = new StringBuilder(150);

        Action<string, int> cosdeAddComment = (text, indent) => { if (text.HasAny()) code.AppendLine(text.IndentLinesBy(indent, "// ")); };
        // if (includeDoc)
        // {
        //     var doc = symbol.GetDocumentationComment();
        //     cosdeAddComment(doc, 0);
        // }

        if (symbol.DeclaredAccessibility != Accessibility.Public && symbol.DeclaredAccessibility != Accessibility.Protected)
            return null;

        string modifiers = $"{symbol.DeclaredAccessibility} ".ToLower();

        if (symbol.IsOverride) modifiers += "override ";
        if (symbol.IsStatic) modifiers += "static ";
        if (symbol.IsAbstract && !(symbol as INamedTypeSymbol).IsInterface())
            modifiers += "abstract ";
        if (symbol.IsVirtual) modifiers += "virtual ";

        modifiers += memberSeparator;

        switch (symbol.Kind)
        {
            case SymbolKind.Property:
                {
                    var prop = (IPropertySymbol)symbol;
                    code.Append(prop.ToPropertyCode(modifiers));
                    break;
                }
            case SymbolKind.Field:
                {
                    var field = (IFieldSymbol)symbol;
                    code.Append(field.ToFieldCode(modifiers));
                    break;
                }
            case SymbolKind.Event:
                {
                    var @event = (IEventSymbol)symbol;
                    code.Append(@event.ToEventCode(modifiers));
                    break;
                }
            case SymbolKind.Method:
                {
                    if (symbol.HiddenFromUser())
                        return null;

                    var method = (symbol as IMethodSymbol);
                    code.Append(method.ToMethodCode(modifiers, memberSeparator));
                    break;
                }
            case SymbolKind.NamedType:
                {
                    var type = (INamedTypeSymbol)symbol;
                    code.Append(type.ToTypeCode(modifiers, usePartials));
                    break;
                }
        }

        if (code.Length == 0)
            code.AppendLine($"{symbol.ToDisplayKind()}: {symbol.ToDisplayString()};");

        return code.ToString();
    }

    public static string ToLiteral(this string input)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input)).ToFullString();
    }

    public static string ToLiteral(this char input)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input)).ToFullString();
    }
    static string ToTypeCode(this INamedTypeSymbol type, string modifiers, bool usePartials)
    {
        var code = new StringBuilder(150);

        if ((!type.IsEnum() && !type.IsDelegate()) && type.IsSealed)
            modifiers += "sealed ";

        string kind = type.IsInterface() ? "interface" :
                      type.IsReferenceType ? "class" :
                      type.IsEnum() ? "enum" :
                      "struct";

        if (type.IsDelegate())
        {
            IMethodSymbol invokeMethod = type.GetMethod("Invoke");

            code.Append($"{modifiers}delegate {invokeMethod.ReturnType.ToDisplayString()} {type.Name}")   // public delegate int GetIndexDlgt                                // public class Filter
                .Append(type.TypeParameters.ToDeclarationString())                          // <T, T2>
                .Append(invokeMethod.GetParametersString())                                 // (CustomIndex count, int? contextArg, T parent)
                .Append(type.TypeParameters.GetConstrains(singleLine: type.IsDelegate()))   // where T: class
                .Append(";");
        }
        else
        {
            //if (usePartials)
            //    kind = "partial " + kind;

            code.Append($"{modifiers}{kind} {type.Name}")                                   // public class Filter
                .Append(type.TypeParameters.ToDeclarationString())                          // <T, T2>
                .Append(type.IsEnum() ? "" :
                        type.ToInheritanceString())                                         // : IList<int>
                .Append(type.TypeParameters.GetConstrains(singleLine: type.IsDelegate()));  // where T: class

            if (usePartials)
                code.Append(" { /*hidden*/ }");
        }

        return code.ToString();
    }
    static bool HiddenFromUser(this ISymbol symbol)
    {
        if (symbol is IMethodSymbol)
        {
            var method = symbol as IMethodSymbol;

            if (method.ContainingType.IsEnum()) //Enum constructor is hidden from user
                return true;

            if (method.MethodKind == MethodKind.PropertyGet ||
                method.MethodKind == MethodKind.PropertySet ||
                method.MethodKind == MethodKind.EventAdd ||
                method.MethodKind == MethodKind.EventRemove)
                return true; //getters, setters and so on are hidden from user

            if (method.IsConstructor())
            {
                if (!method.Parameters.Any() && method.ContainingType.Constructors.Length == 1 && method.DeclaredAccessibility == Accessibility.Public)
                    return true; //hide default constructors if it is the only public constructor
            }
        }
        return false;
    }
    static string ToPropertyCode(this IPropertySymbol symbol, string modifiers)
    {
        var code = new StringBuilder(150);
        if (symbol.ContainingType.IsInterface())
            modifiers = "";

        string getter = "";
        string setter = "";
        if (symbol.GetMethod != null)
        {
            if (symbol.GetMethod.DeclaredAccessibility == Accessibility.Protected)
                getter = "protected get; ";
            else if (symbol.GetMethod.DeclaredAccessibility == Accessibility.Public)
                getter = "get; ";
        }
        if (symbol.SetMethod != null)
        {
            if (symbol.SetMethod.DeclaredAccessibility == Accessibility.Protected)
                setter = "protected set; ";
            else if (symbol.SetMethod.DeclaredAccessibility == Accessibility.Public)
                setter = "set; ";
        }

        var body = $"{{ {getter}{setter}}}";

        var type = symbol.OriginalDefinition.Type.ToMinimalString();

        //if (prop.IsReadOnly) modifiers += "readonly ";

        if (symbol.IsIndexer)
            code.Append($"{modifiers}{type} this{symbol.GetIndexerParametersString()} {body}");
        else
            code.Append($"{modifiers}{type} {symbol.Name} {body}");
        return code.ToString();
    }

    static string ToEventCode(this IEventSymbol symbol, string modifiers)
    {
        var code = new StringBuilder(150);
        if (symbol.ContainingType.IsInterface())
            modifiers = "";

        var type = symbol.OriginalDefinition.Type.ToMinimalString();
        code.Append($"{modifiers}event {type} {symbol.Name};");
        return code.ToString();
    }

    static string ToFieldCode(this IFieldSymbol symbol, string modifiers)
    {
        var code = new StringBuilder(150);

        if (symbol.ContainingType.IsEnum())
        {
            if (symbol.ConstantValue != null)
                code.Append($"{symbol.Name} = {symbol.ConstantValue},");
            else
                code.Append($"{symbol.Name},");
        }
        else
        {
            var type = symbol.OriginalDefinition.Type.ToMinimalString();

            if (symbol.IsConst)
                modifiers += "const ";

            if (symbol.IsReadOnly)
                modifiers += "readonly ";

            var val = "";
            if (symbol.ConstantValue != null)
            {
                val = symbol.ConstantValue.ToString();
                var typeFullName = symbol.OriginalDefinition.Type.GetFullName();
                if (typeFullName == "System.Char")
                    val = $" = {symbol.ConstantValue.To<char>().ToLiteral()}";
                else if (typeFullName == "System.String")
                    val = $" = {val.ToLiteral()}";
                else
                    val = $" = {val}";
            }

            code.Append($"{modifiers}{type} {symbol.Name}{val};");
        }

        return code.ToString();
    }
}
