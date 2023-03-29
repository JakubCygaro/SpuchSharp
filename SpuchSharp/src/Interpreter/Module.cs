using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp.Tokens;
using System.Runtime;

using VariableScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;

namespace SpuchSharp.Interpreting;
internal sealed class Module
{
    public required Ident Ident { get; init; }
    public required Dictionary<Ident, Module> Modules { get; set; } = new();
    public required VariableScope VariableScope { get; init; }
    public required VariableScope OwnedVariables { get; init; }
    public required FunctionScope FunctionScope { get; init; }
    public required FunctionScope OwnedFunctions { get; init; }
    public required WeakReference<Module>? ParentModule { get; init; }
    public FunctionScope UsedFunctions { get; init; } = new();
    public bool IsExternal { get; init; } = false;

    //public override int GetHashCode() => Ident.GetHashCode();
}
