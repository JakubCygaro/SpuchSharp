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
internal sealed class Value : Token
{
    public Ty Ty { get; }
    public object Val { get; }
    public Value(Ty type, string literal)
    {
        Ty = type;
        Val = Ty switch 
        {
            IntTy => int.Parse(literal),
            TextTy => literal,
            BooleanTy => bool.Parse(literal),
            _ => throw new System.Diagnostics.UnreachableException(),
        };
    }
    public Value(Ty type, object val) => (Ty , Val) = (type, val);
    public override string Stringify() => $"{Ty.Ident.Value} {Val}";
    public static Value Add(Value left, Value right)
    {
        TypeCheck(left, right);
        return left.Ty switch
        {
            IntTy => new Value(left.Ty,(int)left.Val + (int)right.Val),
            //TextTy => new Value { Ty = left.Ty, Val = (string)left.Val + (string)right.Val },
            _ => throw new Interpreting.InterpreterException("Types cannot be added!")
        };
    }
    public static Value Sub(Value left, Value right)
    {
        TypeCheck(left, right);
        return left.Ty switch
        {
            IntTy => new Value(left.Ty, (int)left.Val - (int)right.Val),
            _ => throw new Interpreting.InterpreterException("Types cannot be subtracted!")
        };
    }
    public static Value Mul(Value left, Value right)
    {
        TypeCheck(left, right);
        return left.Ty switch
        {
            IntTy => new Value(left.Ty,(int)left.Val * (int)right.Val),
            _ => throw new Interpreting.InterpreterException("Types cannot be multiplied!")
        };
    }
    public static Value And(Value left, Value right)
    {
        TypeCheck(left, right);
        return left.Ty switch
        {
            BooleanTy => new Value(left.Ty, (bool)left.Val && (bool)right.Val),
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static Value Or(Value left, Value right)
    {
        TypeCheck(left, right);
        return left.Ty switch
        {
            BooleanTy => new Value(left.Ty, (bool)left.Val || (bool)right.Val ),
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static Value Eq(Value left, Value right)
    {
        TypeCheck(left, right);
        return left.Ty switch
        {
            BooleanTy => new Value(left.Ty, (bool)left.Val == (bool)right.Val),
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static Value InEq(Value left, Value right)
    {
        TypeCheck(left, right);
        return left.Ty switch
        {
            BooleanTy => new Value(left.Ty, (bool)left.Val != (bool)right.Val ),
            _ => throw new Interpreting.InterpreterException("Types not boolean!")
        };
    }
    public static void TypeCheck(Value left, Value right)
    {
        if (!left.Ty.Equals(right.Ty))
            throw new Interpreting.InterpreterException(
                $"Type mismatch! {left.Ty.Stringify()} {right.Ty.Stringify()}");
    }
}
internal abstract class Ty : Token, IEquatable<Ty>
{
    public abstract Ident Ident { get; }
    public static Ty FromValue(string lit)
    {
        if (lit.ToCharArray().All(char.IsDigit))
        {
            return new IntTy();
        }
        if (lit.StartsWith('"') && lit.EndsWith('"')) 
        {
            return new TextTy();
        }
        if (lit == "false" || lit == "true")
        {
            return new BooleanTy();
        }
        throw new Lexing.LexerException("Failed to parse to Ty");
    }
    public static Ty From(string lit) => lit switch
    {
        "int" => new IntTy(),
        "text" => new TextTy(),
        "bool" => new BooleanTy(),
        _ => throw new Lexing.LexerException("Failed to parse to Ty"),
    };
    public override string Stringify() => Ident.Value;

    public bool Equals(Ty? other)
    {
        if (other is null)
        {
            return false;
        }
        return this.Ident.Equals(other.Ident);
    }
}

internal sealed class TextTy : Ty
{
    static Ident _ident => new Ident() { Value = "text" };

    public override Ident Ident => _ident;
}
internal sealed class IntTy : Ty
{
    static Ident _ident => new Ident() { Value = "int" };

    public override Ident Ident => _ident;
}
internal sealed class BooleanTy : Ty
{
    static Ident _ident => new Ident() { Value = "bool" };
    public override Ident Ident => _ident;
}



