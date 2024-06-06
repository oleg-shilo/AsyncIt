using System;
using AsyncIt.System.Tests;

var service = new OrderService();

var order = await service.GetOrderAsync(1);
order = await service.GetOrderAsync("key");


var users = await UserService<User, Order>.GetUser<User>(new(), "name");
