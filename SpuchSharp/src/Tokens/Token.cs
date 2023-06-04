using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

public readonly struct Location
{
    public override string ToString() => $"({Line}:{Column}) {File}";
    public required ulong Line { get; init; }
    public required ulong Column { get; init; }
    public required string File { get; init; }
}
public abstract class Token
{
    public Location? Location { get; set; }
    public abstract string Stringify();
}
public sealed class EOFToken : Token 
{
    public static readonly EOFToken Instance = new EOFToken();
    public override string Stringify() => "EOF";
}


