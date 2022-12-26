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

public sealed class Ident : Token
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
}
internal sealed class Value : Token
{
    public required Ty Ty { get; set; }
    public required object Val { get; set; }
    public override string Stringify() => $"{Ty.Ident.Value} {Val}";
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
        if (other is null) return false;
        return this.Ident == other.Ident;
    }
}

internal sealed class TextTy : Ty
{
    public override Ident Ident => new Ident() { Value = "text" };
}
internal sealed class IntTy : Ty
{
    public override Ident Ident => new Ident() { Value = "int" };
}
internal sealed class BooleanTy : Ty
{
    public override Ident Ident => new Ident() { Value = "bool" };
}



