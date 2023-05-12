using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Interpreting;
internal abstract class SVariable : SObject
{
    public abstract Tokens.Value Value { get; set; }
    public bool Const { get; set; } = false;
}
internal class SSimpleVariable : SVariable
{
    public override string Display() => $"{Value.Ty.Stringify()} = {Value.ValueAsObject}";
    public override required Tokens.Value Value { get; set; }
}

/// <summary>
/// Implementing object informs that it can be treated as an array
/// </summary>
internal interface IAsArray
{
    public ArrayValue AsArray { get; }
}
internal class SArray : SVariable
{
    public Ty Ty { get; init; }
    public int Size { get; }
    public override Value Value { get => _arrayValue; set => _arrayValue = (ArrayValue)value; }
    private ArrayValue _arrayValue;

    private bool _const;
    new public bool Const 
    {
        get 
        {
            return _const;
        }
        set
        {
            _arrayValue.Const = value;
            _const = value;
        }
    }

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
        catch (InterpreterException ie)
        {
            throw ie;
        }
        catch (Exception e)
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
