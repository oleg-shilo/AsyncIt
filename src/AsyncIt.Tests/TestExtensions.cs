using System;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncIt.Tests;

static class TestExtensions
{
    public static IEnumerable<SyntaxNode> SyntaxNodes(this string code)
        => CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes();
}