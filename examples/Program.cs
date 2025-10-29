using System;
using System.Reflection;
using System.Threading.Tasks;

namespace PayOS.Examples;

public class Program
{
    public Program()
    {

    }

    public static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: dotnet run <name_of_example>");
            Environment.Exit(1);
            return;
        }
        var type = Assembly.GetExecutingAssembly().GetType($"PayOS.Examples.{args[0]}");
        if (type == null)
        {
            Console.WriteLine($"Unable to find example class {args[0]}");
            Environment.Exit(1);
            return;
        }
        var runMethod = type.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (runMethod == null)
        {
            Console.WriteLine($"Example class {args[0]} is missing run method");
            Environment.Exit(1);
            return;
        }
        Task? result = (Task?)runMethod.Invoke(null, null);
        if (result == null)
        {
            Console.WriteLine($"Unable to invoke Run method on {args[0]}");
            Environment.Exit(1);
            return;
        }

        await result;

    }
}