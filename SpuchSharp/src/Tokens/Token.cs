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
public sealed class Value : Token 
{
    public required Ty Ty { get; set; }
    public required object Val { get; set; }
    public override string Stringify() => $"{Ty.Ident.Value} {Val}";
}
public sealed class Ty : Token 
{
    public required Ident Ident { get; set; }
    public static Ty FromValue(string lit)
    {
        if (lit.ToCharArray().All(char.IsDigit))
        {
            return new Ty()
            {
                Ident = new Ident { Value = "int" }
            };
        }
        if (lit.StartsWith('"') && lit.EndsWith('"')) 
        {
            return new Ty()
            {
                Ident = new Ident { Value = "text" }
            };
        }
        throw new Lexing.LexerException("Failed to parse to Ty");
    }
    public static Ty From(string lit) => lit switch
    {
        "int" => new Ty() { Ident = new Ident { Value = lit } },
        "text" => new Ty() { Ident = new Ident { Value = lit } },
        _ => throw new Lexing.LexerException("Failed to parse to Ty"),
    };
    public override string Stringify() => Ident.Value;
}



