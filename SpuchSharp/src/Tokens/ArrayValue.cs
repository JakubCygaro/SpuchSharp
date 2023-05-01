using SpuchSharp.Interpreting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal sealed class ArrayValue : Value
{
    /// <summary>
    /// Type of the array
    /// </summary>
    public override Ty Ty => _arrayTy;
    /// <summary>
    /// Type of the <c>Value</c> held in this array
    /// </summary>
    public Ty ValueTy { get; }

    private Ty _arrayTy;
    public int Size { get; }

    private Value[] _values;
    public Value[] Values { get => _values; init => _values = value; }
    public Value this[int index] { get => Get(index); set => Set(index, value); }

    public bool Const { get; set; }

    private Value Get(int index)
    {
        try
        {
            return Values[index];
        }
        catch (Exception ex) 
        {
            throw new Interpreting.InterpreterException(ex.Message);
        }
    }
    private void Set(int index, Value value)
    {
        try
        {
            if (Values[index].Ty != ValueTy)
                value = Ty.SafeCast(value) ??
                    throw new Interpreting.InterpreterException($"Tried to assing a {value.Ty.Stringify()} " +
                        $"value to an array of type {ValueTy.Stringify()}");
            Values[index] = value;
        }
        catch (Exception ex)
        {
            throw new Interpreting.InterpreterException(ex.Message);
        }
    }

    public ArrayValue(Ty ofType, int size)
    {
        ValueTy = ofType;
        _arrayTy = ArrayTy.ArrayOf(ofType);
        Size = size;
        _values = new Value[Size];
        for (int i = 0; i < size; i++)
            Values[i] = Value.Default(ValueTy, size);

    }
    private ArrayValue(Ty valueTy, Ty arrayTy, int size, Value[] values)
    {
        ValueTy = valueTy;
        _arrayTy = arrayTy;
        Size = size;
        _values = values;
    }

    public override string Stringify()
    {
        return $"{Ty.Stringify()}";
    }

    public override object ValueAsObject => Values.Select(x => x.ValueAsObject).ToArray();
    public override Value Clone()
    {
        var ret = new ArrayValue(ValueTy, this.Ty, Size, (Value[])_values.Clone());
        ret.Const = this.Const;
        return ret;
    }
}
//internal sealed class IntArrayValue : ArrayValue
//{
//    public override object ValueAsObject => Array;
//    public override Ty Ty => ArrayTy.Int;
//    public required int[] Array { get; init; }
//    public override string Stringify() => $"[{Ty.Stringify()}]";
//    public override Value Clone() => new IntArrayValue()
//    {
//        Size = this.Size,
//        Array = this.Array,
//    };
//    public int this[int i] { get => Array[i]; set => Array[i] = value; }
//}
//internal sealed class FloatArrayValue : ArrayValue
//{
//    public override object ValueAsObject => Array;
//    public override Ty Ty => ArrayTy.Float;
//    public required float[] Array { get; init; }
//    public override string Stringify() => $"[{Ty.Stringify()}]";
//    public override Value Clone() => new FloatArrayValue()
//    {
//        Size = this.Size,
//        Array = this.Array,
//    };
//    public float this[int i] { get => Array[i]; set => Array[i] = value; }
//}
//internal sealed class BooleanArrayValue : ArrayValue
//{
//    public override object ValueAsObject => Array;
//    public override Ty Ty => ArrayTy.Boolean;
//    public required bool[] Array { get; init; }
//    public override string Stringify() => $"[{Ty.Stringify()}]";
//    public override Value Clone() => new BooleanArrayValue()
//    {
//        Size = this.Size,
//        Array = this.Array,
//    };
//    public bool this[int i] { get => Array[i]; set => Array[i] = value; }
//}
//internal sealed class TextArrayValue : ArrayValue
//{
//    public override object ValueAsObject => Array;
//    public override Ty Ty => ArrayTy.Text;
//    public required string[] Array { get; init; }
//    public override string Stringify() => $"[{Ty.Stringify()}]";
//    public override Value Clone() => new TextArrayValue()
//    {
//        Size = this.Size,
//        Array = this.Array,
//    };
//    public string this[int i] { get => Array[i]; set => Array[i] = value; }
//}
//internal sealed class AnyArrayValue : ArrayValue
//{
//    public override object ValueAsObject => Array;
//    public override Ty Ty => ArrayTy.Any;
//    public required object[] Array { get; init; }
//    public override string Stringify() => $"[{Ty.Stringify()}]";
//    public override Value Clone() => new AnyArrayValue()
//    {
//        Size = this.Size,
//        Array = this.Array,
//    };
//    public object this[int i] { get => Array[i]; set => Array[i] = value; }
//}