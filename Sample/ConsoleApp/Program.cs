using AsyncIt;

namespace ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            {
                var svc = new NumberService_EM_Async();

                var number = await svc.GetNumberAsync(2);   // generated method
                var number2 = svc.GetNumber(22);            // existing method

                Console.WriteLine($"Extension Methods (async): {number}-{number2}");
            }

            {
                var svc = new NumberService_EM_Sync();

                var number = await svc.GetNumberAsync(1);   // existing method
                var number2 = svc.GetNumber(11);            // generated method

                Console.WriteLine($"Extension Methods (sync): {number}-{number2}");
            }

            {
                var svc = new NumberService_EM_Full();

                var number = await svc.GetNumberAsync(3);    // existing method
                var name2 = svc.GetString("aa");             // existing method

                var number2 = svc.GetNumber(33);             // generated method
                var name = await svc.GetStringAsync("a");    // generated method

                Console.WriteLine($"Extension Methods (full): {number}-{number2}, \"{name}\"-\"{name2}\"");
            }

            {
                var svc = new NumberService_PT_Async();

                var number = await svc.GetNumberAsync(5);  // generated method
                var number2 = svc.GetNumber(55);           // existing method

                Console.WriteLine($"Partial Type (async): {number}-{number2}");
            }

            {
                var svc = new NumberService_PT_Sync();

                var number = await svc.GetNumberAsync(4);   // existing method
                var number2 = svc.GetNumber(44);            // generated method

                Console.WriteLine($"Partial Type (sync): {number}-{number2}");
            }

            {
                var svc = new NumberService_PT_Full();

                var number = await svc.GetNumberAsync(6);   // existing method
                var name2 = svc.GetString("bb");            // existing method

                var number2 = svc.GetNumber(66);            // generated method
                var name = await svc.GetStringAsync("b");   // generated method

                Console.WriteLine($"Partial Type (full): {number}-{number2}, \"{name}\"-\"{name2}\"");
            }

        }

    }


    [Async(Algorithm = Algorithm.PartialType, Interface = Interface.Async)]
    partial class NumberService_PT_Async
    {
        public int GetNumber(int id)
        {
            Task.Delay(500).Wait();
            return id;
        }
    }


    [Async(Algorithm = Algorithm.PartialType, Interface = Interface.Sync)]
    partial class NumberService_PT_Sync
    {
        public async Task<int> GetNumberAsync(int id)
        {
            Task.Delay(500).Wait();
            return id;
        }
    }


    [Async(Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Async)]
    partial class NumberService_EM_Async
    {
        public int GetNumber(int id)
        {
            Task.Delay(500).Wait();
            return id;
        }
    }

    [Async(Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Sync)]
    partial class NumberService_EM_Sync
    {
        public async Task<int> GetNumberAsync(int id)
        {
            Task.Delay(500).Wait();
            return id;
        }
    }

    [Async(Algorithm = Algorithm.ExtensionMethods, Interface = Interface.Full)]
    partial class NumberService_EM_Full
    {
        public async Task<int> GetNumberAsync(int id)
        {
            Task.Delay(500).Wait();
            return id;
        }

        public string GetString(string name)
        {
            Task.Delay(500).Wait();
            return name;
        }
    }

    [Async(Algorithm = Algorithm.PartialType, Interface = Interface.Full)]
    partial class NumberService_PT_Full
    {
        public async Task<int> GetNumberAsync(int id)
        {
            Task.Delay(500).Wait();
            return id;
        }

        public string GetString(string name)
        {
            Task.Delay(500).Wait();
            return name;
        }
    }
}
