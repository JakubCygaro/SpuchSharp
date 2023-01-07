using SpuchSharp.Lexing;
using SpuchSharp.Parsing;
using System.IO;
using System.Text;
using SpuchSharp.Interpreting;

namespace SpuchSharp;
internal class Program
{
    static void Main(string[] args)
    {
#if DEBUG
        if(args.Length == 0)
        {
            args = new string[] { "main.spsh" }; 
        }
#else
        if (args.Length != 1)
            throw new ArgumentException("No main.spsh file path provided.");
#endif
        var main = args[0];
        Interpreter interpreter = new(main);
        try
        {
            interpreter.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
    }
}