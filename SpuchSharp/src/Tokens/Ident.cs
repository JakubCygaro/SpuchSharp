using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;
public sealed class Ident : Token, IEquatable<Ident>, ICloneable<Ident>
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
    public override bool Equals(object? obj)
    {
        if (obj is Ident)
            return Equals((Ident)obj);
        else if (obj is string s)
            return this == s;
        else
            return base.Equals(obj);
    }
    public Ident Clone()
    {
        return new Ident()
        {
            Value = (string)this.Value.Clone(),
        };
    }
    public bool Equals(Ident? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }
    public static bool operator ==(Ident lhs, string rhs)
    {
        return lhs.Value == rhs;
    }
    public static bool operator !=(Ident lhs, string rhs) => !(lhs == rhs);
}
public static class IdentExt
{
    public static Ident AsIdent(this string s)
    {
        return new Ident
        {
            Value = s,
        };
    }
}
