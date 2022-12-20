using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;
public abstract class Token
{
    public abstract string Stringify();
}

public sealed class Ident : Token 
{
    public required string Value { get; set; }
    public override string Stringify() => Value;
}
public sealed class Value : Token 
{
    public required Ty Ty { get; set; }
    public required object Val { get; set; }
    public override string Stringify() => $"{Ty} {Val}";
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
    public override string Stringify() => Ident.Value;
}



