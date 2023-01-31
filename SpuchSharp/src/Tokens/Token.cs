using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

public readonly struct Location
{
    public override string ToString() => $"({Line}:{Column})";
    public required int Line { get; init; }
    public required int Column { get; init; }
}
public abstract class Token
{
    public Location? Location { get; set; }
    public abstract string Stringify();
}

public sealed class Ident : Token, IEquatable<Ident>
{
    public required string Value { get; set; }
    public override string Stringify() => Value;
    public static Ident From(string text)
    {
        if (text.All(c => char.IsAsciiLetter(c)) && !char.IsUpper(text.First()))
        {
            return new Ident() { Value = text };
        }
        throw new Lexing.LexerException($"Failed to tokenize {text} as Ident");
    }
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
    public bool Equals(Ident? other)
    {
        if(other is null) return false;
        return Value == other.Value;
    }
}
internal abstract class Value : Token
{
    public abstract Ty Ty { get; }
    public abstract object ValueAsObject { get; }
    //public object Val { get; }
    public static Value From(Ty type, string literal)
    {
        return type switch
        {
            IntTy => new IntValue { Value = int.Parse(literal) },
            TextTy => new TextValue { Value = literal },
            BooleanTy => new BooleanValue { Value = bool.Parse(literal) },
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    //public Value(Ty type, object val) => (Ty , Val) = (type, val);
    //public override string Stringify() => $"{Ty.Ident.Value} {Val}";

    static Value _void = new VoidValue();
    public static Value Void => _void;

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
            _ => throw new Interpreting.InterpreterException("Types cannot be added!")
        };
    }
    public static Value Sub(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new IntValue { Value = iv.Value - ((IntValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types cannot be subtracted!")
        };
    }
    public static Value Mul(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new IntValue { Value = iv.Value * ((IntValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types cannot be multiplied!")
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
            TextValue tv => new BooleanValue { Value = tv.Value == ((TextValue)right).Value},
            _ => throw new Interpreting.InterpreterException("Types not boolean!")

        };
    }
    public static BooleanValue InEq(Value left, Value right)
    {
        var val = Eq(left, right);
        val.Value &= false;
        return val;
    }
    public static BooleanValue GreaterThan(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new BooleanValue { Value = iv.Value > ((IntValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static BooleanValue LessThan(Value left, Value right)
    {
        var val = GreaterThan(left, right);
        val.Value &= false;
        return val;
    }

    public static BooleanValue GreaterOrEqualTo(Value left, Value right)
    {
        TypeCheck(left, right);
        return left switch
        {
            IntValue iv => new BooleanValue { Value = iv.Value >= ((IntValue)right).Value },
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static BooleanValue LessOrEqualTo(Value left, Value right)
    {
        var val = GreaterOrEqualTo(left, right);
        val.Value &= false;
        return val;
    }

    public static void TypeCheck(Value left, Value right)
    {
        if (!left.Ty.Equals(right.Ty))
            throw new Interpreting.InterpreterException(
                $"Type mismatch! {left.Ty.Stringify()} {right.Ty.Stringify()}");
    }
}
internal class TextValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Text;
    public required string Value { get; init; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
}
internal class IntValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Int;
    public required int Value { get; init; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
}
internal class BooleanValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Boolean;
    public required bool Value { get; set; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";

    public static implicit operator bool(BooleanValue value) => value.Value;
}
internal class VoidValue : Value
{
    public override object ValueAsObject => null!;
    public override Ty Ty => Ty.Void;
    public override string Stringify() => $"{Ty.Stringify()}";
}
internal class AnyValue : Value
{
    public override object ValueAsObject => Value;
    public override Ty Ty => Ty.Any;
    public required object Value { get; init; }
    public override string Stringify() => $"{Ty.Stringify()} {Value}";
}
internal abstract class Ty : Token, IEquatable<Ty>
{
    public static TextTy Text = new TextTy();
    public static IntTy Int = new IntTy();
    public static BooleanTy Boolean = new BooleanTy();
    public static VoidTy Void = new VoidTy();
    public static AnyTy Any = new AnyTy();
    public abstract Ident Ident { get; }
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
        throw new Lexing.LexerException("Failed to parse to Ty");
    }
    public static Ty FromCSharpType(Type type)
    {
        if (type == typeof(string))
            return Ty.Text;
        else if(type == typeof(int))
            return Ty.Int;
        else if (type == typeof(bool))
            return Ty.Boolean;
        else if (type == typeof(void))
            return Ty.Void;
        else if (type == typeof(object))
            return Ty.Any;
        throw new InvalidCastException(
            $"Could not translate external function type {type} to any internal type");
    }
    public static Ty From(string lit) => lit switch
    {
        "int" => Ty.Int,
        "text" => Ty.Text,
        "bool" => Ty.Boolean,
        "void" => Ty.Void,
        _ => throw new Lexing.LexerException("Failed to parse to Ty"),
    };
    public override string Stringify() => Ident.Value;

    public bool Equals(Ty? other)
    {
        if (this is AnyTy)
            return true;
        if (other is null)
        {
            return false;
        }
        return this.Ident.Equals(other.Ident);
    }

}

internal sealed class TextTy : Ty
{
    static Ident _ident = new Ident() { Value = "text" };
    public override Ident Ident => _ident;
}
internal sealed class IntTy : Ty
{
    static Ident _ident = new Ident() { Value = "int" };
    public override Ident Ident => _ident;
}
internal sealed class BooleanTy : Ty
{
    static Ident _ident = new Ident() { Value = "bool" };
    public override Ident Ident => _ident;
}
internal sealed class VoidTy : Ty
{
    static Ident _ident = new Ident() { Value = "void" };
    public override Ident Ident => _ident;
}
internal sealed class AnyTy : Ty
{
    static Ident _ident = new Ident() { Value = "any" };
    public override Ident Ident => _ident;
}



