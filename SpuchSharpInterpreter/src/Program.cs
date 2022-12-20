using SpuchSharp.Lexing;
using System.IO;
using System.Text;

namespace SpuchSharp.Interpreter;

internal class Program
{
    static void Main(string[] args)
    {
        var text = File.ReadAllText("main.spsh", Encoding.UTF8);
        var lexer = new Lexer(text);
        foreach(var token in lexer)
        {
            Console.WriteLine(token);
        }
    }
}