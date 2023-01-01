using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Instructions;
using SpuchSharp.Parsing;
using SpuchSharp.Tokens;

namespace SpuchSharp.Interpreting;

public sealed class Interpreter
{
    private readonly Parser _parser;

    private Dictionary<Ident, SVariable> _globalVariableScope = new();
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
            if(instruction is Statement stmt)
            {
                IfDeclaration(stmt);
                IfAssignment(stmt);
            }
            if (instruction is Expression expr)
                EvaluateExpression(_globalVariableScope, expr);
        }
        DebugInfo();
    }
    private void IfDeclaration(Statement instruction)
    {
        if (instruction is not Declaration decl) return;
        if (decl is Variable var) CreateVariable(_globalVariableScope, var);
        if (decl is Function fun) CreateFunction(fun);
    }
    private void IfAssignment(Statement statement)
    {
        if (statement is not Assignment ass) return;
        AssignValue(ass);
    }
    private Value EvaluateExpression(Dictionary<Ident, SVariable> scope, Expression expr)
    {
        PrintExpression(expr);
        Value val =  expr switch
        {
            ValueExpression ve => ve.Val,
            IdentExpression ie => FindVariable(ie.Ident, scope).Value,
            CallExpression cle => throw new InterpreterException("Call Expressions TODO"),
            ComplexExpression cxe => 
        };

    }
    private SVariable FindVariable(Ident ident, Dictionary<Ident, SVariable> scope)
    {
        if (scope.TryGetValue(ident, out var v))
        {
            return v;
        }
        else throw new InterpreterException($"No variable {ident.Value} declared in this scope");
        
    }
    [Conditional("DEBUG")]
    void PrintExpression(Expression expr)
    {
        Console.WriteLine(expr.Display());
    }
    private void CreateVariable(Dictionary<Ident, SVariable> scope, Variable var)
    {
        SVariable newVariable = new()
        {
            Name = var.Name,
            Value = EvaluateExpression(scope, var.Expr),
        };
        if (!_globalVariableScope.TryAdd(new Ident { Value = var.Name }, newVariable))
            throw new InterpreterException($"Variable `{var.Name}` already declared!");
    }
    private void AssignValue(Assignment ass)
    {
        throw new NotImplementedException("Assignment handling is TODO");
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
