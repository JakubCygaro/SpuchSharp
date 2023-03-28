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
using System.Reflection;

using VariableScope = 
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope = 
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;
using Microsoft.VisualBasic;

namespace SpuchSharp.Interpreting;

public sealed class Interpreter
{
    private readonly Parser _parser;

    private Module _rootModule;

    //private VariableScope _rootVariableScope = new();
    //private FunctionScope _rootFunctionScope = new();
    private Dictionary<Ident, Module> _importedExternalLibs = new();
    internal Interpreter(Parser parser) 
    {
        _parser = parser;
        _rootModule = new()
        {
            FunctionScope = new(),
            VariableScope = new(),
            Ident = new Ident { Value = "root" },
            Modules = new(),
            ParentModule = null,
            OwnedFunctions = new(),
        };
    }
    public Interpreter(string sourceFilePath) : this(GetParserFromSource(sourceFilePath)) { }
    static Parser GetParserFromSource(string path)
    {
        var tokenStream = Lexing.Lexer.Tokenize(File.ReadAllLines(path, Encoding.UTF8));
        return new Parser(tokenStream);
    }
    static Value? ExcecuteInstruction(Instruction instruction, 
        VariableScope varScope,
        FunctionScope funScope,
        Module module,
        LoopContext? loopContext = default)
    {
        //debug
        PrintScope(module.VariableScope);
        PrintInstruction(instruction);


        if(instruction is Statement stmt)
        {
            IfDeclaration(varScope, funScope, module, stmt);
            IfAssignment(varScope, funScope, module, stmt);
            IfDeletion(varScope, funScope, stmt);
            if (stmt is IfStatement ifStatement)
                return IfIfStatement(varScope, funScope, module, ifStatement, loopContext: loopContext);
            if (stmt is ReturnStatement returnStatement)
                return EvaluateReturn(varScope, funScope, module, returnStatement);
            if (stmt is LoopStatement loopStatement)
                return EvaluateLoop(varScope, funScope, module, loopStatement);
            if (stmt is BreakStatement breakStatement)
                return EvaluateBreak(breakStatement, loopContext);
            if (stmt is SkipStatement skipStatement)
                return EvaluateSkip(skipStatement, loopContext);
            if (stmt is ForLoopStatement forLoopStatement)
                return EvaluateForLoop(varScope, funScope, module, forLoopStatement);
            if (stmt is WhileStatement whileStatement)
                return EvaluateWhileLoop(varScope, funScope, module, whileStatement);
        }
        if (instruction is Expression expr)
            EvaluateExpression(varScope, funScope, module, expr);

        return null;
    }
    static Value? EvaluateWhileLoop(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        WhileStatement whileStatement)
    {
        var block = whileStatement.Block;
        var context = new LoopContext();
        Value? returnValue = null;

        while(EvaluateCondition() && !context.ShouldBreak)
            returnValue = ExcecuteBlock(block, varScope, funScope, module, loopContext: context);
        return returnValue;

        bool EvaluateCondition()
        {
            return EvaluateExpression(varScope, funScope, module, whileStatement.Condition)
            as BooleanValue ??
            throw new InterpreterException("The condition of a while statement must evaluate to a boolean value",
            whileStatement.Condition);
        }
    }
    static Value? EvaluateForLoop(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        ForLoopStatement forLoopStmt)
    {
        var block = forLoopStmt.Block;
        var context = new LoopContext();
        Value? returnValue = null;
        var from = EvaluateExpression(varScope, funScope, module, forLoopStmt.From) as IntValue ??
            throw new InterpreterException("Expected int value");
        var to = EvaluateExpression(varScope, funScope, module, forLoopStmt.To) as IntValue ??
            throw new InterpreterException("Expected int value");

        var increase = from > to ? -1 : 1;

        var newVarScope = varScope.Clone();
        var variableValue = (IntValue)from.Clone();
        var variable = new SSimpleVariable
        {
            Ident = forLoopStmt.VariableIdent,
            Value = variableValue,
            IsPublic = false,
        };
        newVarScope.Add(variable);

        for(int x = from; x <= to; x += increase)
        {
            if (context.ShouldBreak)
                break;
            variableValue.Value = x;
            returnValue = ExcecuteBlock(block, newVarScope, funScope, module, loopContext: context);
        }
        return returnValue;
    }
    static Value? EvaluateBreak(BreakStatement breakStmt, LoopContext? loopContext)
    {
        if (loopContext is null)
            throw new InterpreterException("Break statement outside of a loop block", breakStmt);
        loopContext.ShouldBreak = true;
        return Value.Nothing;
    }
    static Value? EvaluateSkip(SkipStatement skipStatement, LoopContext? loopContext)
    {
        if (loopContext is null)
            throw new InterpreterException("Skip statement outside of a loop block", skipStatement);
        return Value.Nothing;
    }
    static Value? EvaluateLoop(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        LoopStatement loopStatement)
    {
        var block = loopStatement.Block;
        var context = new LoopContext();
        Value? returnValue = null;
        while (!context.ShouldBreak)
            returnValue = ExcecuteBlock(block, varScope, funScope, module, loopContext: context);
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
    static Value? ExcecuteBlock(Instruction[] block,
        VariableScope varScope,
        FunctionScope funScope,
        Module module,
        LoopContext? loopContext = default)
    {
        foreach (var ins in block)
        {
            var returnValue = ExcecuteInstruction(ins, varScope, funScope, module, loopContext: loopContext);
            if (returnValue is not null and not NothingValue)
                return returnValue;
        }
        return null;
    }
    public void Run()
    {
        Run(_parser.ToArray(),
            _rootModule,
            _importedExternalLibs);
        RunMain();
        //DebugInfo();
    }
    static void Run(Instruction[] instructions, 
        Module module,
        Dictionary<Ident, Module> importedLibs)
    {
        foreach (var instruction in instructions)
        {
            GlobalExcecute(instruction, module, importedLibs);
        }
        foreach (var instruction in instructions)
        {
            ExcecuteModules(instruction, module, importedLibs);
        }
    }
    static void ExcecuteModules(Instruction instruction,
        Module module,
        Dictionary<Ident, Module> importedLibs)
    {
        if (instruction is ModuleDecl modDecl)
            CreateModule(modDecl, module, importedLibs);
        else if (instruction is UseStmt useStmt)
            UseModule(useStmt, module);
    }
    /// <summary>
    /// This is what the interpreter does before calling main(), all instructions are evaluated
    /// on root scopes
    /// </summary>
    static void GlobalExcecute(Instruction instruction, 
        Module module,
        Dictionary<Ident, Module> importedLibs)
    {
        PrintInstruction(instruction);
        //only process variable and function declarations, ignore everything else
        if (instruction is Statement stmt)
        {
            if (stmt is ModuleDecl modDecl)
                return;
                //CreateModule(modDecl, module, importedLibs);
            else if (stmt is UseStmt useStmt)
                return;
                //UseModule(useStmt, module);
            else
            {
                IfDeclaration(module.VariableScope, module.FunctionScope, module, stmt, importedLibs);
                IfImport(stmt, module, importedLibs);
            }
            //IfAssignment(varScope, funScope, stmt);
        }
        else
            throw new InterpreterException($"Only variable and function declarations can happen in" +
                $" the global module scope.", instruction);
    }
    static void UseModule(UseStmt useStmt, Module module)
    {
        Module toInclude = module;

        foreach(var ident in useStmt.ModulePath)
        {
            if (ident == "super")
                toInclude = toInclude.ParentModule?.ValueOrDefault() ??
                    throw new InterpreterException("TODO super module unavaliable");
            else
                toInclude = toInclude.Modules.GetValueOrDefault(ident) ??
                    throw new InterpreterException("TODO module unavaliable");

        }

        module.VariableScope.ExtendPublic(toInclude.VariableScope);
        module.FunctionScope.ExtendPublic(toInclude.OwnedFunctions);
    }
    static void CreateModule(ModuleDecl modDecl, 
        Module module,
        Dictionary<Ident, Module> importedLibs)
    {
        var modules = module.Modules;

        if (modules.ContainsKey(modDecl.Ident))
            throw new InterpreterException($"Module already used at " +
                $"{modules[modDecl.Ident].Ident.Location}", modDecl);
        var moduleInstructions = GetParserFromSource(modDecl.Ident.Value + ".spsh").ToArray();
        var newModule = new Module
        {
            FunctionScope = new FunctionScope(),
            VariableScope = new VariableScope(),
            Ident = modDecl.Ident,
            Modules = new(),
            ParentModule = new (module),
            OwnedFunctions = new(),
        };
        Run(moduleInstructions, newModule, importedLibs);
        modules.Add(modDecl.Ident, newModule);
    }
    static Value EvaluateReturn(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        ReturnStatement returnStatement)
    {
        return EvaluateExpression(varScope, funScope, module, returnStatement.Expr);
    }
    static void IfImport(Statement instruction, 
        Module module, 
        Dictionary<Ident, Module> importedLibs)
    {
        var funScope = module.FunctionScope;
        if (instruction is not ImportStatement import) 
            return;

        //Console.WriteLine($"Import in module: {module.Ident.Stringify()}");

        //foreach (var function in funScope)
        //{
        //    Console.WriteLine($"{function.Key.Stringify()}");
        //}

        var importPath = import.Path.AsIdent();
        if (importedLibs.ContainsKey(importPath))
        {
            funScope.Extend(importedLibs[importPath].OwnedFunctions, false);
        }
        else
            try 
            {
                var mod = Importer.ImportModule(import.Path, importPath);
                importedLibs.Add(mod.Ident, mod);
                funScope.Extend(importedLibs[importPath].OwnedFunctions, false);
            }
            catch (Exception ex) 
            {
                throw new InterpreterException(ex.Message, ex);
            }
        //foreach (var function in funScope)
        //{
        //    Console.WriteLine($"{function.Key.Stringify()}\n");
        //}
    }
    private void RunMain()
    {
        var mainIdent = new Ident { Value = "main" };
        if (!_rootModule.FunctionScope.TryGetValue(mainIdent, out var main))
            throw new InterpreterException("Could not find a main() fuction to begin execution.");
        if(main.Args.Length != 0)
            throw new InterpreterException("The main() function cannot take any arguments.");
        if(main.ReturnTy is not VoidTy)
            throw new InterpreterException("The main() function cannot have a return type.");
        EvaluateCall(_rootModule.VariableScope, 
            _rootModule.FunctionScope,
            _rootModule,
            new CallExpression
            {
                Args = Array.Empty<Expression>(),
                Function = mainIdent,
                Location = null,
            });
    }
    static void IfDeclaration(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        Statement instruction,
        Dictionary<Ident, Module>? importedLibs = null)
    {
        if (instruction is not Declaration decl) 
            return;
        if (decl is Variable var)
            CreateVariable(varScope, funScope, module, var);
        else if (decl is Function fun)
            CreateFunction(fun, module);
        else if (decl is ArrayDecl arr)
            CreateArray(varScope, funScope, module, arr);
        //else if (decl is ModuleDecl modDecl)
        //    CreateModule(modDecl, module, importedLibs ?? throw new InterpreterException("Internal error" +
        //        " no set of imported external libraries was provided for Interpreter.CreateModule"));
        else
            throw new InterpreterException("Unallowed declaration statement", decl);

    }
    //static void CreateModule(ModuleDecl modDecl, 
    //    Module module, 
    //    HashSet<string> importedLibs)
    //{
    //    var file = modDecl.Ident.Value + ".spsh";
    //    var instructions = GetParserFromSource(file).ToArray();
    //    var newModule = new Module
    //    {
    //        FunctionScope = new(),
    //        VariableScope = new(),
    //        Ident = modDecl.Ident,
    //        Modules = new(),
    //        ParentModule = module,
    //    };
    //    Run(instructions, newModule, importedLibs);
    //    module.Modules.Add(newModule.Ident, newModule);
    //}
    static void CreateArray(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        ArrayDecl arrayDecl)
    {
        if (arrayDecl is TypedArrayDecl typedArr)
            if (typedArr.Sized is not null)
            {
                var sArray = CreateSizedArray(varScope, funScope, module, typedArr); 
                
                varScope.Add(sArray);
                return;
            }
        //var values = new List<Value>();
        //foreach (var expr in arrayDecl.Expressions)
        //{
        //    values.Add(EvaluateExpression(varScope, funScope, expr));
        //}
        //if (values.Count == 0)
        //    throw new InterpreterException("Empty array declaration", arrayDecl);
        var arrayValue = (ArrayValue)EvaluateExpression(varScope, funScope, module, arrayDecl.ArrayExpression);
        var declType = arrayDecl switch
        {
            TypedArrayDecl typed => typed.Type,
            _ => arrayValue.ValueTy,
        };

        if (!arrayValue.ValueTy.Equals(declType))
            throw new InterpreterException("Array type does not match the assigned value", arrayValue);
        var array = new SArray(declType, arrayValue.Size)
        {
            Ident = new Ident() { Value = arrayDecl.Name },
            Value = arrayValue,
            IsPublic = false,
        };
        //var index = 0;
        //foreach (var value in values)
        //{
        //    array.Set(index, value);
        //    index++;
        //}
        varScope.Add(array);
    }
    static SArray CreateSizedArray(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        TypedArrayDecl typedArr)
    {
        var sizes = new LinkedList<(int size, Ty type)>();
        var arrayType = ArrayTy.ArrayOf(typedArr.Type);
        foreach (var size in typedArr.Sized!)
        {
            var s = EvaluateExpression(varScope, funScope, module, size)
                as IntValue
                ?? throw new InterpreterException("Array size expression must be of integer value", size);
            var ty = arrayType.OfType;
            sizes.AddLast((s, ty));
            arrayType = ArrayTy.ArrayOf(ty);
        }

        var sArray = new SArray(sizes.First!.Value.type, sizes.First!.Value.size)
        {
            Ident = new Ident { Value = typedArr.Name },
            IsPublic = false,
        };
        sizes.RemoveFirst();
        var val = (ArrayValue)sArray.Value;
        FillArrays(ref val, sizes);
        return sArray;
    }
    static void FillArrays(ref ArrayValue arrayValue, LinkedList<(int size, Ty type)> sizes)
    {
        if (sizes.Count == 0)
            return;
        for(int i = 0; i < arrayValue.Values.Count(); i++)
            arrayValue.Values[i] = new ArrayValue(sizes.First!.Value.type, sizes.First!.Value.size);
        sizes.RemoveFirst();
        foreach (var valueArray in arrayValue.Values)
        {
            var val = (ArrayValue)valueArray;
            FillArrays(ref val, sizes);
        }
    }


    /// <summary>
    /// Will return a non-null <c>Value</c> if any of the contained instructions
    /// was a <c>return</c> statement, but will return a <c>NothingValue</c> if a break or skip statement was
    /// enqountered
    /// </summary>
    /// <param name="varScope"></param>
    /// <param name="funScope"></param>
    /// <param name="ifStmt"></param>
    /// <returns></returns>
    /// <exception cref="InterpreterException"></exception>
    static Value? IfIfStatement(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        IfStatement ifStmt,
        LoopContext? loopContext = default)
    {
        var value = EvaluateExpression(varScope, funScope, module, ifStmt.Expr);
        if (value is not BooleanValue boolean)
            throw new InterpreterException("If statement expression must evaluate to a true/false value");
        if (boolean)
            return ExcecuteBlock(ifStmt.Block, varScope, funScope, module, loopContext: loopContext);
        else if(ifStmt.ElseBlock is Instruction[] elseBlock)
            return ExcecuteBlock(elseBlock, varScope, funScope, module, loopContext: loopContext);
        return null;
    }
    static void IfDeletion(VariableScope varScope, FunctionScope funScope, Statement stmt)
    {
        if (stmt is not DeleteStatement delete)
            return;
        var sVar = FindVariable(varScope, delete.VariableIdent);
        DeleteVariable(sVar.Ident, varScope);
    }
    static void DeleteVariable(Ident ident, VariableScope varScope)
    {
        varScope.Remove(ident);
    }
    static void IfAssignment(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        Statement statement)
    {
        if (statement is Assignment ass) 
            AssignValue(varScope, funScope,  module, ass);
    }
    static Value EvaluateExpression(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        Expression expr)
    {
        PrintExpression(expr);
        return expr switch
        {
            SimpleExpression s => EvaluateSimple(varScope, funScope, module, s),
            ComplexExpression c => EvaluateComplex(varScope, funScope, module, c),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    static Value EvaluateSimple(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        SimpleExpression expr)
    {
        return expr switch
        {
            ValueExpression v => v.Val,
            IdentExpression i => FindVariable(varScope, i.Ident).Value,
            CallExpression c => EvaluateCall(varScope, funScope, module, c),
            IndexerExpression id => EvaluateIndexer(varScope, funScope, module, id),
            ArrayExpression ae => EvalueArrayExpression(varScope, funScope, module, ae),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    static Value EvalueArrayExpression(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        ArrayExpression arrayExpression)
    {
        var expressions = arrayExpression.Expressions;
        var values = expressions.Select(expr => EvaluateExpression(varScope, funScope, module, expr))
                                .ToArray();
        var size = values.Count();
        if (size == 0)
            throw new InterpreterException("An empty array cannot be initialized");
        return new ArrayValue(values[0].Ty, size)
        {
            Values = values,
        };
    }
    static Value EvaluateIndexer(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        IndexerExpression expr)
    {
        //Console.WriteLine("DEBUG INDEXER");
        //Console.WriteLine(expr.Display());
        var arrayValue = EvaluateExpression(varScope, funScope, module, expr.ArrayProducer) as ArrayValue 
            ?? throw new InterpreterException("Cannot index into a non-array type", expr.ArrayProducer);

        var index = EvaluateExpression(varScope, funScope, module, expr.IndexExpression) as IntValue
            ?? throw new InterpreterException("An array index must be of integer type", expr.IndexExpression);
        return arrayValue[index];
    }
    static SVariable FindVariable(VariableScope varScope, Ident ident)
    {
        if (varScope.TryGetValue(ident, out var arr))
            return arr;
        throw InterpreterException.VariableNotFound(ident);
    }
    static SArray FindArray(VariableScope varScope, Ident ident)
    {
        if (varScope.TryGetValue(ident, out var arr))
            return arr as SArray ??
                throw new InterpreterException("Tried indexing into a non array type", ident);
        throw InterpreterException.VariableNotFound(ident);
    }
    static Value EvaluateCall(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        CallExpression call)
    {
        //get the function that should be called
        var targetFunction = FindFunction(call.Function, funScope);

        Module functionModule;
        if(targetFunction.ParentModule?.ValueOrDefault() is Module m)
        {
            if (object.ReferenceEquals(module, m))
                functionModule = module;
            else
                functionModule = m;
        }
        else
        {
            functionModule = module;
        }

        //get the return type of that function
        var returnType = targetFunction.ReturnTy;
        //check whether argument amount makes sense
        if (targetFunction.Args.Length != call.Args.Length)
            throw new InterpreterException($"Expected {targetFunction.Args.Length} arguments " +
                $"got {call.Args.Length}", call.Function);
        //variable scope for the function
        VariableScope newVariables = new();

        foreach(var (argument, index) in call.Args.Select((x, i) => (x, i)))
        {
            if (targetFunction.Args[index].Ref)
            {
                if(argument is not IdentExpression identExpression)
                    throw new InterpreterException("A ref argument can only be a variable name", argument);
                var variable = FindVariable(varScope, identExpression.Ident);
                if (variable.Value.Ty != targetFunction.Args[index].Ty)
                    throw new InterpreterException($"Mismatched argument type, " +
                        $"expected variable of type {targetFunction.Args[index].Ty.Stringify()} " +
                        $"but got a variable of type {variable.Value.Ty.Stringify()}", argument);

                newVariables.Add(targetFunction.Args[index].Name, variable);
                continue;
            }
            var value = EvaluateExpression(varScope, funScope, module, argument);
            if (targetFunction.Args[index].Ty != value.Ty)
                throw new InterpreterException($"Mismatched argument type, " +
                        $"expected variable of type {targetFunction.Args[index].Ty.Stringify()} " +
                        $"but got a variable of type {value.Ty.Stringify()}", argument);

            if (targetFunction.Args[index].Ty is ArrayTy arrayTy)
            {
                var valueAsArray = value as ArrayValue
                    ?? throw new InterpreterException("Type mismatch, call argument was not an array", 
                    call.Args[index]);

                newVariables.Add(targetFunction.Args[index].Name, new SArray(arrayTy.OfType, 
                    (valueAsArray).Size)
                {
                    Ident = targetFunction.Args[index].Name,
                    Value = valueAsArray,//.Clone()
                    IsPublic = false,
                    
                });
            }
            else
                newVariables.Add(targetFunction.Args[index].Name, new SSimpleVariable
                {
                    Ident = targetFunction.Args[index].Name,
                    Value = value,
                    IsPublic = false,
                });
        }

        if (targetFunction is ExternalFunction ext)
            return CallExternalFunction(ext, newVariables.Values.ToList());

        newVariables.Extend(functionModule.VariableScope);
        var newFunScope = new FunctionScope();
        newFunScope.Extend(functionModule.FunctionScope);

        var retValue = ExcecuteBlock(targetFunction.Block,
            newVariables, 
            functionModule.FunctionScope, 
            functionModule);

        if (retValue is NothingValue)
            retValue = null;

        retValue ??= Value.Void;

        if (!retValue.Ty.Equals(returnType))
            throw new InterpreterException(
                $"Return statement type does not match " +
                $"the return type of the function `{targetFunction.Ident.Stringify()}`");
        return retValue;
    }
    static Value CallExternalFunction(ExternalFunction external, List<SVariable> variables)
    {
        return external.Invoke(variables.Select(v => v.Value.ValueAsObject).ToArray());
    }
    static Value EvaluateComplex(VariableScope varScope,
        FunctionScope funScope, 
        Module module,
        ComplexExpression expr)
    {
        var left = EvaluateExpression(varScope, funScope, module, expr.Left);
        var right = EvaluateExpression(varScope, funScope, module, expr.Right);
        try
        {
            return expr switch
            {
                AddExpr => Value.Add(left, right),
                SubExpr => Value.Sub(left, right),
                MulExpr => Value.Mul(left, right),
                DivExpr => Value.Div(left, right),
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
        catch(Exception ex) 
        {
            throw new InterpreterException(ex.Message, ex);
        }
    }
    static SSimpleVariable FindSimpleVariable(Ident ident, VariableScope scope)
    {
        if (scope.TryGetValue(ident, out var v))
        {
            return v as SSimpleVariable ??
                throw new InterpreterException($"Value {ident.Value} is not a variable", ident);
        }
        else throw new InterpreterException($"No variable {ident.Value} declared in this scope", ident);
        
    }
    static SFunction FindFunction(Ident ident, FunctionScope scope)
    {
        if (scope.TryGetValue(ident, out var f))
        {
            return f;
        }
            throw new InterpreterException($"No function {ident.Value} declared in this scope", ident);
    }

    static void CreateVariable(VariableScope varScope,
        FunctionScope funScope, 
        Module module,
        Variable var)
    {
        var value = EvaluateExpression(varScope, funScope, module, var.Expr);
        if (var is Typed typed)
            if (!typed.Type.Equals(value.Ty))
                throw new InterpreterException(
                    "Mismatched types, assigned type is different from declared type.", typed.Type);
        SVariable newVariable = value switch
        {
            ArrayValue arrayValue => new SArray(arrayValue.ValueTy, arrayValue.Size) 
            { 
                Ident = new Ident { Value = var.Name },
                Value = arrayValue,
                IsPublic = false,
            },
            Value otherValue => new SSimpleVariable 
            { 
                Ident = new Ident { Value = var.Name },
                Value = otherValue,
                IsPublic = false,
            }
        };

        if (!varScope.TryAdd(newVariable.Ident, newVariable))
            throw new InterpreterException($"Variable `{var.Name}` already declared!");
    }
    static void AssignValue(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        Assignment ass)
    {
        //var svar = FindSimpleVariable(ass.Left.Ident, scope);
        var val = EvaluateExpression(varScope, funScope, module, ass.Expr);

        if (ass.Left is ArrayIndexTarget arrayIndexTarget)
            AssignIndex(varScope, funScope, module, arrayIndexTarget, val);
        else if(ass.Left is IdentTarget identTarget)
            AssignVariable(varScope, identTarget, val);
    }
    static void AssignIndex(VariableScope varScope, 
        FunctionScope funScope,
        Module module,
        ArrayIndexTarget arrayIndexTarget,
        Value assignedValue)
    {
        
        var indexer = arrayIndexTarget.Target 
            as IndexerExpression 
            ?? throw new InterpreterException("Left hand side of assingment is not an index expresion",
            arrayIndexTarget.Target);
        var arrayValue = EvaluateExpression(varScope, funScope, module, indexer.ArrayProducer) 
            as ArrayValue
            ?? throw new InterpreterException("Could not obtain the array for assingment", 
            indexer.ArrayProducer);

        var index = EvaluateExpression(varScope, funScope, module, arrayIndexTarget.IndexExpression)
                    as IntValue
                    ?? throw new InterpreterException("Index was not of integer value",
                    arrayIndexTarget.IndexExpression);

        arrayValue[index] = assignedValue.Clone();
    }
    static void AssignVariable(VariableScope varScope,
        IdentTarget identTarget,
        Value assignedValue)
    {
        var identExpr = identTarget.Target as IdentExpression ??
            throw new InterpreterException("TODO cannot assing to", identTarget.Target);
        var variable = FindVariable(varScope, identExpr.Ident);
        if(!variable.Value.Ty.Equals(assignedValue.Ty))
            throw new InterpreterException("Mismatched types", identExpr.Ident);
        variable.Value = assignedValue;
    }
    static void CreateFunction(Function fun, Module module)
    {
        var ownedFunctions = module.OwnedFunctions;
        if (ownedFunctions.ContainsKey(fun.Name))
            throw new InterpreterException($"Function {fun.Name} already exists!", fun.Name);
        var function = new SFunction
        {
            Args = fun.Args,
            Block = fun.Block,
            Ident = fun.Name,
            ReturnTy = fun.ReturnTy,
            IsPublic = fun.IsPublic,
            ParentModule = new(module),
        };
        ownedFunctions.Add(fun.Name, function);
        module.FunctionScope.Add(fun.Name, function);
    }

    [Conditional("EXPRESSION")]
    static void PrintExpression(Expression expr)
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
    static void PrintInstruction(Instruction ins)
    {
        Console.WriteLine($$"""
            Instruction {
                Type: {{ins}}
            }
            """);
    }

    [Conditional("DEBUG")]
    static void DebugInfo(VariableScope varScope, FunctionScope funScope)
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
    static void PrintCall(CallExpression call)
    {
        Console.WriteLine(call.Display());
    }
    [Conditional("SCOPE")]
    static void PrintScope(VariableScope variableScope)
    {
        Console.WriteLine("=================");
        Console.WriteLine("VARIABLE SCOPE:");
        foreach(var (i, V) in variableScope)
        {
            Console.WriteLine($"[{i.Stringify()} {V.Display()}]");
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
    public static void Extend(this VariableScope scope, 
        VariableScope other,
        bool noExternal = true) 
    {
        foreach (var pair in other)
            if (!scope.TryAdd(pair.Key, pair.Value))
                throw new InterpreterException(
                    $"Variable {pair.Key.Stringify()} already declared ", pair.Key);
    }
    public static void ExtendPublic(this VariableScope scope, 
        VariableScope other)
    {
        foreach (var pair in other.Where(v => v.Value.IsPublic))
            if (!scope.TryAdd(pair.Key, pair.Value))
                throw new InterpreterException(
                    $"Variable {pair.Key.Stringify()} already declared ", pair.Key);
    }
    public static void Extend(this FunctionScope scope, 
        FunctionScope other, 
        bool noExternal = true)
    {
        foreach (var pair in other.Where(f => f.Value is not ExternalFunction == noExternal))
            if (!scope.TryAdd(pair.Key, pair.Value))
                throw new InterpreterException(
                    $"Function {pair.Key.Stringify()} already declared ", pair.Key);
    }
    public static void ExtendPublic(this FunctionScope scope, 
        FunctionScope other,
        bool noExternal = true)
    {
        foreach (var pair in other.Where(f => f.Value.IsPublic && 
                                            f.Value is not ExternalFunction == noExternal))
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
public static class EnumerableExt
{
    public static void Each<T>(this IEnumerable<T> ie, Action<T, int> action)
    {
        var i = 0;
        foreach (var e in ie) action(e, i++);
    }
}
public static class WeakReferenceExr
{
    public static T? ValueOrDefault<T>(this WeakReference<T> weakReference)
        where T: class
    {
        if(weakReference.TryGetTarget(out var value))
        {
            return value;
        }
        return null;
    }
}