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
        //var text = File.ReadAllLines("main.spsh", Encoding.UTF8);
        //int chuj = 0;
        Interpreter interpreter = new("main.spsh");
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