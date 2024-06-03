using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;

namespace AsyncIt.Tests;

public class TestBase
{
    public TestBase()
    {
        CodeGenerator.SuppressXmlDocGeneration = true;
    }

    internal static string thisAsm = Assembly.GetExecutingAssembly().Location;
    internal static string asyncAsm = typeof(CodeGenerator).Assembly.Location;
    internal static string projectDir = null;
    internal static string ProjectDir
    {
        get
        {
            if (projectDir != null)
                return projectDir;

            projectDir = Path.Combine(Path.GetDirectoryName(thisAsm), "system-test");
            Directory.CreateDirectory(projectDir);
            return projectDir;

        }
    }

    internal static void CreateProjectFile(string prjectName)
    {
        File.WriteAllText(Path.Combine(ProjectDir, prjectName), """
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <RootNamespace>system_test</RootNamespace>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>

              <ItemGroup>
                <Reference Include="AsyncIt">
                  <HintPath>AsyncIt.dll</HintPath>
                </Reference>
              </ItemGroup>

            </Project>
            """);
    }

    internal static void SaveExtendableCode(string fileName, string code)
    {
        File.WriteAllText(Path.Combine(ProjectDir, fileName), code);
        var newCode = CSharpSyntaxTree.ParseText(code).GenerateSourceForTypes().First().Value;
        File.WriteAllText(Path.Combine(ProjectDir, Path.ChangeExtension(fileName, ".g.cs")), newCode);
    }
    internal static void SaveCodeFile(string fileName, string code)
        => File.WriteAllText(Path.Combine(ProjectDir, fileName), code);
    internal static void SaveFile(string source)
        => File.Copy(source, Path.Combine(ProjectDir, Path.GetFileName(source)), true);

    internal static (int exitCode, string output) ExecuteBackgroundProcess(string app, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = app,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = ProjectDir,
                CreateNoWindow = true,
            }
        };
        process.Start();
        process.WaitForExit();
        return (process.ExitCode, process.StandardOutput.ReadToEnd());
    }
}
static class TestExtensions
{
    public static ISymbol SymbolAt(this Document doc, int pos)
        => SymbolFinder.FindSymbolAtPositionAsync(doc, pos).Result;
    public static string GetTypeInfo(this string type)
    {
        if (type == "HttpClient")
            return httpClientTypeInfo; // I cannot find the right Roslyn config to parse HttpClient programatically

        var code = """
        using System;
        using System.IO;
        using AsyncIt.Tests;
        using System.Net;
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.Runtime.CompilerServices;
        using AsyncIt;

        [assembly: AsyncExternal(typeof($(type)), Interface.Sync)]
        // [assembly: AsyncExternal(typeof(HttpClient))]
        """;

        code = code.Replace("$(type)", type);

        var doc = code.ToCompiledDoc([
                typeof(object).Assembly.Location,
                typeof(TestExtensions).Assembly.Location,
                typeof(AsyncExternalAttribute).Assembly.Location,
                // typeof(HttpClient).Assembly.Location,
                typeof(Directory).Assembly.Location]);

        ISymbol symbol = doc.SymbolAt(code.IndexOf(type) + 3);
        var directoryCode = symbol?.Reconstruct();

        return directoryCode;
    }
    public static Document ToCompiledDoc(this string code, params string[] refs)
    {
        var workspace = new AdhocWorkspace();

        var projName = "TestProject";
        var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
            projName, projName, LanguageNames.CSharp,
            metadataReferences: refs.Select(x => MetadataReference.CreateFromFile(x)));

        var newProject = workspace.AddProject(projectInfo);
        var doc = workspace.AddDocument(newProject.Id, "script.cs", SourceText.From(code));

