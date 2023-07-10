using SpuchSharp.API;
using SpuchSharp.Instructions;
using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Interpreting;
internal class SFunction : SObject
{
    public override string Display()
    {
        StringBuilder sb = new();
        sb.AppendLine($"{Ident}");
        sb.Append('(');
        foreach (var arg in Args)
            sb.Append($"{arg.Ty} {arg.Name},");
        sb.Append(')');
        return sb.ToString();
    }
    public required Parsing.FunArg[] Args { get; init; }
    public required Instruction[] Block { get; init; }
    public required Ty ReturnTy { get; init; }
    public required WeakReference<Module>? ParentModule { get; init; }
}

internal class ExternalFunction : SFunction
{
    public required System.Reflection.MethodInfo MethodInfo { private get; init; }
    public Value Invoke(object[] arguments, Location? loc = default)
    {
        object? ret = null;
        try
        {
            ret = MethodInfo.Invoke(null, arguments);
        }
        catch (Exception ex)
        {
            throw new ExternalLibraryException($"An external library function `{this.Ident.Stringify()}` has thrown an exception, at {loc}", ex);
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
