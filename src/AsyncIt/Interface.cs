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
using AsyncIt;

class LocalModel
{
    public string Namespace = "";
    public AsyncAttribute Attribute;
    public string TypeName = "";
    public string FilePath = "";
    public TypeDeclarationSyntax SyntaxNode;
}

class ExternalModel
{
    public AsyncExternalAttribute Attribute;
    public string TypeName = "";
    public string FilePath = "";
    public ISymbol TypeSymbol;
}

class TypeMetadata
{
    // public string Namespace;
    public string[] UsingNamespaces = new string[0];

    public string[] Attributes = new string[0];
    public string Modifiers = "";                           // public, static, sealed etc
    public string Namespace = "";
    public string Name = "";
    public string BaseList = "";                            // list of base classes
    public string GenericParameters = "";                   // method generic type params
    public string GenericParametersConstraints = "";        // generic type constraints
    public MethodMetadata[] Methods = new MethodMetadata[0];
}

class MethodMetadata
{
    public string[] Attributes;
    public string Modifiers = "";                        // public, static, sealed etc
    public string ReturnType = "";                       // int, string, Task<T> etc
    public string Name = "";                             // method name
    public string GenericParameters = "";                // method generic type params <T, T2>
    public string GenericParametersConstraints = "";     // where T1 : class, new()
    public string Parameters = "";                       // (int id, string name)
    public string ParametersNames = "";                  // id, name
}