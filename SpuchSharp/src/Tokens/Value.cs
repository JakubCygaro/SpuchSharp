using SpuchSharp.Interpreting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SpuchSharp.Tokens;


internal abstract class Value : Token
{
    public abstract Value Clone();
    public abstract Ty Ty { get; }
    public abstract object ValueAsObject { get; }
    public static Value From(Ty type, string literal)
    {
        return type switch
        {
            TextTy => new TextValue(literal),
            BooleanTy => new BooleanValue { Value = bool.Parse(literal) },
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }


    public static Value Void = new VoidValue();
    public static Value Nothing = new NothingValue();

    /// <summary>
    /// Creates a spuch# <c>Value</c> from a c# object
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <remarks>
    /// Throws <c>InvalidCastException</c> if there is no corresponding spuch# Type for provided c# obcject
    /// </remarks>
    /// <exception cref="InvalidCastException"></exception>
    public static Value FromObject(object? obj)
    {
        return obj switch
        {
            string s => new TextValue(s),
            short h => new ShortValue { Value = h },
            int i => new IntValue { Value = i },
            long l => new LongValue { Value = l },
            float f => new FloatValue { Value = f },
            double d => new DoubleValue { Value = d },
            bool b => new BooleanValue { Value = b },
            null => Value.Void,
            _ => throw new InvalidCastException($"Could not transalte c# type {obj} into Spuch# type")
        };
    }
    public virtual Value Add(Value other) =>
        throw InterpreterException.InvalidOperation("addition", this, other);
    public virtual Value Sub(Value other) =>
        throw InterpreterException.InvalidOperation("subtraction", this, other);
    public virtual Value Mul(Value other) =>
        throw InterpreterException.InvalidOperation("multiplication", this, other);
    public virtual Value Div(Value other) =>
        throw InterpreterException.InvalidOperation("division", this, other);
    public virtual Value Modulo(Value other) =>
        throw InterpreterException.InvalidOperation("modulo", this, other);
    public virtual BooleanValue And(Value other) =>
        throw InterpreterException.InvalidOperation("logical and", this, other);
    public virtual BooleanValue Or(Value other) =>
        throw InterpreterException.InvalidOperation("logical or", this, other);
    public virtual BooleanValue Eq(Value other) =>
        throw InterpreterException.InvalidOperation("logical equality", this, other);
    public virtual BooleanValue InEq(Value other)
    {
        var v = Eq(other);
        v.Value = !v.Value;
        return v;
    }
    public virtual BooleanValue GreaterThan(Value other) =>
        throw InterpreterException.InvalidOperation("logical greater than", this, other);
    public virtual BooleanValue LessThan(Value other)
    {
        var val = GreaterThan(other);
        val.Value = !val.Value;
        return val;
    }
    public virtual BooleanValue GreaterOrEqualTo(Value other)
    {
        return GreaterThan(other) || Eq(other);
    }
    public virtual BooleanValue LessOrEqualTo(Value other)
    {
        return LessThan(other) || Eq(other);
    }

    public static void TypeCheck(Value left, Value right)
    {
        if (!left.Ty.Equals(right.Ty))
            throw new Interpreting.InterpreterException(
                $"Type mismatch! {left.Ty.Stringify()} and {right.Ty.Stringify()}", left);
    }


    //REWORK THIS, MOVE IT INTO Ty 
    public static Value Default(Ty ty, int arraySize = 0)
    {
        return ty switch
        {
            ShortTy => new ShortValue { Value = 0 },
            IntTy => new IntValue { Value = 0 },
            LongTy => new LongValue { Value = 0 },
            FloatTy => new FloatValue { Value = 0f },
            DoubleTy => new DoubleValue { Value = 0f },
            TextTy => new TextValue(string.Empty),
            BooleanTy => new BooleanValue { Value = false },
            AnyTy => new AnyValue { Value = new object() },
            ArrayTy arrayTy => new ArrayValue(arrayTy.OfType, arraySize),
            _ => throw new Interpreting.InterpreterException(
                $"The runtime was unable to determine the default value for type: {ty.Ident}"),
        };
    }
}
internal sealed class TextValue : Value, IAsArray
{
    private ArrayValue? _textAsArray;
    ArrayValue IAsArray.AsArray 
    { 
        get 
        {
            if(_textAsArray is not null)
                return _textAsArray;

            Value[] values = new Value[_value.Length];
            var valueAsSpan = _value.AsSpan();

            for (int i = 0; i < _value.Length; i++)
            {
                var chuj = new string(valueAsSpan.Slice(i, 1));
                values[i] = new TextValue(chuj);
            }
            _textAsArray = new ArrayValue(Ty.Text, values);
            return _textAsArray;
        }
    }

