using System;
using System.Collections.Generic;
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
    public Value[] Values { get; }
    public Value this[int index] { get => Values[index]; set=> Values[index] = value; }

    public ArrayValue(Ty type, int size)
    {
        ValueTy = type;
        _arrayTy = ArrayTy.ArrayOf(type);
        Size = size;
        Values = new Value[Size];
        for (int i = 0; i < size; i++)
            Values[i] = Value.Default(ValueTy);
    }
    public override string Stringify()
    {
        return $"{Ty.Stringify()}";
    }
    //public override object ValueAsObject => Values switch 
    //{
    //    IntValue[] i => i.Select(i => i.ValueAsObject ).ToArray(),
    //    FloatValue[] f => f.Select(f => f.ValueAsObject).ToArray(),
    //    TextValue[] t => t.Select(t => t.ValueAsObject).ToArray(),
    //    BooleanValue[] b => b.Select(b => b.ValueAsObject).ToArray(),
    //    AnyValue[] a => a.Select(a => a.ValueAsObject).ToArray(),
    //    _ => throw new Interpreting.InterpreterException("ValueArray object translation failed")
    //};
    public override object ValueAsObject => Values.Select(x => x.ValueAsObject).ToArray();
    public override Value Clone()
    {
        return new ArrayValue(ValueTy, Size);
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