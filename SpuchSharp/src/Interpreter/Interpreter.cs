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

using VariableScope = 
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope = 
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;

namespace SpuchSharp.Interpreting;
public sealed class Interpreter
{
    private readonly Parser _parser;

    private VariableScope _globalVariableScope = new();
    private FunctionScope _globalFunctionScope = new();
    internal Interpreter(Parser parser) 
    {
        _parser = parser;
    }
    public Interpreter(string sourceFilePath) : this(GetParserFromSource(sourceFilePath)) { }
    static Parser GetParserFromSource(string path)
    {
        return new Parser(new Lexing.Lexer(File.ReadAllLines(path, Encoding.UTF8)));
    }
    private void ExcecuteInstruction(Instruction instruction, 
        VariableScope varScope, 
        FunctionScope funScope)
    {
        PrintInstruction(instruction);
        if(instruction is Statement stmt)
        {
            IfDeclaration(varScope, funScope, stmt);
            IfAssignment(varScope, funScope, stmt);
        }
        if (instruction is Expression expr)
            EvaluateExpression(varScope, funScope, expr);

        PrintScope(varScope);
    }
    public void Run()
    {
        foreach(var instruction in _parser)
        {
            //ExcecuteInstruction(instruction, _globalVariableScope, _globalFunctionScope);
            GlobalExcecute(instruction);
        }
        RunMain();
        DebugInfo();
    }
    /// <summary>
    /// This is what the interpreter does before calling main(), all instructions are evaluated
    /// on global scopes
    /// </summary>
    private void GlobalExcecute(Instruction instruction)
    {
        PrintInstruction(instruction);
        //only process variable and function declarations, ignore everything else
        if (instruction is Statement stmt)
        {
            IfDeclaration(_globalVariableScope, _globalFunctionScope, stmt);
            //IfAssignment(varScope, funScope, stmt);
        }
        else
            throw new InterpreterException($"Only variable and function declarations can happen in" +
                $" the global scope.", instruction);
    }
    private void RunMain()
    {
        var mainIdent = new Ident { Value = "main" };
        if (!_globalFunctionScope.TryGetValue(mainIdent, out var main))
            throw new InterpreterException("Could not find a main() fuction to begin execution.");
        if(main.Args.Length != 0)
            throw new InterpreterException("The main() function cannot take any arguments.");
        EvaluateCall(_globalVariableScope,
            _globalFunctionScope,
            new CallExpression
            {
                Args = Array.Empty<Expression>(),
                Function = mainIdent,
            });
    }
    private void IfDeclaration(VariableScope varScope, FunctionScope funScope, Statement instruction)
    {
        if (instruction is not Declaration decl) return;
        if (decl is Variable var) CreateVariable(varScope, funScope, var);
        if (decl is Function fun) CreateFunction(fun, funScope);
    }
    private void IfAssignment(VariableScope scope, FunctionScope funScope, Statement statement)
    {
        if (statement is Assignment ass) 
            AssignValue(scope, funScope, ass);
    }
    private Value EvaluateExpression(VariableScope scope, FunctionScope funScope, Expression expr)
    {
        PrintExpression(expr);
        return expr switch
        {
            SimpleExpression s => EvaluateSimple(scope, funScope, s),
            ComplexExpression c => EvaluateComplex(scope, funScope, c),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    private Value EvaluateSimple(VariableScope scope, FunctionScope funScope, SimpleExpression expr)
    {
        return expr switch
        {
            ValueExpression v => v.Val,
            IdentExpression i => FindVariable(i.Ident, scope).Value,
            CallExpression c => EvaluateCall(scope, funScope, c),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    private Value EvaluateCall(VariableScope scope, FunctionScope funScope, CallExpression call)
    {
        var targetFunction = FindFunction(call.Function, funScope);
        if (targetFunction.Args.Length != call.Args.Length)
            throw new InterpreterException($"Expected {targetFunction.Args.Length + 1} arguments " +
                $"got {call.Args.Length + 1}", call.Function);
        VariableScope variables = new();
        for(int i = 0; i < targetFunction.Args.Length; i++)
        {
            var value = EvaluateExpression(scope, funScope, call.Args[i]);
            if (!targetFunction.Args[i].Ty.Equals(value.Ty))
                throw new InterpreterException("Mismatched argument type!", call.Function);
            variables.Add(targetFunction.Args[i].Name, new SVariable
            {
                Ident = targetFunction.Args[i].Name,
                Value = value,
            });
        }
        variables.Extend(_globalVariableScope);
        var newFunScope = new FunctionScope();
        newFunScope.Extend(_globalFunctionScope);

        foreach (var instruction in targetFunction.Block)
            ExcecuteInstruction(instruction, variables, newFunScope);

        return Value.Void;

        //throw new NotImplementedException("Function call evaluation TODO");
    }
    private Value EvaluateComplex(VariableScope scope, FunctionScope funScope, ComplexExpression expr)
    {
        var left = EvaluateExpression(scope, funScope, expr.Left);
        var right = EvaluateExpression(scope, funScope, expr.Right);

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
    private SVariable FindVariable(Ident ident, VariableScope scope)
    {
        if (scope.TryGetValue(ident, out var v))
        {
            return v;
        }
        else throw new InterpreterException($"No variable {ident.Value} declared in this scope", ident);
        
    }
    private SFunction FindFunction(Ident ident, FunctionScope scope)
    {
        if(scope.TryGetValue(ident, out var f))
        {
            return f;
        }
        else throw new InterpreterException($"No function {ident.Value} declared in this scope");
    }

    private void CreateVariable(VariableScope scope, FunctionScope funScope, Variable var)
    {
        SVariable newVariable = new()
        {
            Ident = new Ident { Value = var.Name },
            Value = EvaluateExpression(scope, funScope, var.Expr),
        };
        if (!scope.TryAdd(newVariable.Ident, newVariable))
            throw new InterpreterException($"Variable `{var.Name}` already declared!");
    }
    private void AssignValue(VariableScope scope, FunctionScope funScope, Assignment ass)
    {
        var svar = FindVariable(ass.Left, scope);
        var val = EvaluateExpression(scope, funScope, ass.Expr);
        if (!svar.Value.Ty.Equals(val.Ty))
            throw new InterpreterException("Mismatched types!");
        svar.Value = val;
    }
    private void CreateFunction(Function fun, FunctionScope funScope)
    {
        if (funScope.ContainsKey(fun.Name))
            throw new InterpreterException($"Function {fun.Name} already exists!", fun.Name);
        funScope.Add(fun.Name, new SFunction
        {
            Args = fun.Args,
            Block = fun.Block,
            Ident = fun.Name,
        });
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

            Functions:
            {printFunctions()}

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
        string printFunctions()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var (ident, sfun) in _globalFunctionScope)
            {
                sb.AppendLine($"[{ident.Stringify()}, {sfun.Display()}]");
            }
            return sb.ToString();
        }
    }
    [Conditional("DEBUG")]
    void PrintCall(CallExpression call)
    {
        Console.WriteLine(call.Display());
    }
    [Conditional("DEBUG")]
    void PrintScope(VariableScope variableScope)
    {
        Console.WriteLine("VARIABLE SCOPE:");
        foreach(var (i, V) in variableScope)
        {
            Console.WriteLine($"[{i.Stringify()} {V.Value.Stringify()}]");
        }
        Console.WriteLine();
    }
}

internal static class ScopeExt
{
    /// <summary>
    /// Clones a <c>VariableScope</c>
    /// </summary>
    /// <param name="other"></param>
    /// <returns>A new <c>VariableScope</c></returns>
    public static VariableScope Clone(this VariableScope other)
    {
        //return other.ToDictionary(entry => entry.Key, entry => entry.Value);
        return new VariableScope(other);
    }
    /// <summary>
    /// Clones a <c>FunctionScope</c>
    /// </summary>
    /// <param name="other"></param>
    /// <returns>A new <c>FunctionScope</c></returns>
    public static FunctionScope Clone(this FunctionScope other)
    {
        //return other.ToDictionary(entry => entry.Key, entry => entry.Value);
        return new FunctionScope(other);
    }
    public static void Extend(this VariableScope scope, VariableScope other) 
    {
        foreach (var pair in other)
            if (!scope.TryAdd(pair.Key, pair.Value))
                throw new InterpreterException(
                    $"Variable {pair.Key.Stringify()} already declared, ", pair.Key);
    }
    public static void Extend(this FunctionScope scope, FunctionScope other)
    {
        foreach (var pair in other)
            if (!scope.TryAdd(pair.Key, pair.Value))
                throw new InterpreterException(
                    $"Function {pair.Key.Stringify()} already declared, ", pair.Key);
    }
}
