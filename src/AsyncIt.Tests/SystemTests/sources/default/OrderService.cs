using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using AsyncIt;

namespace AsyncIt.System.Tests;

[Async]
public partial class OrderService
{
    public Order GetOrder(int id) => null;

    internal Order GetOrder(string key) => null;

    Order GetOrderImp(int id) => null;

    Order GetOrderImp(string key) => null;
}
