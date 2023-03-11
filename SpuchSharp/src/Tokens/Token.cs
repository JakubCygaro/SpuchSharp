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
        if (text.All(c => char.IsAsciiLetter(c) || char.IsAsciiDigit(c)) 
            && char.IsAsciiLetter(text.First()))
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
