using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VariableScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;
using StructScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Tokens.StructTy>;

namespace SpuchSharp.Interpreting.Ext;
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
    public static void PrintScope(this VariableScope scope)
    {
        foreach((var name, var variable) in scope)
        {
            Console.WriteLine($"{name.Stringify()} = {variable.Value.Stringify()}");
        }
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
        if (!scope.TryAdd(variable.Ident, variable))
            throw new InterpreterException(
                    $"Variable {variable.Ident.Stringify()} already declared ", variable.Ident);
    }
    public static void AddPublic(this VariableScope scope, SVariable variable)
    {
        if (!variable.IsPublic)
            throw new InterpreterException($"Cannot import a variable that is not public " +
                $"`{variable.Ident.Stringify()}`");
        if (!scope.TryAdd(variable.Ident, variable))
            throw new InterpreterException(
                    $"Variable {variable.Ident.Stringify()} already declared ", variable.Ident);
    }
    public static void Add(this FunctionScope funScope, SFunction function)
    {
        if (!funScope.TryAdd(function.Ident, function))
            throw new InterpreterException(
                    $"Function {function.Ident.Stringify()} already declared ", function.Ident);
    }
    public static void AddPublic(this FunctionScope funScope, SFunction function)
    {
        if (!function.IsPublic)
            throw new InterpreterException($"Cannot import a function that is not public " +
                $"`{function.Ident.Stringify()}`");
        if (!funScope.TryAdd(function.Ident, function))
            throw new InterpreterException(
                    $"Function {function.Ident.Stringify()} already declared ", function.Ident);
    }
    public static void Add(this StructScope structScope, StructTy structTy)
    {
        if (!structScope.TryAdd(structTy.Ident, structTy))
            throw new InterpreterException(
                    $"Struct {structTy.Ident.Stringify()} already declared ", structTy.Ident);
    }
    public static void AddPublic(this StructScope structScope, StructTy structTy)
    {
        if (!structTy.IsPublic)
            throw new InterpreterException($"Cannot import a struct declaration that is not public " +
                $"`{structTy.Ident.Stringify()}`");
        if (!structScope.TryAdd(structTy.Ident, structTy))
            throw new InterpreterException(
                    $"Struct {structTy.Ident.Stringify()} already declared ", structTy.Ident);
    }
    public static SSimpleVariable FindSimpleVariable(this VariableScope scope, Ident ident)
    {
        if (scope.TryGetValue(ident, out var v))
        {
            return v as SSimpleVariable ??
                throw new InterpreterException($"Value {ident.Value} is not a variable", ident);
        }
        else throw new InterpreterException($"No variable {ident.Value} declared in this scope", ident);
    }
    public static SFunction FindFunction(this FunctionScope scope, Ident ident)
    {
        if (scope.TryGetValue(ident, out var f))
        {
            return f;
        }
        throw new InterpreterException($"No function {ident.Value} declared in this scope", ident);
    }
    public static SVariable FindVariable(this VariableScope varScope, Ident ident)
    {
        if (varScope.TryGetValue(ident, out var arr))
            return arr;
        throw InterpreterException.VariableNotFound(ident);
    }
    public static SArray FindArray(this VariableScope varScope, Ident ident)
    {
        if (varScope.TryGetValue(ident, out var arr))
            return arr as SArray ??
                throw new InterpreterException("Tried indexing into a non array type", ident);
        throw InterpreterException.VariableNotFound(ident);
    }
    public static T Find<T>(this Dictionary<Ident, T> dict, Ident ident)
        where T: SObject
    {
        if (dict.TryGetValue(ident, out var arr))
            return arr;
        throw InterpreterException.VariableNotFound(ident);
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
public static class WeakReferenceExt
{
    public static T? ValueOrDefault<T>(this WeakReference<T> weakReference)
        where T : class
    {
        if (weakReference.TryGetTarget(out var value))
        {
            return value;
        }
        return null;
    }
}