using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using AsyncIt;

namespace AsyncIt.System.Tests;

[Async]
public partial class UserService<T1, T2> where T1 : class, new()
{
    // static public async Task<List<T>> GetUser<T, T2>(Dictionary<string, Nullable<int>> id, string name) where T : class, new()
    static public async Task<List<T>> GetUser<T>(Dictionary<string, Nullable<int>> id, string name) where T : class, new()
        => null;
}