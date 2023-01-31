using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.API;
using SpuchSharp.Instructions;
using SpuchSharp.Tokens;

namespace SpuchSharp.Interpreting;

internal abstract class SObject
{
    public required Ident Ident { get; init; }
    public override int GetHashCode()
    {
        return Ident.GetHashCode();
    }

    public abstract string Display();
}

internal class SVariable : SObject
{
    public required Tokens.Value Value { get; set; }
    public override string Display() => $"{Value.Ty.Stringify()} = {Value.ValueAsObject}";
}

internal class SFunction : SObject
{
    public override string Display()
    {
        StringBuilder sb = new();
        sb.AppendLine($"{Ident}");
        sb.Append('(');
        foreach(var arg in Args)
            sb.Append($"{arg.Ty} {arg.Name},");
        sb.Append(')');
        return sb.ToString();
    }
    public required Parsing.FunArg[] Args { get; init; }
    public required Instruction[] Block { get; init; }
    public required Ty ReturnTy { get; init; }
}

internal class ExternalFunction : SFunction
{
    public required System.Reflection.MethodInfo MethodInfo { private get; init; }
    public Value Invoke(object[] arguments)
    {
        object? ret = null;
        try
        {
            ret = MethodInfo.Invoke(null, arguments);
        }
        catch(Exception ex) 
        {
            throw new ExternalLibraryException(ex.Message, ex);
        }
        // schizo type marshalling back into spuch#
        return Value.FromObject(ret);
    }
    public override string Display()
    {
        Console.WriteLine("[EXTERNAL FUNCTION]");
        StringBuilder sb = new();
        sb.AppendLine($"{MethodInfo.Name}");
        sb.Append('(');
        foreach (var arg in Args)
            sb.Append($"{arg.Ty} {arg.Name},");
        sb.Append(')');
        return sb.ToString();
    }
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

