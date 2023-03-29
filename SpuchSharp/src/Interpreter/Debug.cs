using SpuchSharp.Instructions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VariableScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;

namespace SpuchSharp.Interpreting;

internal static class Debug
{
    [Conditional("EXPRESSION")]
    public static void PrintExpression(Expression expr)
    {
        Console.WriteLine($$"""
            Expression {
                Type: {{expr}}
                Display: {{expr.Display()}}
            }
            """);
    }

    [Conditional("INSTRUCTION")]
    public static void PrintInstruction(Instruction ins)
    {
        Console.WriteLine($$"""
            Instruction {
                Type: {{ins}}
            }
            """);
    }

    [Conditional("DEBUG")]
    public static void DebugInfo(VariableScope varScope, FunctionScope funScope)
    {
        Console.WriteLine($"""

            //EXECUTION ENDED//
            ///////DEBUG///////
            
            Global Variables:
            {printVariables()}

            Functions:
            {printFunctions()}

            ///////DEBUG///////
            """);

        string printVariables()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var (ident, svar) in varScope)
            {
                sb.AppendLine($"[{ident.Stringify()}, {svar.Display()}]");
            }
            return sb.ToString();
        }
        string printFunctions()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var (ident, sfun) in funScope)
            {
                sb.AppendLine($"{ident.Stringify()}, {sfun.Display()}");
            }
            return sb.ToString();
        }
    }
    [Conditional("DEBUG")]
    public static void PrintCall(CallExpression call)
    {
        Console.WriteLine(call.Display());
    }
    [Conditional("SCOPE")]
    public static void PrintScope(VariableScope variableScope)
    {
        Console.WriteLine("=================");
        Console.WriteLine("VARIABLE SCOPE:");
        foreach (var (i, V) in variableScope)
        {
            Console.WriteLine($"[{i.Stringify()} {V.Display()}]");
        }
        Console.WriteLine("=================");
    }
}
