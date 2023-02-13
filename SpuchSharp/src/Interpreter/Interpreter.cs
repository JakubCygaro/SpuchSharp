using System;
using System.Collections.Generic;
using System.Diagnostics;


using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Instructions;
using SpuchSharp.Parsing;
using SpuchSharp.Tokens;
using SpuchSharp.API;

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
    private HashSet<string> _importedExternalLibs = new();
    internal Interpreter(Parser parser) 
    {
        _parser = parser;
    }
    public Interpreter(string sourceFilePath) : this(GetParserFromSource(sourceFilePath)) { }
    static Parser GetParserFromSource(string path)
    {
        return new Parser(new Lexing.Lexer(File.ReadAllLines(path, Encoding.UTF8)));
    }
    private Value? ExcecuteInstruction(Instruction instruction, 
        VariableScope varScope, 
        FunctionScope funScope,
        LoopContext? loopContext = default)
    {
        //debug
        PrintScope(varScope);
        PrintInstruction(instruction);

        if(instruction is Statement stmt)
        {
            IfDeclaration(varScope, funScope, stmt);
            IfAssignment(varScope, funScope, stmt);
            IfDeletion(varScope, funScope, stmt);
            if (stmt is IfStatement ifStatement)
                return IfIfStatement(varScope, funScope, ifStatement, loopContext: loopContext);
            if (stmt is ReturnStatement returnStatement)
                return EvaluateReturn(varScope, funScope, returnStatement);
            if (stmt is LoopStatement loopStatement)
                return EvaluateLoop(varScope, funScope, loopStatement);
            if (stmt is BreakStatement breakStatement)
                return EvaluateBreak(breakStatement, loopContext);
            if (stmt is SkipStatement skipStatement)
                return EvaluateSkip(skipStatement, loopContext);
            if (stmt is ForLoopStatement forLoopStatement)
                return EvaluateForLoop(varScope, funScope, forLoopStatement);
            if (stmt is WhileStatement whileStatement)
                return EvaluateWhileLoop(varScope, funScope, whileStatement);
        }
        if (instruction is Expression expr)
            EvaluateExpression(varScope, funScope, expr);

        return null;
    }
    private Value? EvaluateWhileLoop(VariableScope varScope,
        FunctionScope funScope,
        WhileStatement whileStatement)
    {
        var block = whileStatement.Block;
        var context = new LoopContext();
        Value? returnValue = null;

        while(EvaluateCondition() && !context.ShouldBreak)
            returnValue = ExcecuteBlock(block, varScope, funScope, loopContext: context);
        return returnValue;

        bool EvaluateCondition()
        {
            return EvaluateExpression(varScope, funScope, whileStatement.Condition)
            as BooleanValue ??
            throw new InterpreterException("The condition of a while statement must evaluate to a boolean value",
            whileStatement.Condition);
        }
    }
    private Value? EvaluateForLoop(VariableScope varScope, 
        FunctionScope funScope, 
        ForLoopStatement forLoopStmt)
    {
        var block = forLoopStmt.Block;
        var context = new LoopContext();
        Value? returnValue = null;
        var from = EvaluateExpression(varScope, funScope, forLoopStmt.From) as IntValue ??
            throw new InterpreterException("Expected int value");
        var to = EvaluateExpression(varScope, funScope, forLoopStmt.To) as IntValue ??
            throw new InterpreterException("Expected int value");

        var increase = from > to ? -1 : 1;

        var newVarScope = varScope.Clone();
        var variableValue = (IntValue)from.Clone();
        var variable = new SVariable
        {
            Ident = forLoopStmt.VariableIdent,
            Value = variableValue,
        };
        newVarScope.Add(variable);

        for(int x = from; x <= to; x += increase)
        {
            if (context.ShouldBreak)
                break;
            variableValue.Value = x;
            returnValue = ExcecuteBlock(block, newVarScope, funScope, loopContext: context);
        }
        return returnValue;
    }
    private Value? EvaluateBreak(BreakStatement breakStmt, LoopContext? loopContext)
    {
        if (loopContext is null)
            throw new InterpreterException("Break statement outside of a loop block", breakStmt);
        loopContext.ShouldBreak = true;
        return Value.Nothing;
    }
    private Value? EvaluateSkip(SkipStatement skipStatement, LoopContext? loopContext)
    {
        if (loopContext is null)
            throw new InterpreterException("Skip statement outside of a loop block", skipStatement);
        return Value.Nothing;
    }
    private Value? EvaluateLoop(VariableScope varScope, 
        FunctionScope funScope, 
        LoopStatement loopStatement)
    {
        var block = loopStatement.Block;
        var context = new LoopContext();
        Value? returnValue = null;
        while (!context.ShouldBreak)
            returnValue = ExcecuteBlock(block, varScope, funScope, loopContext: context);
        return returnValue;
    }


    /// <summary>
    /// Excecuted the provided block of instructions
    /// </summary>
    /// <remarks>
    /// Returns a non-null <c>Value</c> if any of the instructions returned a value
    /// </remarks>
    /// <param name="block"></param>
    /// <param name="varScope"></param>
    /// <param name="funScope"></param>
    /// <returns></returns>
    private Value? ExcecuteBlock(Instruction[] block,
        VariableScope varScope,
        FunctionScope funScope,
        LoopContext? loopContext = default)
    {
        foreach (var ins in block)
        {
            var returnValue = ExcecuteInstruction(ins, varScope, funScope, loopContext: loopContext);
            if (returnValue is not null and not NothingValue)
                return returnValue;
        }
        return null;
    }
    public void Run()
    {
        foreach (var instruction in _parser)
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
            IfImport(stmt, _globalFunctionScope);
            //IfAssignment(varScope, funScope, stmt);
        }
        else
            throw new InterpreterException($"Only variable and function declarations can happen in" +
                $" the global scope.", instruction);
    }
    private Value EvaluateReturn(VariableScope varScope, 
        FunctionScope funScope, 
        ReturnStatement returnStatement)
    {
        return EvaluateExpression(varScope, funScope, returnStatement.Expr);
    }
    private void IfImport(Statement instruction, FunctionScope funScope)
    {
        if (instruction is not ImportStatement import) 
            return;
        if (_importedExternalLibs.Contains(import.Path))
            throw new InterpreterException($"Cannot import an already imported library: {import.Path}", 
                import);
        try 
        {
            funScope.Extend(Importer.ImportFunctions(import.Path));
            _importedExternalLibs.Add(import.Path);
        }
        catch(Exception ex) 
        {
            throw new InterpreterException(ex.Message, ex);
        }

    }
    private void RunMain()
    {
        var mainIdent = new Ident { Value = "main" };
        if (!_globalFunctionScope.TryGetValue(mainIdent, out var main))
            throw new InterpreterException("Could not find a main() fuction to begin execution.");
        if(main.Args.Length != 0)
            throw new InterpreterException("The main() function cannot take any arguments.");
        if(main.ReturnTy is not VoidTy)
            throw new InterpreterException("The main() function cannot have a return type.");
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
        if (instruction is not Declaration decl) 
            return;
        if (decl is Variable var) 
            CreateVariable(varScope, funScope, var);
        if (decl is Function fun) 
            CreateFunction(fun, funScope);
    }
    /// <summary>
    /// Will return a non-null <c>Value</c> if any of the contained instructions
    /// was a <c>return</c> statement
    /// </summary>
    /// <param name="varScope"></param>
    /// <param name="funScope"></param>
    /// <param name="ifStmt"></param>
    /// <returns></returns>
    /// <exception cref="InterpreterException"></exception>
    private Value? IfIfStatement(VariableScope varScope, 
        FunctionScope funScope, 
        IfStatement ifStmt,
        LoopContext? loopContext = default)
    {
        var value = EvaluateExpression(varScope, funScope, ifStmt.Expr);
        if (value is not BooleanValue boolean)
            throw new InterpreterException("If statement expression must evaluate to a true/false value");
        if (boolean)
            return ExcecuteBlock(ifStmt.Block, varScope, funScope, loopContext: loopContext);
        else if(ifStmt.ElseBlock is Instruction[] elseBlock)
            return ExcecuteBlock(elseBlock, varScope, funScope, loopContext: loopContext);
        return null;
    }
    private void IfDeletion(VariableScope varScope, FunctionScope funScope, Statement stmt)
    {
        if (stmt is not DeleteStatement delete)
            return;
        var sVar = FindVariable(delete.VariableIdent, varScope);
        DeleteVariable(sVar.Ident, varScope);
    }
    private void DeleteVariable(Ident ident, VariableScope varScope)
    {
        varScope.Remove(ident);
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
        var returnType = targetFunction.ReturnTy;
        if (targetFunction.Args.Length != call.Args.Length)
            throw new InterpreterException($"Expected {targetFunction.Args.Length} arguments " +
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
        if (targetFunction is ExternalFunction ext)
            return CallExternalFunction(ext, variables.Values.ToList());

        variables.Extend(_globalVariableScope);
        var newFunScope = new FunctionScope();
        newFunScope.Extend(_globalFunctionScope);

        var retValue = ExcecuteBlock(targetFunction.Block, variables, newFunScope);
        if (retValue is NothingValue)
            retValue = null;
        retValue ??= Value.Void;

        if (!retValue.Ty.Equals(returnType))
            throw new InterpreterException(
                $"Return statement type does not match " +
                $"the return type of the function `{targetFunction.Ident.Stringify()}`");
        return retValue;
    }
    private Value CallExternalFunction(ExternalFunction external, List<SVariable> variables)
    {
        return external.Invoke(variables.Select(v => v.Value.ValueAsObject).ToArray());
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
            GreaterThanExpr => Value.GreaterThan(left, right),
            LessThanExpr => Value.LessThan(left, right),
            GreaterOrEqToExpr => Value.GreaterOrEqualTo(left, right),
            LessOrEqToExpr => Value.LessOrEqualTo(left, right),
            _ => throw new InterpreterException("Unrecognized expression type", expr)
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
        var value = EvaluateExpression(scope, funScope, var.Expr);
        if (var is Typed typed)
            if (!typed.Type.Equals(value.Ty))
                throw new InterpreterException(
                    "Mismatched types, assigned type is different from declared type.", typed.Type);
        SVariable newVariable = new()
        {
            Ident = new Ident { Value = var.Name },
            Value = value,
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
            ReturnTy = fun.ReturnTy,
        });
    }

    [Conditional("EXPRESSION")]
    void PrintExpression(Expression expr)
    {
        //if (expr is ComplexExpression c)
        //{
        //    var ser = JsonSerializer.Serialize(c, new JsonSerializerOptions() { WriteIndented = true });
        //    Console.WriteLine(ser);
        //}
        Console.WriteLine($$"""
            Expression {
                Type: {{expr}}
                Display: {{expr.Display()}}
            }
            """);
    }

    [Conditional("INSTRUCTION")]
    void PrintInstruction(Instruction ins)
    {
        Console.WriteLine($$"""
            Instruction {
                Type: {{ins}}
            }
            """);
    }

    [Conditional("DEBUG")]
    void DebugInfo()
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
                sb.AppendLine($"{ident.Stringify()}, {sfun.Display()}");
            }
            return sb.ToString();
        }
    }
    [Conditional("DEBUG")]
    void PrintCall(CallExpression call)
    {
        Console.WriteLine(call.Display());
    }
    [Conditional("SCOPE")]
    void PrintScope(VariableScope variableScope)
    {
        Console.WriteLine("=================");
        Console.WriteLine("VARIABLE SCOPE:");
        foreach(var (i, V) in variableScope)
        {
            Console.WriteLine($"[{i.Stringify()} {V.Value.Stringify()}]");
        }
        Console.WriteLine("=================");
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
                    $"Variable {pair.Key.Stringify()} already declared ", pair.Key);
    }
    public static void Extend(this FunctionScope scope, FunctionScope other)
    {
        foreach (var pair in other)
            if (!scope.TryAdd(pair.Key, pair.Value))
                throw new InterpreterException(
                    $"Function {pair.Key.Stringify()} already declared ", pair.Key);
    }
    public static void Add(this VariableScope scope, SVariable variable)
    {
        if(!scope.TryAdd(variable.Ident, variable))
            throw new InterpreterException(
                    $"Variable {variable.Ident.Stringify()} already declared ", variable.Ident);
    }
}