using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.API;
using SpuchSharp.Instructions;
using SpuchSharp.Tokens;
using System.Runtime;

namespace SpuchSharp.Interpreting;

internal abstract class SObject
{
    public required Ident Ident { get; init; }
    public override int GetHashCode()
    {
        return Ident.GetHashCode();
    }

    public abstract string Display();
    public required bool IsPublic { get; init; }

}

internal class SStruct : SObject
{
    public override string Display()
    {
        StringBuilder sb = new();
        sb.AppendLine($$"""struct {{Ident.Stringify()}} {""");
        foreach(var (ident, value) in Fields)
        {
            sb.AppendLine($"{value.Ty} {ident.Stringify()} = {value.ValueAsObject};");
        }
        sb.AppendLine(@"}");
        return sb.ToString();
    }
    public required Dictionary<Ident, Value> Fields { get; init; } = new();

}

