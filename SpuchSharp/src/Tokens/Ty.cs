using SpuchSharp.Interpreting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SpuchSharp.Tokens;

internal abstract class Ty : Token, IEquatable<Ty>
{
    public static TextTy Text = new TextTy();
    public static ShortTy Short = new ShortTy();
    public static IntTy Int = new IntTy();
    public static LongTy Long = new LongTy();
    public static FloatTy Float = new FloatTy();
    public static DoubleTy Double = new DoubleTy();
    public static BooleanTy Boolean = new BooleanTy();
    public static VoidTy Void = new VoidTy();
    public static AnyTy Any = new AnyTy();
    public static NothingTy Nothing = new NothingTy();
    public abstract Ident Ident { get; }

    public virtual Value Cast(Value v)
    {
        throw InterpreterException.InvalidCast(this.Ident, v);
    }
    public virtual Value? SafeCast(Value v)
    {
        try { return Cast(v); }
        catch { return null; }
    }
    public abstract Value DefaultValue();

    public static Ty FromValue(string lit)
    {
        if (lit.ToCharArray().All(char.IsDigit))
        {
            return Ty.Int;
        }
        if (lit.StartsWith('"') && lit.EndsWith('"'))
        {
            return Ty.Text;
        }
        if (lit == "false" || lit == "true")
        {
            return Ty.Boolean;
        }
        if (float.TryParse(lit, out float _))
            return Ty.Float;
        throw new Lexing.LexerException($"Failed to parse to Ty -> {lit}");
    }
    public static Ty FromCSharpType(Type type)
    {
        if (type == typeof(string))
            return Ty.Text;
        else if (type == typeof(string[]))
            return ArrayTy.Text;

        else if (type == typeof(short))
            return Ty.Short;
        else if (type == typeof(short[]))
            return ArrayTy.Short;

        else if (type == typeof(int))
            return Ty.Int;
        else if (type == typeof(int[]))
            return ArrayTy.Int;

        else if (type == typeof(long))
            return Ty.Long;
        else if (type == typeof(long[]))
            return ArrayTy.Long;

        else if (type == typeof(float))
            return Ty.Float;
        else if (type == typeof(float[]))
            return ArrayTy.Float;

        else if (type == typeof(double))
            return Ty.Double;
        else if (type == typeof(double[]))
            return ArrayTy.Double;

        else if (type == typeof(bool))
            return Ty.Boolean;
        else if (type == typeof(bool[]))
            return ArrayTy.Boolean;

        else if (type == typeof(void))
            return Ty.Void;

        else if (type == typeof(object))
            return Ty.Any;
        else if (type == typeof(object[]))
            return ArrayTy.Any;

        throw new InvalidCastException(
            $"Could not translate external function type {type} to any internal type");
    }
    public static Ty? From(string lit) => lit switch
    {
        "short" => Ty.Short,
        "int" => Ty.Int,
        "long" => Ty.Long,
        "text" => Ty.Text,
        "bool" => Ty.Boolean,
        "void" => Ty.Void,
        "float" => Ty.Float,
        "double" => Ty.Double,
        _ => null,
    };
    public static Ty? From(ReadOnlySpan<char> lit) => lit switch
    {
        "short" => Ty.Short,
        "int" => Ty.Int,
        "long" => Ty.Long,
        "text" => Ty.Text,
        "bool" => Ty.Boolean,
        "void" => Ty.Void,
        "float" => Ty.Float,
        "double" => Ty.Double,
        _ => null,
    };
    public override string Stringify() => Ident.Value;
    public override bool Equals(object? obj)
    {
        if (obj is Ty other)
            return this == other;
        else
            return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    public bool Equals(Ty? other)
    {
        if (other is null)
        {
            return false;
        }
        if (this is AnyTy)
            return true;
        if (this is ArrayTy arr)
            return ArrayTyEquals(arr, other);
        return this.Ident.Equals(other.Ident);
    }
    public static bool operator == (Ty a, Ty b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(Ty a, Ty b) => !(a == b);
    private bool ArrayTyEquals(ArrayTy arr, Ty other)
    {
        if (other is not ArrayTy otherArray)
            return false;
        if (otherArray.OfType.Equals(ArrayTy.Any))
            return true;
        return arr.OfType.Equals(otherArray.OfType);
    }
}

internal sealed class TextTy : Ty
{
    static Ident _ident = new Ident() { Value = "text" };
    public override Ident Ident => _ident;
    public override Value DefaultValue()
    {
        return new TextValue("");
    }
    public override Value Cast(Value v)
    {
        if (v is TextValue)
            return v;
        return base.Cast(v);
    }
}
internal sealed class IntTy : Ty
{
    static Ident _ident = new Ident() { Value = "int" };
    public override Ident Ident => _ident;
    public override IntValue Cast(Value v)
    {
        return v switch
        {
            LongValue l => new IntValue { Value = (int)l.Value },
            ShortValue s => new IntValue { Value = (int)s.Value },
            FloatValue f => new IntValue { Value = (int)f.Value },
            DoubleValue d => new IntValue { Value = (int)d.Value },
            IntValue i => i,
            _ => throw InterpreterException.InvalidCast(_ident, v),
        };
    }
    public override Value DefaultValue()
    {
        return new IntValue { Value = 0 };
    }
}

internal sealed class BooleanTy : Ty
{
    static Ident _ident = new Ident() { Value = "bool" };
    public override Ident Ident => _ident;
    public override Value DefaultValue()
    {
        return new BooleanValue { Value = false };
    }
}
internal sealed class VoidTy : Ty
{
    static Ident _ident = new Ident() { Value = "void" };
    public override Ident Ident => _ident;
    public override Value DefaultValue()
    {
        return Value.Void;
    }
}
internal sealed class AnyTy : Ty
{
    static Ident _ident = new Ident() { Value = "any" };
    public override Ident Ident => _ident;
    public override Value DefaultValue()
    {
        return new AnyValue { Value = null! };
    }
}
internal sealed class NothingTy : Ty
{
    static Ident _ident = new Ident() { Value = "nothing_type" };
    public override Ident Ident => _ident;
    public override Value DefaultValue()
    {
        return Value.Nothing;
    }
}
internal sealed class FloatTy : Ty
{
    static Ident _ident = new Ident() { Value = "float" };
    public override Ident Ident => _ident;
    public override FloatValue Cast(Value v)
    {
        return v switch
        {
            ShortValue s => new FloatValue { Value = (float)s.Value },
            IntValue i => new FloatValue { Value = (float)i.Value },
            LongValue l => new FloatValue { Value = (float)l.Value },
            DoubleValue d => new FloatValue { Value = (float)d.Value },
            FloatValue f => f,
            _ => throw InterpreterException.InvalidCast(Ident, v),
        };
    }
    public override Value DefaultValue()
    {
        return new FloatValue { Value = 0f };
    }
}
internal sealed class DoubleTy : Ty
{
    static Ident _ident = new Ident() { Value = "double" };
    public override Ident Ident => _ident;
    public override DoubleValue Cast(Value v)
    {
        return v switch
        {
            ShortValue s => new DoubleValue { Value = s.Value },
            IntValue i => new DoubleValue { Value = i.Value },
            LongValue l => new DoubleValue { Value = l.Value },
            FloatValue f => new DoubleValue { Value = f.Value },
            DoubleValue d => d,
            _ => throw InterpreterException.InvalidCast(Ident, v),
        };
    }
    public override Value DefaultValue()
    {
        return new DoubleValue { Value = 0f };
    }
}
internal sealed class ShortTy : Ty
{
    static Ident _ident = new Ident() { Value = "short" };
    public override Ident Ident => _ident;
    public override ShortValue Cast(Value v)
    {
        return v switch
        {
            IntValue i => new ShortValue { Value = (short)i.Value },
            LongValue l => new ShortValue { Value = (short)l.Value },
            FloatValue f => new ShortValue { Value = (short)f.Value },
            DoubleValue d => new ShortValue { Value = (short)d.Value },
            ShortValue s => s,
            _ => throw InterpreterException.InvalidCast(Ident, v),
        };
    }
    public override Value DefaultValue()
    {
        return new ShortValue { Value = 0 };
    }
}
internal sealed class LongTy : Ty
{
    static Ident _ident = new Ident() { Value = "long" };
    public override Ident Ident => _ident;
    
    public override LongValue Cast(Value v)
    {
        return v switch
        {
            ShortValue s => new LongValue { Value = (long)s.Value },
            IntValue i => new LongValue { Value = (long)i.Value },
            FloatValue f => new LongValue { Value = (long)f.Value },
            DoubleValue d => new LongValue { Value = (long)d.Value },
            LongValue l => l,
            _ => throw InterpreterException.InvalidCast(Ident, v),
        };
    }
    public override Value DefaultValue()
    {
        return new LongValue { Value = 0 };
    }
}
internal sealed class ArrayTy : Ty
{
    private Ty _type;
    public Ty OfType => _type;

    private Ident _ident;
    public override Ident Ident => _ident;
    public static ArrayTy ArrayOf(Ty type)
    {
        return type switch
        {
            ShortTy => Short,
            IntTy => Int,
            LongTy => Long,
            FloatTy => Float,
            DoubleTy => Double,
            BooleanTy => Boolean,
            TextTy => Text,
            AnyTy => Any,
            _ =>  new ArrayTy(type)
        };
    }
    private ArrayTy(Ty type)
    {
        _type = type;
        _ident = new Ident() { Value = $"{_type.Ident.Value} []" };
    }
    /// <summary>
    /// This will return an <c>ArrayValue</c> of size 0 !
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override Value DefaultValue()
    {
        return DefaultValue(0);
    }
    public Value DefaultValue(int size)
    {
        return new ArrayValue(_type, size);
    }
    public override Value Cast(Value v)
    {
        if (v is ArrayValue arrayVal)
        {
            if (_type == arrayVal.ValueTy)
                return arrayVal;
            else
                return base.Cast(v);
        }
        else
            return base.Cast(v);
    }
    public override string Stringify()
    {
        StringBuilder sb = new();
        sb.Append($"{_type.Stringify()}");
        var innerType = _type;
        while(innerType is ArrayTy innerArray)
        {
            sb.Append("[]");
            innerType = innerArray.OfType;
        }
        return sb.ToString();
    }

    new public static ArrayTy Short = new ArrayTy(Ty.Short);
    new public static ArrayTy Long = new ArrayTy(Ty.Long);
    new public static ArrayTy Int = new ArrayTy(Ty.Int);
    new public static ArrayTy Float = new ArrayTy(Ty.Float);
    new public static ArrayTy Double = new ArrayTy(Ty.Double);
    new public static ArrayTy Boolean = new ArrayTy(Ty.Boolean);
    new public static ArrayTy Text = new ArrayTy(Ty.Text);
    new public static ArrayTy Any = new ArrayTy(Ty.Any);
}