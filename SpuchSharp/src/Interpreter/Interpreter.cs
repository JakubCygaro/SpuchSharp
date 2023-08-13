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
using SpuchSharp.Interpreting.Ext;

using VariableScope = 
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope = 
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;
using StructScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Tokens.StructTy>;

using System.Collections.Immutable;

namespace SpuchSharp.Interpreting;

public sealed class Interpreter
{
    private readonly Parser _parser;
    private readonly ProjectSettings _settings;
    private Module _rootModule;
    private readonly string[] _args;

    private Dictionary<Ident, Module> _importedExternalLibs = new();
    internal Interpreter(Parser parser, 
        ProjectSettings projectSettings,
        string[] args) 
    {
        _args = args;
        _parser = parser;
        _settings = projectSettings;
        _rootModule = new()
        {
            FunctionScope = new(),
            VariableScope = new(),
            StructScope = new(),
            Ident = new Ident { Value = "root" },
            Modules = new(),
            ParentModule = null,
            OwnedFunctions = new(),
            OwnedVariables = new(),
            OwnedStructs = new(),
            DirectoryPath = Path.GetDirectoryName(Path.GetFullPath(projectSettings.EntryPoint))!
        };
    }
    public Interpreter(ProjectSettings projectSettings, string[]? args = null) 
        : this(GetParserFromSource(projectSettings.EntryPoint), 
              projectSettings,
              args ?? new string[0]) { }
    /// <summary>
    /// For debug purposes
    /// </summary>
    /// <param name="source"> A source code literal </param>
    public Interpreter(string source, string[]? args = null)
        : this(new Parser(TokenStream.ParseFromQuote(source)), 
              ProjectSettings.Debug,
              args ?? new string[0]) { }
    static Parser GetParserFromSource(string path)
    {
        var tokenStream = Lexing.Lexer.Tokenize(File.ReadAllText(path, Encoding.UTF8), path);
        return new Parser(tokenStream);
    }
    Value? ExcecuteInstruction(Instruction instruction, 
        VariableScope varScope,
        FunctionScope funScope,
        Module module,
        LoopContext? loopContext = default)
    {
        //debug
        Debug.PrintScope(module.VariableScope);
        Debug.PrintInstruction(instruction);


        if(instruction is Statement stmt)
        {
            if (stmt is FunctionDecl)
                throw new InterpreterException("Disallowed function declaration", stmt);
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
    Value? EvaluateWhileLoop(VariableScope varScope,
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
    Value? EvaluateForLoop(VariableScope varScope, 
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
            Const = false,
        };
        newVarScope.Add(variable);

        for(int x = from; x < to; x += increase)
        {
            if (context.ShouldBreak)
                break;
            variableValue.Value = x;
            returnValue = ExcecuteBlock(block, newVarScope, funScope, module, loopContext: context);
        }
        return returnValue;
    }
    Value? EvaluateBreak(BreakStatement breakStmt, LoopContext? loopContext)
    {
        if (loopContext is null)
            throw new InterpreterException("Break statement outside of a loop block", breakStmt);
        loopContext.ShouldBreak = true;
        return Value.Nothing;
    }
    Value? EvaluateSkip(SkipStatement skipStatement, LoopContext? loopContext)
    {
        if (loopContext is null)
            throw new InterpreterException("Skip statement outside of a loop block", skipStatement);
        return Value.Nothing;
    }
    Value? EvaluateLoop(VariableScope varScope, 
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
    Value? ExcecuteBlock(Instruction[] block,
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
    public int Run()
    {
        Run(_parser.ToArray(),
            _rootModule,
            _importedExternalLibs);
        return RunMain();
        //DebugInfo();
    }
    void Run(Instruction[] instructions, 
        Module module,
        Dictionary<Ident, Module> importedLibs)
    {
        foreach (var instruction in instructions)
        {
            GlobalExcecute(instruction, module, importedLibs);
        }

        module.VariableScope.Extend(module.OwnedVariables);

        foreach (var instruction in instructions)
        {
            ExcecuteModules(instruction, module, importedLibs);
        }
    }
    void ExcecuteModules(Instruction instruction,
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
    void GlobalExcecute(Instruction instruction, 
        Module module,
        Dictionary<Ident, Module> importedLibs)
    {
        Debug.PrintInstruction(instruction);
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
                //IfDeclaration(module.VariableScope, module.FunctionScope, module, stmt, importedLibs);
                if (stmt is FunctionDecl fun)
                    CreateFunction(fun, module);
                else if (stmt is VariableDecl var)
                {
                    //Console.WriteLine("VARIABLE CREATION:");
                    //Console.WriteLine(var.Name);

                    CreateVariable(module.OwnedVariables, 
                        module.FunctionScope, 
                        module, 
                        var, 
                        toleratePub: true);


                    //module.OwnedVariables.PrintScope();
                    //module.VariableScope.PrintScope();

                    //module.VariableScope.Extend(module.OwnedVariables);
                }
                else if (stmt is ArrayDecl arr)
                {
                    CreateArray(module.OwnedVariables, 
                        module.FunctionScope, 
                        module, 
                        arr, 
                        toleratePub: true);
                    //module.VariableScope.Extend(module.OwnedVariables);
                }
                else if (stmt is StructDecl sd)
                {
                    CreateStructType(sd, module);
                    //Console.WriteLine($"Struct declaration detected: \n{sd.Stringify()}");
                }
                IfImport(stmt, module, importedLibs);
            }
            //IfAssignment(varScope, funScope, stmt);
        }
        else
            throw new InterpreterException($"Only variable and function declarations can happen in" +
                $" the global module scope.", instruction);
    }
    void CreateStructType(StructDecl structDecl,
        Module module)
    {
        StructTy structTy = new(structDecl.Name, structDecl.Fields) 
        { 
            IsPublic = structDecl.IsPublic 
        };
        module.OwnedStructs.Add(structTy);
        module.StructScope.Add(structTy);
        //throw InterpreterException.UnsuportedInstruction(structDecl);
    }
    void UseModule(UseStmt useStmt, Module module)
    {
        Module toInclude = module;

        foreach(var ident in useStmt.ModulePath)
        {
            if (ident == "super")
                toInclude = toInclude.ParentModule?.ValueOrDefault() ??
                    throw new InterpreterException("TODO super module unavaliable");
            else
            {
                if (toInclude.Modules.GetValueOrDefault(ident) is null)
                {
                    if (toInclude.OwnedVariables.GetValueOrDefault(ident) is SVariable variable)
                    {
                        module.VariableScope.AddPublic(variable);
                        return;
                    }
                    else if (toInclude.OwnedFunctions.GetValueOrDefault(ident) is SFunction function)
                    {
                        module.FunctionScope.AddPublic(function);
                        return;
                    }
                    else
                        throw new InterpreterException("TODO function or variable not found", useStmt);
                }
                else
                    toInclude = toInclude.Modules.GetValueOrDefault(ident) ??
                        throw new InterpreterException("TODO module unavaliable");

                if (!toInclude.IsPublic &&
                        !ReferenceEquals(toInclude.ParentModule?.ValueOrDefault(), module) &&
                        !ReferenceEquals(module.ParentModule?.ValueOrDefault(), toInclude))
                    throw new InterpreterException("Cannot use an unpublic module", useStmt);
            }
        }
        if (!toInclude.IsPublic &&
                !ReferenceEquals(toInclude.ParentModule?.ValueOrDefault(), module) &&
                !ReferenceEquals(module.ParentModule?.ValueOrDefault(), toInclude))
            throw new InterpreterException("Cannot use an unpublic module", useStmt);

        module.VariableScope.ExtendPublic(toInclude.OwnedVariables);
        module.FunctionScope.ExtendPublic(toInclude.OwnedFunctions);
    }
    /// <summary>
    /// Creates a new <c>Module</c> from a <c>ModuleDecl</c> and adds it as a child of the provided <c>Module</c>
    /// instance
    /// </summary>
    /// <param name="modDecl">Module declaration</param>
    /// <param name="module">Parent module</param>
    /// <param name="importedLibs">Global imports</param>
    /// <exception cref="InterpreterException"></exception>
    /// <exception cref="ParserException"></exception>
    void CreateModule(ModuleDecl modDecl, 
        Module module,
        Dictionary<Ident, Module> importedLibs)
    {
        var modules = module.Modules;

        if (modules.ContainsKey(modDecl.Ident))
            throw new InterpreterException($"Module already used at " +
                $"{modules[modDecl.Ident].Ident.Location}", modDecl);

        var dir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(module.DirectoryPath);

        Instruction[] moduleInstructions;
        var supposedSource = modDecl.Ident.Value;
        string path;
        if (File.Exists(supposedSource + ".spsh"))
        {
            path = supposedSource + ".spsh";
            moduleInstructions = GetParserFromSource(modDecl.Ident.Value + ".spsh").ToArray();
        }
        else if (Directory.Exists(supposedSource))
        {
            path = Path.Combine(supposedSource, "mod.spsh");
            moduleInstructions = GetParserFromSource(path).ToArray();
        }
        else
            throw new ParserException($"Could not locate source file for module `{supposedSource}`");

        var directoryPath = Path.GetDirectoryName(Path.GetFullPath(path)) ??
                                    throw new InterpreterException($"Could not determine module directory path for module `{supposedSource}`");

        Directory.SetCurrentDirectory(dir);

        var newModule = new Module
        {
            FunctionScope = new FunctionScope(),
            VariableScope = new VariableScope(),
            StructScope = new StructScope(),
            Ident = modDecl.Ident,
            Modules = new(),
            ParentModule = new(module),
            OwnedFunctions = new(),
            OwnedVariables = new(),
            OwnedStructs = new(),
            IsPublic = modDecl.IsPublic,
            DirectoryPath = directoryPath
        };
        Run(moduleInstructions, newModule, importedLibs);
        modules.Add(modDecl.Ident, newModule);
    }
    Value EvaluateReturn(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        ReturnStatement returnStatement)
    {
        return EvaluateExpression(varScope, funScope, module, returnStatement.Expr);
    }
    void IfImport(Statement instruction, 
        Module module, 
        Dictionary<Ident, Module> importedLibs)
    {
        var funScope = module.FunctionScope;
        if (instruction is not ImportStatement import) 
            return;
        string importPath;
        Ident importIdent;
        if (_settings.ExternalLibs.ContainsKey(import.Path))
        {
            importPath = _settings.ExternalLibs[import.Path];
            importPath = Path.GetFullPath(importPath);
            importIdent = Path.GetFileNameWithoutExtension(importPath).AsIdent();
        }
        else
        {
            importIdent = import.Path.AsIdent();
            importPath = Path.Combine(Importer.GetExternalLibsGlobalPath(), import.Path + ".dll");
        }
        if (importedLibs.ContainsKey(importIdent))
        {
            funScope.Extend(importedLibs[importIdent].OwnedFunctions, false);
        }
        else
            try 
            {
                if (!File.Exists(importPath))
                    throw new Exception($"Could not locate external library file, with name " +
                        $"`{importIdent.Stringify()}`");
                var mod = Importer.ImportModule(importPath, importIdent);
                importedLibs.Add(mod.Ident, mod);
                funScope.Extend(importedLibs[importIdent].OwnedFunctions, false);
            }
            catch (Exception ex) 
            {
                throw new InterpreterException(ex.Message, ex);
            }
    }
    private int RunMain()
    {
        var mainIdent = new Ident { Value = "main" };
        if (!_rootModule.FunctionScope.TryGetValue(mainIdent, out var main))
            throw new InterpreterException($"Could not find a main() fuction to begin execution, file: {Path.GetFullPath(_settings.EntryPoint)}");
        Value ret;
        if(main.Args.Length == 0 && main.ReturnTy is VoidTy or IntTy)
        {
            ret = EvaluateCall(_rootModule.VariableScope,
                _rootModule.FunctionScope,
                _rootModule,
                new CallExpression
                {
                    Args = Array.Empty<Expression>(),
                    Function = mainIdent,
                    Location = null,
                });
        }
        else if (main.Args.Length == 2 && main.ReturnTy is VoidTy or IntTy)
        {
            ConstantExpression argc = new ()
            {
                Val = new IntValue() 
                {
                    Value = _args.Length
                },
                Location = default
            };

            //if (_args.Length == 0)
            //{

            //}
            var args = _args
                .Select(s => new TextValue(s))
                //.Select(v => new ConstantExpression() 
                //{ 
                //    Location = default,
                //    Val = v,
                //})
                .ToArray();

            //var argv = new ArrayExpression()
            //{
            //    Expressions = args,
            //    Location = default
            //};

            var argv = new ArrayValue(Ty.Text, _args.Length)
            {
                Values = args,
            };


            var callArgs = new Expression[]
            {
                argc,
                new ConstantExpression()
                {
                    Val = argv,
                    Location = default,
                }
            };

            ret = EvaluateCall(_rootModule.VariableScope,
                _rootModule.FunctionScope,
                _rootModule,
                new CallExpression
                {
                    Args = callArgs,
                    Function = mainIdent,
                    Location = null,
                });
        }
        else
            throw new InterpreterException($"Could not find a main() fuction to begin execution, file: {Path.GetFullPath(_settings.EntryPoint)}");
        if (ret is IntValue i)
            return i;
        else 
            return 0;
    }
    void IfDeclaration(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        Statement instruction,
        Dictionary<Ident, Module>? importedLibs = null)
    {
        if (instruction is not Declaration decl) 
            return;
        if (decl is VariableDecl var)
        {
            if (var.IsPublic)
                throw new InterpreterException("Local variable declaration cannot be public", var);
            CreateVariable(varScope, funScope, module, var);
        }
        //else if (decl is Function fun)
        //    CreateFunction(fun, module);
        else if (decl is ArrayDecl arr)
        {
            if (arr.IsPublic)
                throw new InterpreterException("Local array declaration cannot be public", arr);
            CreateArray(varScope, funScope, module, arr);
        }
        //else if (decl is ModuleDecl modDecl)
        //    CreateModule(modDecl, module, importedLibs ?? throw new InterpreterException("Internal error" +
        //        " no set of imported external libraries was provided for Interpreter.CreateModule"));
        else
            throw new InterpreterException("Unallowed declaration statement", decl);

    }

    void CreateArray(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        ArrayDecl arrayDecl,
        bool toleratePub = false)
    {
        if (arrayDecl.IsPublic && !toleratePub)
            throw new InterpreterException("Variable declaration cannot be public in this scope", arrayDecl);

        ArrayValue arrayValue;
        Ty declType;
        SArray array;
        if (arrayDecl is TypedArrayDecl typedArr)
        {
            if (typedArr.Sized is not null)
            {
                var sArray = CreateSizedArray(varScope, funScope, module, typedArr);

                varScope.Add(sArray);
                return;
            }
            else if (arrayDecl.ArrayExpression is null)
            {
                declType = typedArr.Type;
                arrayValue = (ArrayValue)declType.DefaultValue();
                array = new SArray(declType, arrayValue.Size)
                {
                    Ident = new Ident() { Value = arrayDecl.Name },
                    Value = arrayValue,
                    IsPublic = arrayDecl.IsPublic,
                    Const = arrayDecl.Const,
                };
            }
            else if (arrayDecl.ArrayExpression is Expression expr)
            {
                arrayValue = EvaluateExpression(varScope, funScope, module, expr) as ArrayValue ??
                    throw new InterpreterException("Expression is not an array", expr);

                array = new SArray(typedArr.Type, arrayValue.Size)
                {
                    Ident = new Ident() { Value = arrayDecl.Name },
                    Value = typedArr.Type.Cast(arrayValue),
                    IsPublic = arrayDecl.IsPublic,
                    Const = arrayDecl.Const,
                };
            }
            else
                throw new InterpreterException("TODO Typed Array creation failed");
        }
        else /*if (arrayDecl.ArrayExpression is not null)*/
        {
            arrayValue = (ArrayValue)EvaluateExpression(varScope, funScope, module, arrayDecl.ArrayExpression ??
                throw new InterpreterException("Untyped variable declaration must contain assignment", arrayDecl));
            declType = arrayValue.Ty;
            if (!arrayValue.ValueTy.Equals(declType))
                throw new InterpreterException("Array type does not match the assigned value", arrayValue);
            array = new SArray(declType, arrayValue.Size)
            {
                Ident = new Ident() { Value = arrayDecl.Name },
                Value = arrayValue,
                IsPublic = arrayDecl.IsPublic,
                Const = arrayDecl.Const,
            };
        }

        varScope.Add(array);
    }
    SArray CreateSizedArray(VariableScope varScope, 
        FunctionScope funScope, 
        Module module,
        TypedArrayDecl typedArr)
    {
        var sizes = new LinkedList<(int size, Ty type)>();
        //var arrayType = ArrayTy.ArrayOf(typedArr.Type);
        var arrayType = (ArrayTy)typedArr.Type;

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
            IsPublic = typedArr.IsPublic,
            Const = typedArr.Const,
        };
        sizes.RemoveFirst();
        var val = (ArrayValue)sArray.Value;
        FillArrays(ref val, sizes);
        return sArray;
    }
    void FillArrays(ref ArrayValue arrayValue, LinkedList<(int size, Ty type)> sizes)
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
    Value? IfIfStatement(VariableScope varScope, 
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
    void IfDeletion(VariableScope varScope, FunctionScope funScope, Statement stmt)
    {
        if (stmt is not DeleteStatement delete)
            return;
        var sVar = varScope.FindVariable(delete.VariableIdent);
        DeleteVariable(sVar.Ident, varScope);
    }
    void DeleteVariable(Ident ident, VariableScope varScope)
    {
        varScope.Remove(ident);
    }
    void IfAssignment(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        Statement statement)
    {
        if (statement is Assignment ass) 
            AssignValue(varScope, funScope,  module, ass);
    }
    Value EvaluateExpression(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        Expression expr)
    {
        Debug.PrintExpression(expr);
        return expr switch
        {
            SimpleExpression s => EvaluateSimple(varScope, funScope, module, s),
            ComplexExpression c => EvaluateComplex(varScope, funScope, module, c),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    Value EvaluateSimple(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        SimpleExpression expr)
    {
        return expr switch
        {
            ConstantExpression v => v.Val,
            IdentExpression i => varScope.FindVariable(i.Ident).Value,
            CallExpression c => EvaluateCall(varScope, funScope, module, c),
            IndexerExpression id => EvaluateIndexer(varScope, funScope, module, id),
            ArrayExpression ae => EvalueArrayExpression(varScope, funScope, module, ae),
            NotExpression ne => EvaluateNot(varScope, funScope, module, ne),
            IncrementExpression ie => EvaluateIncrement(varScope, funScope, module, ie),
            DecrementExpression de => EvaluateDecrement(varScope, funScope, module, de),
            CastExpression ce => EvaluateCast(varScope, funScope, module, ce),
            StructExpression se => EvaluateStruct(varScope, funScope, module, se),
            FieldExpression fe => EvaluateField(varScope, funScope, module, fe),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    Value EvaluateField(VariableScope varScope, 
        FunctionScope funScope, 
        Module module, 
        FieldExpression fieldExpression)
    {
        var structValue = EvaluateExpression(varScope,
            funScope,
            module,
            fieldExpression.StructProducer)
            as StructValue ??
            throw new InterpreterException("EVALUATE FIELD TODO 1");

        return structValue.Fields.GetValueOrDefault(fieldExpression.FieldName) ??
            throw new InterpreterException("EVALUATE FIELD TODO 2");
    }
    Value EvaluateStruct(VariableScope varScope, 
        FunctionScope funScope, 
        Module module, 
        StructExpression structExpression)
    {
        var structTy = module.StructScope.GetValueOrDefault(structExpression.Type) ??
            throw new InterpreterException(
                $"No struct type `{structExpression.Type.Stringify()}` defined in this scope", structExpression);

        Dictionary<Ident, Value> fields = new();
        foreach(var (ident, expr) in structExpression.FiendsExpressions)
        {
            fields.Add(ident, EvaluateExpression(varScope, funScope, module, expr));
        }
        return structTy.New(fields);
    }
    Value EvaluateCast(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        CastExpression castExpression)
    {
        var val = EvaluateExpression(varScope, funScope, module, castExpression.Expression);
        var casted = castExpression.TargetType.Cast(val);
        return casted;
    }
    Value EvaluateDecrement(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        DecrementExpression decrementExpression)
    {
        var target = varScope.FindSimpleVariable(decrementExpression.Expression.Ident);
        if (target.Const)
            throw InterpreterException.ConstantReassignment(decrementExpression.Expression.Ident,
                decrementExpression.Location);

        var val = EvaluateExpression(varScope, funScope, module, decrementExpression.Expression);

        switch (val)
        {
            case ShortValue s:
                if (decrementExpression.Pre)
                {
                    --s.Value;
                    return s;
                }
                else
                {
                    var ret = s.Clone();
                    --s.Value;
                    return ret;
                }
            case IntValue i:
                if (decrementExpression.Pre)
                {
                    --i.Value;
                    return i;
                }
                else
                {
                    var ret = i.Clone();
                    --i.Value;
                    return ret;
                }
            case LongValue l:
                if (decrementExpression.Pre)
                {
                    --l.Value;
                    return l;
                }
                else
                {
                    var ret = l.Clone();
                    --l.Value;
                    return ret;
                }
            case FloatValue f:
                if (decrementExpression.Pre)
                {
                    --f.Value;
                    return f;
                }
                else
                {
                    var ret = f.Clone();
                    --f.Value;
                    return ret;
                }
            case DoubleValue d:
                if (decrementExpression.Pre)
                {
                    --d.Value;
                    return d;
                }
                else
                {
                    var ret = d.Clone();
                    --d.Value;
                    return ret;
                }
            default:
                throw new InterpreterException("Invalid use of decrement", decrementExpression);
        }
    }
    Value EvaluateIncrement(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        IncrementExpression incrementExpression)
    {
        var target = varScope.FindSimpleVariable(incrementExpression.Expression.Ident);
        if (target.Const)
            throw InterpreterException.ConstantReassignment(incrementExpression.Expression.Ident, 
                incrementExpression.Location);

        var val = EvaluateExpression(varScope, funScope, module, incrementExpression.Expression);

        switch (val)
        {
            case ShortValue s:
                if (incrementExpression.Pre)
                {
                    ++s.Value;
                    return s;
                }
                else
                {
                    var ret = s.Clone();
                    ++s.Value;
                    return ret;
                }
            case IntValue i:
                if (incrementExpression.Pre)
                {
                    ++i.Value;
                    return i;
                }
                else
                {
                    var ret = i.Clone();
                    ++i.Value;
                    return ret;
                }
            case LongValue l:
                if (incrementExpression.Pre)
                {
                    ++l.Value;
                    return l;
                }
                else
                {
                    var ret = l.Clone();
                    ++l.Value;
                    return ret;
                }
            case FloatValue f:
                if (incrementExpression.Pre)
                {
                    ++f.Value;
                    return f;
                }
                else
                {
                    var ret = f.Clone();
                    ++f.Value;
                    return ret;
                }
            case DoubleValue d:
                if (incrementExpression.Pre)
                {
                    ++d.Value;
                    return d;
                }
                else
                {
                    var ret = d.Clone();
                    ++d.Value;
                    return ret;
                }
            default:
                throw new InterpreterException("Invalid use of increment", incrementExpression);
        }
    }
    Value EvaluateNot(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        NotExpression notExpression)
    {
        var val = EvaluateExpression(varScope, funScope, module, notExpression.Expr) as BooleanValue ??
            throw new InterpreterException(
                "Failed to evaluate operand to a boolean value for negation operator", notExpression);
        val.Value = !val.Value;
        return val;
    }
    Value EvalueArrayExpression(VariableScope varScope,
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
    Value EvaluateIndexer(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        IndexerExpression expr)
    {
        //var arrayValue = EvaluateExpression(varScope, funScope, module, expr.ArrayProducer) as ArrayValue 
        //    ?? throw new InterpreterException("Cannot index into a non-array type", expr.ArrayProducer);
        ArrayValue arrayValue;
        var value = EvaluateExpression(varScope, funScope, module, expr.ArrayProducer);
        if(value is IAsArray asArray)
        {
            arrayValue = asArray.AsArray;
        }
        else
            arrayValue = value as ArrayValue
                ?? throw new InterpreterException("Cannot index into a non-array type", expr.ArrayProducer);

        var index = EvaluateExpression(varScope, funScope, module, expr.IndexExpression) as IntValue
            ?? throw new InterpreterException("An array index must be of integer type", expr.IndexExpression);
        return arrayValue[index];
    }

    Ty ProduceType(Optional<Ty, Ident> optional, Module module)
    {
        if (optional.HasLeft)
            return optional.LeftOrThrow;
        else
            return FindRuntimeType(optional.RightOrThrow, module);
    }
    Ty FindRuntimeType(Ident ident, Module module)
    {
        return module.StructScope.FindTy(ident) ??
            throw new InterpreterException($"No type `{ident.Stringify()}` defined in this scope");
    }

    Value EvaluateCall(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        CallExpression call)
    {
        //get the function that should be called
        var targetFunction = funScope.FindFunction(call.Function);

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
        List<SVariable> deconst = new();

        foreach(var (argument, index) in call.Args.Select((x, i) => (x, i)))
        {
            Ty ty;
            if (targetFunction.Args[index].Ref)
            {
                if(argument is not IdentExpression identExpression)
                    throw new InterpreterException("A ref argument can only be a variable name", argument);
                var variable = varScope.FindVariable(identExpression.Ident);
                ty = ProduceType(targetFunction.Args[index].Ty, module);
                if (variable.Value.Ty != ty)
                    throw new InterpreterException($"Mismatched argument type, " +
                        $"expected variable of type {ty.Stringify()} " +
                        $"but got a variable of type {ty.Stringify()}", argument);

                if (targetFunction.Args[index].Const)
                {
                    if (!variable.Const)
                    {
                        variable.Const = true;
                        deconst.Add(variable);
                    }
                }
                else
                {
                    if (variable.Const)
                        throw new ConstReferenceException(
                            $"Cannot pass a non-constant reference to a constant variable `{variable.Ident.Stringify()}`", call);
                }
                newVariables.Add(targetFunction.Args[index].Name, variable);
                continue;
            }
            var value = EvaluateExpression(varScope, funScope, module, argument);
            ty = ProduceType(targetFunction.Args[index].Ty, module);
            if (ty != value.Ty)
                throw new InterpreterException($"Mismatched argument type, " +
                        $"expected variable of type {ty.Stringify()} " +
                        $"but got a variable of type {ty.Stringify()}", argument);

            if (ty is ArrayTy arrayTy)
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
                    Const = targetFunction.Args[index].Const,
                });
            }
            else if(ty is StructTy structTy)
            {
                var valueAsStruct = value as StructValue
                    ?? throw new InterpreterException("Type mismatch, call argument was not a struct",
                    call.Args[index]);
                newVariables.Add(targetFunction.Args[index].Name, new SStruct((StructValue)valueAsStruct.Clone())
                {
                    Ident = targetFunction.Args[index].Name,
                    IsPublic = false,
                    Const = targetFunction.Args[index].Const,
                });
            }
            else
                newVariables.Add(targetFunction.Args[index].Name, new SSimpleVariable
                {
                    Ident = targetFunction.Args[index].Name,
                    Value = value,
                    IsPublic = false,
                    Const = targetFunction.Args[index].Const,
                });
        }

        if (targetFunction is ExternalFunction ext)
            return CallExternalFunction(ext, newVariables.Values.ToList(), call.Location);

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
                $"the return type of the function `{targetFunction.Ident.Stringify()}`", call);
        if (retValue is ArrayValue arrV)
            arrV.Const = false;

        foreach (var v in deconst)
            v.Const = false;

        return retValue;
    }
    Value CallExternalFunction(ExternalFunction external, 
        List<SVariable> variables,
        Location? loc = default)
    {
        return external.Invoke(variables.Select(v => v.Value.ValueAsObject).ToArray(), loc);
    }
    Value EvaluateComplex(VariableScope varScope,
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
                AddExpr => left.Add(right),
                SubExpr => left.Sub(right),
                MulExpr => left.Mul(right),
                DivExpr => left.Div(right),
                AndExpr => left.And(right),
                ModuloExpr => left.Modulo(right),
                OrExpr => left.Or(right),
                EqExpr => left.Eq(right),
                InEqExpr => left.InEq(right),
                GreaterThanExpr => left.GreaterThan(right),
                LessThanExpr => left.LessThan(right),
                GreaterOrEqToExpr => left.GreaterOrEqualTo(right),
                LessOrEqToExpr => left.LessOrEqualTo(right),
                _ => throw new InterpreterException("Unrecognized expression type", expr)
            };
        }
        catch(Exception ex) 
        {
            throw new InterpreterException(ex.Message, ex);
        }
    }
    void CreateVariable(VariableScope varScope,
        FunctionScope funScope, 
        Module module,
        VariableDecl var,
        bool toleratePub = false)
    {
        if (var.IsPublic && !toleratePub)
            throw new InterpreterException("Variable declaration cannot be public in this scope", var);

        Value value;
        if(var is StructVariableDecl structVariableDecl)
        {
            var structTy = module.StructScope.GetValueOrDefault(structVariableDecl.Type) ??
                throw new InterpreterException("STRUCT NOT FOUND TOOD");
            value = structVariableDecl.Expr switch
            {
                Expression expr => EvaluateExpression(varScope, funScope, module, expr),
                _ => structTy.DefaultValue()
            };
        }
        else if (var.Expr is null) 
        {
            if (var is not TypedVariableDecl tvd)
                throw new InterpreterException("Untyped variable declaration must contain asssignment", 
                    var);
            value = tvd.Type.DefaultValue();
        }
        else
        {
            value = EvaluateExpression(varScope, funScope, module, var.Expr);
            if (value is not ArrayValue arrv)
                value = value.Clone();
            else
            {
                if (arrv.Const && !var.Const)
                    throw InterpreterException.ConstantReassignment(var.Name, var.Location);
            }   
        }

        if (var is TypedVariableDecl typed)
        {
            value = typed.Type.SafeCast(value) ??
                throw new InterpreterException(
                    "Mismatched types, assigned type is different from declared type.", typed.Type);
        }
        SVariable newVariable = value switch
        {
            StructValue structValue => new SStruct(structValue) 
            { 
                Ident = new Ident { Value = var.Name },
                IsPublic = var.IsPublic,
                Const = var.Const,
                Value = structValue,
            },
            ArrayValue arrayValue => new SArray(arrayValue.ValueTy, arrayValue.Size) 
            { 
                Ident = new Ident { Value = var.Name },
                Value = arrayValue,
                IsPublic = var.IsPublic,
                Const = var.Const,
            },
            Value otherValue => new SSimpleVariable 
            { 
                Ident = new Ident { Value = var.Name },
                Value = otherValue,
                IsPublic = var.IsPublic,
                Const = var.Const,
            }
        };

        if (!varScope.TryAdd(newVariable.Ident, newVariable))
            throw new InterpreterException($"Variable `{var.Name}` already declared!");
    }
    void AssignValue(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        Assignment ass)
    {
        var val = EvaluateExpression(varScope, funScope, module, ass.Expr).Clone();

        if (ass.Left is ArrayIndexTarget arrayIndexTarget)
            AssignIndex(varScope, funScope, module, arrayIndexTarget, ass, val);
        else if (ass.Left is FieldTarget fieldTarget)
            AssignField(varScope, funScope, module, fieldTarget, ass, val);
        else if (ass.Left is IdentTarget identTarget)
            AssignVariable(varScope, identTarget, ass, val);
    }
    void AssignField(VariableScope varScope,
        FunctionScope funScope,
        Module module,
        FieldTarget fieldTarget,
        Assignment assignment,
        Value assignedValue)
    {
        var fieldAccess = fieldTarget.FieldExpression;
        var value = EvaluateExpression(varScope, funScope, module, fieldAccess.StructProducer) 
            as StructValue ??
            throw new InterpreterException("ASSIGN FIELD TODO 1");
        var fieldTy = value.Fields[fieldAccess.FieldName].Ty;
        value.Fields[fieldAccess.FieldName] = fieldTy.Cast(assignedValue);
    }
    void AssignIndex(VariableScope varScope, 
        FunctionScope funScope,
        Module module,
        ArrayIndexTarget arrayIndexTarget,
        Assignment assignment,
        Value assignedValue)
    {
        
        var indexer = arrayIndexTarget.Target 
            as IndexerExpression 
            ?? throw new InterpreterException("Left hand side of assingment is not an index expresion",
            arrayIndexTarget.Target);

        if(indexer.ArrayProducer is IdentExpression identExpression)
            if(varScope.FindArray(identExpression.Ident).Const)
                throw new ConstException("Tried to reassign a value of a const array",
                arrayIndexTarget.Target.Location);

        var arrayValue = EvaluateExpression(varScope, funScope, module, indexer.ArrayProducer) 
            as ArrayValue
            ?? throw new InterpreterException("Could not obtain the array for assingment", 
            indexer.ArrayProducer);

        //if (arrayValue.Const)
        //    throw new InterpreterException("Tried to reassign a value of a const array", 
        //        arrayIndexTarget.Target.Location);

        var index = EvaluateExpression(varScope, funScope, module, arrayIndexTarget.IndexExpression)
                    as IntValue
                    ?? throw new InterpreterException("Index was not of integer value",
                    arrayIndexTarget.IndexExpression);


        if (assignedValue is not ArrayValue)
            assignedValue = assignedValue.Clone();

        //if(assignedValue is ArrayValue)
        //    arrayValue[index] = assignedValue;
        //else
        //    arrayValue[index] = assignedValue.Clone();

        switch (assignment)
        {
            case RegularAssignment:
                arrayValue[index] = assignedValue;
                break;
            case AddAssignment:
                arrayValue[index] = arrayValue[index].Add(assignedValue);
                break;
            case SubAssignment:
                arrayValue[index] = arrayValue[index].Sub(assignedValue);
                break;
            case MulAssignment:
                arrayValue[index] = arrayValue[index].Mul(assignedValue);
                break;
            case DivAssignment:
                arrayValue[index] = arrayValue[index].Div(assignedValue);
                break;
            case ModuloAssignment:
                arrayValue[index] = arrayValue[index].Modulo(assignedValue);
                break;
            default:
                throw new InterpreterException("Assignment Failure");
        }


    }
    void AssignVariable(VariableScope varScope,
        IdentTarget identTarget,
        Assignment assignment,
        Value assignedValue)
    {
        var identExpr = identTarget.Target as IdentExpression ??
            throw new InterpreterException("TODO cannot assing to", identTarget.Target);
        var variable = varScope.FindVariable(identExpr.Ident);
        if (variable.Const)
            throw InterpreterException.ConstantReassignment(variable);
        //if(!variable.Value.Ty.Equals(assignedValue.Ty))

        //SArray case
        if(assignedValue is ArrayValue arrayValue)
        {
            if (assignment is not RegularAssignment)
                throw new InterpreterException("Cannot assign to array in such way", assignment);
            if (variable is not SVariable sArray)
                throw new InterpreterException("Cannot assing an array to a non array variable", 
                    identTarget.Target);
            if (sArray.Value.Ty != arrayValue.Ty)
                throw new InterpreterException("Type mismatch", identTarget.Target);
            if(!sArray.Const && arrayValue.Const)
                throw new ConstException("Cannot assing a constant array to a non constant variable", 
                    identTarget.Target);
            sArray.Value = arrayValue;
            return;
        }
        switch (assignment)
        {
            case RegularAssignment:
                variable.Value = variable.Value.Ty.SafeCast(assignedValue) ??
                    throw new InterpreterException($"Mismatched types {variable.Value.Ty.Stringify()} " +
                    $"| {assignedValue.Ty.Stringify()}", identExpr.Ident);
                break;
            case AddAssignment:
                variable.Value = variable.Value.Add(assignedValue);
                break;
            case SubAssignment:
                variable.Value = variable.Value.Sub(assignedValue);
                break;
            case MulAssignment:
                variable.Value = variable.Value.Mul(assignedValue);
                break;
            case DivAssignment:
                variable.Value = variable.Value.Div(assignedValue);
                break;
            case ModuloAssignment:
                variable.Value = variable.Value.Modulo(assignedValue);
                break;
            default:
                throw new InterpreterException("Assignment Failure");
        }
    }
    void CreateFunction(FunctionDecl fun, Module module)
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
}