        return doc;
    }

    public static IEnumerable<SyntaxNode> SyntaxNodes(this string code)
        => CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes();

    static string listTypeInfo = """
        using System;
        using System.Collections.ObjectModel;
        using System.Collections;

        ===
        namespace System.Collections.Generic
        {
        ===
            public class List<T>: IList, ICollection, IReadOnlyList, IReadOnlyCollection, IEnumerable, IList, ICollection, IEnumerable
        ===
            {
                public | List|()|;
                public | List|(IEnumerable<T> collection|)|;
                public | List|(int capacity|)|;

                public |int Capacity { get; set; }
                public |int Count { get; }
                public |T this[int index] { get; set; }

                public | void |Add|(T item|)|;
                public | void |AddRange|(IEnumerable<T> collection|)|;
                public | ReadOnlyCollection<T> |AsReadOnly|()|;
                public | int |BinarySearch|(int index|, int count|, T item|, IComparer<T>? comparer|)|;
                public | int |BinarySearch|(T item|)|;
                public | int |BinarySearch|(T item|, IComparer<T>? comparer|)|;
                public | void |Clear|()|;
                public | bool |Contains|(T item|)|;
                public | List<TOutput> |ConvertAll<TOutput>|(Converter<T, TOutput> converter|)|;
                public | void |CopyTo|(int index|, T[] array|, int arrayIndex|, int count|)|;
                public | void |CopyTo|(T[] array|)|;
                public | void |CopyTo|(T[] array|, int arrayIndex|)|;
                public | int |EnsureCapacity|(int capacity|)|;
                public | bool |Exists|(Predicate<T> match|)|;
                public | T? |Find|(Predicate<T> match|)|;
                public | List<T> |FindAll|(Predicate<T> match|)|;
                public | int |FindIndex|(int startIndex|, int count|, Predicate<T> match|)|;
                public | int |FindIndex|(int startIndex|, Predicate<T> match|)|;
                public | int |FindIndex|(Predicate<T> match|)|;
                public | T? |FindLast|(Predicate<T> match|)|;
                public | int |FindLastIndex|(int startIndex|, int count|, Predicate<T> match|)|;
                public | int |FindLastIndex|(int startIndex|, Predicate<T> match|)|;
                public | int |FindLastIndex|(Predicate<T> match|)|;
                public | void |ForEach|(Action<T> action|)|;
                public | List<T>.Enumerator |GetEnumerator|()|;
                public | List<T> |GetRange|(int index|, int count|)|;
                public | int |IndexOf|(T item|)|;
                public | int |IndexOf|(T item|, int index|)|;
                public | int |IndexOf|(T item|, int index|, int count|)|;
                public | void |Insert|(int index|, T item|)|;
                public | void |InsertRange|(int index|, IEnumerable<T> collection|)|;
                public | int |LastIndexOf|(T item|)|;
                public | int |LastIndexOf|(T item|, int index|)|;
                public | int |LastIndexOf|(T item|, int index|, int count|)|;
                public | bool |Remove|(T item|)|;
                public | int |RemoveAll|(Predicate<T> match|)|;
                public | void |RemoveAt|(int index|)|;
                public | void |RemoveRange|(int index|, int count|)|;
                public | void |Reverse|()|;
                public | void |Reverse|(int index|, int count|)|;
                public | void |Sort|()|;
                public | void |Sort|(IComparer<T>? comparer|)|;
                public | void |Sort|(Comparison<T> comparison|)|;
                public | void |Sort|(int index|, int count|, IComparer<T>? comparer|)|;
                public | T[] |ToArray|()|;
                public | void |TrimExcess|()|;
                public | bool |TrueForAll|(Predicate<T> match|)|;

                public |sealed struct Enumerator: IEnumerator, IEnumerator, IDisposable { /*hidden*/ }
            }
        }
        """;

    static string httpClientTypeInfo = """
            using System;
            using System.Net;
            using System.Net.Http.Headers;
            using System.Threading.Tasks;

            namespace System.Net.Http
            {
                public class HttpClient : HttpMessageInvoker, IDisposable
                {
                    public HttpClient();
                    public HttpClient(HttpMessageHandler handler);
                    public HttpClient(HttpMessageHandler handler, bool disposeHandler);

                    public Uri? BaseAddress { get; set; }
                    public static IWebProxy DefaultProxy { get; set; }
                    public HttpRequestHeaders DefaultRequestHeaders { get; }
                    public Version DefaultRequestVersion { get; set; }
                    public HttpVersionPolicy DefaultVersionPolicy { get; set; }
                    public long MaxResponseContentBufferSize { get; set; }
                    public TimeSpan Timeout { get; set; }

                    public void CancelPendingRequests();
                    public Task<HttpResponseMessage> DeleteAsync(string? requestUri);
                    public Task<HttpResponseMessage> DeleteAsync(string? requestUri, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri);
                    public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> GetAsync(string? requestUri);
                    public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption);
                    public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> GetAsync(string? requestUri, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> GetAsync(Uri? requestUri);
                    public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption);
                    public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> GetAsync(Uri? requestUri, CancellationToken cancellationToken);
                    public Task<byte[]> GetByteArrayAsync(string? requestUri);
                    public Task<byte[]> GetByteArrayAsync(string? requestUri, CancellationToken cancellationToken);
                    public Task<byte[]> GetByteArrayAsync(Uri? requestUri);
                    public Task<byte[]> GetByteArrayAsync(Uri? requestUri, CancellationToken cancellationToken);
                    public Task<Stream> GetStreamAsync(string? requestUri);
                    public Task<Stream> GetStreamAsync(string? requestUri, CancellationToken cancellationToken);
                    public Task<Stream> GetStreamAsync(Uri? requestUri);
                    public Task<Stream> GetStreamAsync(Uri? requestUri, CancellationToken cancellationToken);
                    public Task<string> GetStringAsync(string? requestUri);
                    public Task<string> GetStringAsync(string? requestUri, CancellationToken cancellationToken);
                    public Task<string> GetStringAsync(Uri? requestUri);
                    public Task<string> GetStringAsync(Uri? requestUri, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent? content);
                    public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content);
                    public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent? content);
                    public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content);
                    public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent? content);
                    public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content);
                    public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken);
                    public HttpResponseMessage Send(HttpRequestMessage request);
                    public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption);
                    public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken);
                    public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken);
                    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
                    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption);
                    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken);
                    public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
                    protected override void Dispose(bool disposing);
                }
            }
            """;
}
