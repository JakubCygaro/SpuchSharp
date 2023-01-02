using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Instructions;
using SpuchSharp.Parsing;
using SpuchSharp.Tokens;
using System.Text.Json;

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
        if (statement is Assignment ass) 
            AssignValue(_globalVariableScope, ass);
    }
    private Value EvaluateExpression(Dictionary<Ident, SVariable> scope, Expression expr)
    {
        PrintExpression(expr);
        return expr switch
        {
            SimpleExpression s => EvaluateSimple(scope, s),
            ComplexExpression c => EvaluateComplex(scope, c),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    private Value EvaluateSimple(Dictionary<Ident, SVariable> scope, SimpleExpression expr)
    {
        return expr switch
        {
            ValueExpression v => v.Val,
            IdentExpression i => FindVariable(i.Ident, scope).Value,
            CallExpression c => throw new NotImplementedException("Calls implementation todo"),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    private Value EvaluateComplex(Dictionary<Ident, SVariable> scope, ComplexExpression expr)
    {
        var left = EvaluateExpression(scope, expr.Left);
        var right = EvaluateExpression(scope, expr.Right);

        return expr switch
        {
            AddExpr => Value.Add(left, right),
            SubExpr => Value.Sub(left, right),
            MulExpr => Value.Mul(left, right),
            AndExpr => Value.And(left, right),
            OrExpr => Value.Or(left, right),
            EqExpr => Value.Eq(left, right),
            InEqExpr => Value.InEq(left, right),
            _ => throw new InterpreterException("FUCK")
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
    
    private void CreateVariable(Dictionary<Ident, SVariable> scope, Variable var)
    {
        SVariable newVariable = new()
        {
            Value = EvaluateExpression(scope, var.Expr),
        };
        if (!_globalVariableScope.TryAdd(new Ident { Value = var.Name }, newVariable))
            throw new InterpreterException($"Variable `{var.Name}` already declared!");
    }
    private void AssignValue(Dictionary<Ident, SVariable> scope, Assignment ass)
    {
        var svar = FindVariable(ass.Left, scope);
        var val = EvaluateExpression(scope, ass.Expr);
        if (!svar.Value.Ty.Equals(val.Ty))
            throw new InterpreterException("Mismatched types!");
        svar.Value = val;
    }
    private void CreateFunction(Function fun)
    {

    }

    [Conditional("DEBUG")]
    void PrintExpression(Expression expr)
    {
        //if (expr is ComplexExpression c)
        //{
        //    var ser = JsonSerializer.Serialize(c, new JsonSerializerOptions() { WriteIndented = true });
        //    Console.WriteLine(ser);
        //}
        Console.WriteLine($$"""
            Expression 
            {
                Type: {{expr}}
                Display: {{expr.Display()}}
            }
            """);
    }

    [Conditional("DEBUG")]
    void PrintInstruction(Instruction ins)
    {
        Console.WriteLine($$"""
            Instruction
            {
                Type: {{ins}}
            }
            """);
    }

    [Conditional("DEBUG")]
    void DebugInfo()
    {
        Console.WriteLine($"""

            Execution Ended...

            ///////DEBUG///////
            
            Variables:
            {printVariables()}

            ///////DEBUG///////
            """);

        string printVariables()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var (ident, svar) in _globalVariableScope)
            {
                sb.AppendLine($"[{ident.Stringify()}, {svar.Value.Stringify()}]");
            }
            return sb.ToString();
        }
    }
}
