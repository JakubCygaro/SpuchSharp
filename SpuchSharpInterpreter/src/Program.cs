using SpuchSharp.Lexing;
using System.IO;
using System.Text;

namespace SpuchSharp.Interpreter;

internal class Program
{
    static void Main(string[] args)
    {
        var text = File.ReadAllLines("main.spsh", Encoding.UTF8);
        int chuj = 0;
        var lexer = new Lexer(text);
        foreach(var token in lexer)
        {
            Console.WriteLine(token);
            Console.WriteLine(token.Stringify());
            chuj++;
            if (chuj > 7) break;
        }
    }
}