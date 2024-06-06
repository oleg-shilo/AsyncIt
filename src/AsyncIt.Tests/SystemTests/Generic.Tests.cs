using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AsyncIt.Tests.SystemTests;
public class System_Tests : TestBase
{
    [Fact]
    public void ShouldIgnorePrivateAndProtectedMethods()
    {
        var folder = new TestFolder("a");
        folder.Create("test.csproj");
        folder.Copy(typeof(CodeGenerator).Assembly.Location);

        folder.Create("Program.cs");
        folder.Create("Models.cs");
        folder.Create("OrderService.cs");
        folder.Create("UserService.cs");

        folder.ProcessWithAsyncIt("OrderService.cs");
        folder.ProcessWithAsyncIt("UserService.cs");

        (var exitCode, var output) = folder.ExecuteBackgroundProcess("dotnet.exe", "build");

        Assert.Equal(0, exitCode);
    }
}

