using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.API;
using SpuchSharp.Instructions;
using SpuchSharp.Tokens;

namespace SpuchSharp.Interpreting;

internal abstract class SObject
{
    public required Ident Ident { get; init; }
    public override int GetHashCode()
    {
        return Ident.GetHashCode();
    }

    public abstract string Display();
}

internal abstract class SVariable : SObject 
{
    public abstract Tokens.Value Value { get; set; }
}
internal class SSimpleVariable : SVariable
{
    public override string Display() => $"{Value.Ty.Stringify()} = {Value.ValueAsObject}";
    public override required Tokens.Value Value { get; set; }
}
internal class SArray : SVariable
{
    public Ty Ty { get; init; }
    public int Size { get; }
    public override Value Value { get => _arrayValue; set => _arrayValue = (ArrayValue)value; }
    private ArrayValue _arrayValue;
    
    public SArray(Ty ty, int size)
    {
        Ty = ty;
        _arrayValue = new ArrayValue(ty, size);
    }
    public void Set(int index, Value value)
    {
        try
        {
            if (!value.Ty.Equals(Ty))
                throw new InterpreterException($"A value of type {value.Ty.Stringify()} " +
                    $"cannot be held in an array of type {Ty.Stringify()}");
            _arrayValue[index] = value;
        }
        catch(InterpreterException ie)
        {
            throw ie;
        }
        catch(Exception e)
        {
            throw new InterpreterException(e.Message, e);
        }
    }
    public T Get<T>(int index)
        where T : Value
    {
        try
        {
            return _arrayValue[index] as T ??
                throw new InterpreterException("Array value invalid cast");
        }
        catch (Exception e)
        {
            throw new InterpreterException(e.Message, e);
        }
    }
    public override string Display()
    {
        return $"[{Ty}] {Ident.Value} [{Size}]";
    }

}

internal class SFunction : SObject
{
    public override string Display()
    {
        StringBuilder sb = new();
        sb.AppendLine($"{Ident}");
        sb.Append('(');
        foreach(var arg in Args)
            sb.Append($"{arg.Ty} {arg.Name},");
        sb.Append(')');
        return sb.ToString();
    }
    public required Parsing.FunArg[] Args { get; init; }
    public required Instruction[] Block { get; init; }
    public required Ty ReturnTy { get; init; }
}

internal class ExternalFunction : SFunction
{
    public required System.Reflection.MethodInfo MethodInfo { private get; init; }
    public Value Invoke(object[] arguments)
    {
        object? ret = null;
        try
        {
            ret = MethodInfo.Invoke(null, arguments);
        }
        catch(Exception ex) 
        {
            throw new ExternalLibraryException(ex.Message, ex);
        }
        // schizo type marshalling back into spuch#
        return Value.FromObject(ret);
    }
    public override string Display()
    {
        Console.WriteLine("[EXTERNAL FUNCTION]");
        StringBuilder sb = new();
        sb.AppendLine($"{MethodInfo.Name}");
        sb.Append('(');
        foreach (var arg in Args)
            sb.Append($"{arg.Ty} {arg.Name},");
        sb.Append(')');
        return sb.ToString();
    }
}

internal class SStruct : SObject
{
    public override string Display()
    {
        StringBuilder sb = new();
        sb.AppendLine($$"""struct {{Ident.Stringify()}} {""");
        foreach(var (ident, value) in Fields)
        {
            sb.AppendLine($"{value.Ty} {ident.Stringify()} = {value.ValueAsObject};");
        }
        sb.AppendLine(@"}");
        return sb.ToString();
    }
    public required Dictionary<Ident, Value> Fields { get; init; } = new();

}

