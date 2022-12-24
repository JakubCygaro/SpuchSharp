using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Instructions;
using SpuchSharp.Parsing;

namespace SpuchSharp.Interpreting;

public sealed class Interpreter
{
    private readonly Parser _parser;

    private HashSet<SVariable> _globalVariableScope = new();
    internal Interpreter(Parser parser) 
    {
        _parser = parser;
    }
    public Interpreter(string sourceFilePath) : this(GetParserFromSource(sourceFilePath)) { }
    static Parser GetParserFromSource(string path)
    {
        return new Parser(new Lexing.Lexer(File.ReadAllLines(path, Encoding.UTF8)));
    }
    public void Run()
    {
        foreach(var instruction in _parser)
        {
            PrintInstruction(instruction);
            IfDeclaration(instruction);
        }
        DebugInfo();
    }
    private void IfDeclaration(Instruction instruction)
    {
        if (instruction is not Declaration decl) return;
        if (decl is Variable var) CreateVariable(var);
        if (decl is Function fun) CreateFunction(fun);

    }
    private void CreateVariable(Variable var)
    {
        SVariable newVariable = new()
        {
            Name = var.Name,
            Value = var.Value,
        };
        if (!_globalVariableScope.Add(newVariable))
            throw new InterpreterException($"Variable `{var.Name}` already declared!");
    }
    private void CreateFunction(Function fun)
    {

    }



    [Conditional("DEBUG")]
    void PrintInstruction(Instruction ins)
    {
        Console.WriteLine(ins);
    }
    [Conditional("DEBUG")]
    void DebugInfo()
    {
        Console.WriteLine();
        foreach(var v in _globalVariableScope) 
        {
            Console.WriteLine(v.ToString());
        }
    }
}
