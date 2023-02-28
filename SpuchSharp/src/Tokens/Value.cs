using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal abstract class Value : Token
{
    public abstract Value Clone();
    public abstract Ty Ty { get; }
    public abstract object ValueAsObject { get; }
    //public object Val { get; }
    public static Value From(Ty type, string literal)
    {
        return type switch
        {
            //IntTy => new IntValue { Value = int.Parse(literal) },
            TextTy => new TextValue { Value = literal },
            BooleanTy => new BooleanValue { Value = bool.Parse(literal) },
            //FloatTy => new FloatValue { Value = float.Parse(literal, 
             //                       System.Globalization.CultureInfo.InvariantCulture)},
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    //public static Value From(string literal)
    //{
    //    return literal
    //}
    //public Value(Ty type, object val) => (Ty , Val) = (type, val);
    //public override string Stringify() => $"{Ty.Ident.Value} {Val}";

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
            string s => new TextValue { Value = s },
            int i => new IntValue { Value = i },
            bool b => new BooleanValue { Value = b },
            null => Value.Void,
            _ => throw new InvalidCastException($"Could not transalte c# type {obj} into Spuch# type")
        };
    }
    public static Value Add(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new IntValue { Value = iv.Value + ((IntValue)right).Value },
            TextValue sv => new TextValue { Value = sv.Value + ((TextValue)right).Value },
            FloatValue fv => new FloatValue { Value = fv.Value + ((FloatValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types cannot be added!")
        };
    }
    public static Value Sub(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new IntValue { Value = iv.Value - ((IntValue)right).Value },
            FloatValue fv => new FloatValue { Value = fv.Value - ((FloatValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types cannot be subtracted!")
        };
    }
    public static Value Mul(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new IntValue { Value = iv.Value * ((IntValue)right).Value },
            FloatValue fv => new FloatValue { Value = fv.Value * ((FloatValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types cannot be multiplied!")
        };
    }
    public static Value Div(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new IntValue { Value = iv.Value / ((IntValue)right).Value },
            FloatValue fv => new FloatValue { Value = fv.Value / ((FloatValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types cannot be divided!")
        };
    }
    public static Value And(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            BooleanValue bv => new BooleanValue { Value = bv.Value && ((BooleanValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static Value Or(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            BooleanValue bv => new BooleanValue { Value = bv.Value || ((BooleanValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static BooleanValue Eq(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            BooleanValue bv => new BooleanValue { Value = bv.Value == ((BooleanValue)right).Value },
            IntValue iv => new BooleanValue { Value = iv.Value == ((IntValue)right).Value },
            TextValue tv => new BooleanValue { Value = tv.Value == ((TextValue)right).Value },
            FloatValue fv => new BooleanValue { Value = fv.Value == ((FloatValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types not boolean!")

        };
    }
    public static BooleanValue InEq(Value left, Value right)
    {
        var val = Eq(left, right);
        val.Value = !val.Value;
        return val;
    }
    public static BooleanValue GreaterThan(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new BooleanValue { Value = iv.Value > ((IntValue)right).Value },
            FloatValue fv => new BooleanValue { Value = fv.Value > ((FloatValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static BooleanValue LessThan(Value left, Value right)
    {
        var val = GreaterThan(left, right);
        val.Value = !val.Value;
        return val;
    }

    public static BooleanValue GreaterOrEqualTo(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new BooleanValue { Value = iv.Value >= ((IntValue)right).Value },
            FloatValue fv => new BooleanValue { Value = fv.Value >= ((FloatValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static BooleanValue LessOrEqualTo(Value left, Value right)
    {
        var val = GreaterOrEqualTo(left, right);
        val.Value = !val.Value;
        return val;
    }

    public static void TypeCheck(Value left, Value right)
    {
        if (!left.Ty.Equals(right.Ty))
            throw new Interpreting.InterpreterException(
                $"Type mismatch! {left.Ty.Stringify()} and {right.Ty.Stringify()}", left);
    }
    public static Value Default(Ty ty, int arraySize = 0)
    {
        return ty switch
        {
            IntTy => new IntValue { Value = 0 },
            FloatTy => new FloatValue { Value = 0f },
            TextTy => new TextValue { Value = string.Empty },
            BooleanTy => new BooleanValue { Value = false },
            AnyTy => new AnyValue { Value = new object() },
            ArrayTy arrayTy => new ArrayValue(arrayTy.OfType, arraySize),
            _ => throw new Interpreting.InterpreterException(
                $"The runtime was unable to determine the default value for type: {ty.Ident}"),
        };
    }
}
internal sealed class TextValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Text;
    public required string Value { get; init; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
    public override Value Clone() =>
        new TextValue
        {
            Value = this.Value
        };
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

    public static implicit operator int(IntValue intV) => intV.Value;
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

    public static implicit operator bool(BooleanValue value) => value.Value;
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