    public TextValue(string value)
    {
        _value = value;
    }
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Text;
    private string _value;
    public string Value 
    {
        get => _value;
    }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new TextValue(this._value);
    public override Value Add(Value other)
    {
        if (other is not TextValue text)
            throw InterpreterException.InvalidOperation("addition", this, other);
        return new TextValue(this.Value + text.Value);
    }
    public override BooleanValue Eq(Value other)
    {
        if (other is not TextValue text)
            return base.Eq(other);
        return new BooleanValue { Value = Value == text.Value };
    }
}
internal sealed class ShortValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Short;
    public required short Value { get; set; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new ShortValue
        {
            Value = this.Value
        };
    public override Value Add(Value other) =>
        other switch
        {
            ShortValue h => new ShortValue { Value = (short)(Value + h.Value) },
            IntValue i => new IntValue { Value = Value + i.Value },
            LongValue l => new LongValue { Value = Value + l.Value },
            FloatValue f => new FloatValue { Value = Value + f.Value },
            DoubleValue f => new DoubleValue { Value = Value + f.Value },
            _ => base.Add(other)
        };
    public override Value Sub(Value other) =>
        other switch
        {
            ShortValue h => new ShortValue { Value = (short)(Value - h.Value) },
            IntValue i => new IntValue { Value = Value - i.Value },
            LongValue l => new LongValue { Value = Value - l.Value },
            FloatValue f => new FloatValue { Value = Value - f.Value },
            DoubleValue f => new DoubleValue { Value = Value - f.Value },
            _ => base.Sub(other)
        };
    public override Value Mul(Value other) =>
        other switch
        {
            ShortValue h => new ShortValue { Value = (short)(Value * h.Value) },
            IntValue i => new IntValue { Value = Value * i.Value },
            LongValue l => new LongValue { Value = Value * l.Value },
            FloatValue f => new FloatValue { Value = Value * f.Value },
            DoubleValue f => new DoubleValue { Value = Value * f.Value },
            _ => base.Mul(other)
        };
    public override Value Div(Value other) =>
        other switch
        {
            ShortValue h => new ShortValue { Value = (short)(Value / h.Value) },
            IntValue i => new IntValue { Value = Value / i.Value },
            LongValue l => new LongValue { Value = Value / l.Value },
            FloatValue f => new FloatValue { Value = Value / f.Value },
            DoubleValue f => new DoubleValue { Value = Value / f.Value },
            _ => base.Div(other)
        };
    public override Value Modulo(Value other) =>
        other switch
        {
            ShortValue h => new ShortValue { Value = (short)(Value % h.Value) },
            _ => base.Modulo(other)
        };
    public override BooleanValue Eq(Value other) =>
        other switch
        {
            ShortValue h => h == Value,
            IntValue i => i == Value,
            LongValue l => l == Value,
            FloatValue f => f.Value == Value,
            DoubleValue d => d.Value == Value,
            _ => base.Eq(other)
        };
    public override BooleanValue GreaterThan(Value other) =>
        other switch
        {
            ShortValue h => Value > h,
            IntValue i => Value > i,
            LongValue l => Value > l,
            FloatValue f => Value > f.Value,
            DoubleValue d => Value > d.Value,
            _ => base.GreaterThan(other)
        };
    public override BooleanValue LessThan(Value other) =>
        other switch
        {
            ShortValue h => Value < h,
            IntValue i => Value < i,
            LongValue l => Value < l,
            FloatValue f => Value < f.Value,
            DoubleValue d => Value < d.Value,
            _ => base.LessThan(other)
        };


    public static implicit operator short(ShortValue shortV) => shortV.Value;
    
}

internal sealed class IntValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Int;
    public required int Value { get; set; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new IntValue
        {
            Value = this.Value
        };
    public override Value Add(Value other) =>
        other switch
        {
            ShortValue h => new IntValue { Value = h.Value + Value },
            IntValue i => new IntValue { Value = i.Value + Value },
            LongValue l => new LongValue { Value = l.Value + Value },
            FloatValue f => new FloatValue { Value = f.Value + Value },
            DoubleValue d => new DoubleValue { Value = d.Value + Value },
            _ => base.Add(other)
        };
    public override Value Sub(Value other) =>
        other switch
        {
            ShortValue h => new IntValue { Value = Value - h.Value },
            IntValue i => new IntValue { Value = Value - i.Value },
            LongValue l => new LongValue { Value = Value - l.Value },
            FloatValue f => new FloatValue { Value = Value - f.Value },
            DoubleValue f => new DoubleValue { Value = Value - f.Value },
            _ => base.Sub(other)
        };
    public override Value Mul(Value other) =>
        other switch
        {
            ShortValue h => new IntValue { Value = Value * h.Value },
            IntValue i => new IntValue { Value = Value * i.Value },
            LongValue l => new LongValue { Value = Value * l.Value },
            FloatValue f => new FloatValue { Value = Value * f.Value },
            DoubleValue f => new DoubleValue { Value = Value * f.Value },
            _ => base.Mul(other)
        };
    public override Value Div(Value other) =>
        other switch
        {
            ShortValue h => new IntValue { Value = Value / h.Value },
            IntValue i => new IntValue { Value = Value / i.Value },
            LongValue l => new LongValue { Value = Value / l.Value },
            FloatValue f => new FloatValue { Value = Value / f.Value },
            DoubleValue f => new DoubleValue { Value = Value / f.Value },
            _ => base.Div(other)
        };
    public override Value Modulo(Value other) =>
        other switch
        {
            IntValue i => new IntValue { Value = Value % i.Value },
            _ => base.Modulo(other)
        };
    public override BooleanValue Eq(Value other) =>
        other switch
        {
            ShortValue h => h == Value,
            IntValue i => i == Value,
            LongValue l => l == Value,
            FloatValue f => f.Value == Value,
            DoubleValue d => d.Value == Value,
            _ => base.Eq(other)
        };
    public override BooleanValue GreaterThan(Value other) =>
        other switch
        {
            ShortValue h => Value > h,
            IntValue i => Value > i,
            LongValue l => Value > l,
            FloatValue f => Value > f.Value,
            DoubleValue d => Value > d.Value,
            _ => base.GreaterThan(other)
        };
    public override BooleanValue LessThan(Value other) =>
        other switch
        {
            ShortValue h => Value < h,
            IntValue i => Value < i,
            LongValue l => Value < l,
            FloatValue f => Value < f.Value,
            DoubleValue d => Value < d.Value,
            _ => base.LessThan(other)
        };
    public static implicit operator int(IntValue intV) => intV.Value;
    
}
internal sealed class LongValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Long;
    public required long Value { get; set; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new LongValue
        {
            Value = this.Value
        };
    public override Value Add(Value other) =>
        other switch
        {
            ShortValue h => new LongValue { Value = h.Value + Value },
            IntValue i => new LongValue { Value = i.Value + Value },
            LongValue l => new LongValue { Value = l.Value + Value },
            FloatValue f => new FloatValue { Value = f.Value + Value },
            DoubleValue d => new DoubleValue { Value = d.Value + Value },
            _ => base.Add(other)
        };
    public override Value Sub(Value other) =>
        other switch
        {
            ShortValue h => new LongValue { Value = Value - h.Value },
            IntValue i => new LongValue { Value = Value - i.Value },
            LongValue l => new LongValue { Value = Value - l.Value },
            FloatValue f => new FloatValue { Value = Value - f.Value },
            DoubleValue f => new DoubleValue { Value = Value - f.Value },
            _ => base.Sub(other)
        };
    public override Value Mul(Value other) =>
        other switch
        {
            ShortValue h => new LongValue { Value = Value * h.Value },
            IntValue i => new LongValue { Value = Value * i.Value },
            LongValue l => new LongValue { Value = Value * l.Value },
            FloatValue f => new FloatValue { Value = Value * f.Value },
            DoubleValue f => new DoubleValue { Value = Value * f.Value },
            _ => base.Mul(other)
        };
    public override Value Div(Value other) =>
        other switch
        {
            ShortValue h => new LongValue { Value = Value / h.Value },
            IntValue i => new LongValue { Value = Value / i.Value },
            LongValue l => new LongValue { Value = Value / l.Value },
            FloatValue f => new FloatValue { Value = Value / f.Value },
            DoubleValue f => new DoubleValue { Value = Value / f.Value },
            _ => base.Div(other)
        };
    public override Value Modulo(Value other) =>
        other switch
        {
            LongValue i => new LongValue { Value = Value % i.Value },
            _ => base.Modulo(other)
        };

    public override BooleanValue Eq(Value other) =>
        other switch
        {
            ShortValue h => h == Value,
            IntValue i => i == Value,
            LongValue l => l == Value,
            FloatValue f => f.Value == Value,
            DoubleValue d => d.Value == Value,
            _ => base.Eq(other)
        };
    public override BooleanValue GreaterThan(Value other) =>
        other switch
        {
            ShortValue h => Value > h,
            IntValue i => Value > i,
            LongValue l => Value > l,
            FloatValue f => Value > f.Value,
            DoubleValue d => Value > d.Value,
            _ => base.GreaterThan(other)
        };
    public override BooleanValue LessThan(Value other) =>
        other switch
        {
            ShortValue h => Value < h,
            IntValue i => Value < i,
            LongValue l => Value < l,
            FloatValue f => Value < f.Value,
            DoubleValue d => Value < d.Value,
            _ => base.LessThan(other)
        };
    public static implicit operator long(LongValue longV) => longV.Value;
    
}
internal sealed class FloatValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Float;
    public required float Value { get; set; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new FloatValue
        {
            Value = this.Value
        };
    public override Value Add(Value other) =>
        other switch
        {
            ShortValue h => new FloatValue { Value = h.Value + Value },
            IntValue i => new FloatValue { Value = i.Value + Value },
            LongValue l => new FloatValue { Value = l.Value + Value },
            FloatValue f => new FloatValue { Value = f.Value + Value },
            DoubleValue d => new DoubleValue { Value = d.Value + Value },
            _ => base.Add(other)
        };
    public override Value Sub(Value other) =>
        other switch
        {
            ShortValue h => new FloatValue { Value = Value - h.Value },
            IntValue i => new FloatValue { Value = Value - i.Value },
            LongValue l => new FloatValue { Value = Value - l.Value },
            FloatValue f => new FloatValue { Value = Value - f.Value },
            DoubleValue f => new DoubleValue { Value = Value - f.Value },
            _ => base.Sub(other)
        };
    public override Value Mul(Value other) =>
        other switch
        {
            ShortValue h => new FloatValue { Value = Value * h.Value },
            IntValue i => new FloatValue { Value = Value * i.Value },
            LongValue l => new FloatValue { Value = Value * l.Value },
            FloatValue f => new FloatValue { Value = Value * f.Value },
            DoubleValue f => new DoubleValue { Value = Value * f.Value },
            _ => base.Mul(other)
        };
    public override Value Div(Value other) =>
        other switch
        {
            ShortValue h => new FloatValue { Value = Value / h.Value },
            IntValue i => new FloatValue { Value = Value / i.Value },
            LongValue l => new FloatValue { Value = Value / l.Value },
            FloatValue f => new FloatValue { Value = Value / f.Value },
            DoubleValue f => new DoubleValue { Value = Value / f.Value },
            _ => base.Div(other)
        };
    public override Value Modulo(Value other) =>
        other switch
        {
            FloatValue i => new FloatValue { Value = Value % i.Value },
            _ => base.Modulo(other)
        };
    public override BooleanValue Eq(Value other) =>
        other switch
        {
            ShortValue h => h == Value,
            IntValue i => i == Value,
            LongValue l => l == Value,
            FloatValue f => f.Value == Value,
            DoubleValue d => d.Value == Value,
            _ => base.Eq(other)
        };
    public override BooleanValue GreaterThan(Value other) =>
        other switch
        {
            ShortValue h => Value > h,
            IntValue i => Value > i,
            LongValue l => Value > l,
            FloatValue f => Value > f.Value,
            DoubleValue d => Value > d.Value,
            _ => base.GreaterThan(other)
        };
    public override BooleanValue LessThan(Value other) =>
        other switch
        {
            ShortValue h => Value < h,
            IntValue i => Value < i,
            LongValue l => Value < l,
            FloatValue f => Value < f.Value,
            DoubleValue d => Value < d.Value,
            _ => base.LessThan(other)
        };
}
internal sealed class DoubleValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Double;
    public required double Value { get; set; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new DoubleValue
        {
            Value = this.Value
        };
    public override Value Add(Value other) =>
        other switch
        {
            ShortValue h => new DoubleValue { Value = h.Value + Value },
            IntValue i => new DoubleValue { Value = i.Value + Value },
            LongValue l => new DoubleValue { Value = l.Value + Value },
            FloatValue f => new DoubleValue { Value = f.Value + Value },
            DoubleValue d => new DoubleValue { Value = d.Value + Value },
            _ => base.Add(other)
        };
    public override Value Sub(Value other) =>
        other switch
        {
            ShortValue h => new DoubleValue { Value = Value - h.Value },
            IntValue i => new DoubleValue { Value = Value - i.Value },
            LongValue l => new DoubleValue { Value = Value - l.Value },
            FloatValue f => new DoubleValue { Value = Value - f.Value },
            DoubleValue f => new DoubleValue { Value = Value - f.Value },
            _ => base.Sub(other)
        };
    public override Value Mul(Value other) =>
        other switch
        {
            ShortValue h => new DoubleValue { Value = Value * h.Value },
            IntValue i => new DoubleValue { Value = Value * i.Value },
            LongValue l => new DoubleValue { Value = Value * l.Value },
            FloatValue f => new DoubleValue { Value = Value * f.Value },
            DoubleValue f => new DoubleValue { Value = Value * f.Value },
            _ => base.Mul(other)
        };
    public override Value Div(Value other) =>
        other switch
        {
            ShortValue h => new DoubleValue { Value = Value / h.Value },
            IntValue i => new DoubleValue { Value = Value / i.Value },
            LongValue l => new DoubleValue { Value = Value / l.Value },
            FloatValue f => new DoubleValue { Value = Value / f.Value },
            DoubleValue f => new DoubleValue { Value = Value / f.Value },
            _ => base.Div(other)
        };
    public override Value Modulo(Value other) =>
        other switch
        {
            DoubleValue i => new DoubleValue { Value = Value % i.Value },
            _ => base.Modulo(other)
        };
    public override BooleanValue Eq(Value other) =>
        other switch
        {
            ShortValue h => h == Value,
            IntValue i => i == Value,
            LongValue l => l == Value,
            FloatValue f => f.Value == Value,
            DoubleValue d => d.Value == Value,
            _ => base.Eq(other)
        };
    public override BooleanValue GreaterThan(Value other) =>
        other switch
        {
            ShortValue h => Value > h,
            IntValue i => Value > i,
            LongValue l => Value > l,
            FloatValue f => Value > f.Value,
            DoubleValue d => Value > d.Value,
            _ => base.GreaterThan(other)
        };
    public override BooleanValue LessThan(Value other) =>
        other switch
        {
            ShortValue h => Value < h,
            IntValue i => Value < i,
            LongValue l => Value < l,
            FloatValue f => Value < f.Value,
            DoubleValue d => Value < d.Value,
            _ => base.LessThan(other)
        };
}
internal sealed class BooleanValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Boolean;
    public required bool Value { get; set; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new BooleanValue
        {
            Value = this.Value
        };
    public static implicit operator BooleanValue(bool b) => new BooleanValue { Value = b };
    public static implicit operator bool(BooleanValue value) => value.Value;
    public override BooleanValue And(Value other)
    {
        if (other is not BooleanValue b)
            return base.And(other);
        return new BooleanValue { Value = b.Value && Value };
    }
    public override BooleanValue Or(Value other)
    {
        if (other is not BooleanValue b)
            return base.And(other);
        return new BooleanValue { Value = b.Value || Value };
    }
    public override BooleanValue Eq(Value other)
    {
        if (other is not BooleanValue b)
            return base.Eq(other);
        return new BooleanValue { Value = Value == b.Value };
    }
}
internal sealed class VoidValue : Value
{
    public override object ValueAsObject => null!;
    public override Ty Ty => Ty.Void;
    public override string Stringify() => $"{Ty.Stringify()}";
    public override Value Clone() => Value.Void;
}
internal sealed class AnyValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Any;
    public required object Value { get; init; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new AnyValue
        {
            Value = this.Value
        };
}
internal sealed class NothingValue : Value
{
    public override object ValueAsObject => null!;
    public override Ty Ty => Ty.Nothing;
    public override string Stringify() => $"{Ty.Stringify()} <NOVALUE>";
    public override Value Clone() => Value.Nothing;
}


