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

internal class SStruct : SVariable
{
    private StructValue _structValue;
    public override Value Value { get => _structValue; set => Set(value); }
    
    public SStruct(StructValue structValue)
    {
        _structValue = structValue;
    }

    public void Set(Value value)
    {
        if (value is not StructValue sV)
            throw new InterpreterException(
                $"Type mismatch, expected value of struct type `{Value.Ty.Stringify()}` " +
                $"\nbut got a value of type `{value.Ty.Stringify()}`", value.Location);
        if (sV.Ty != Value.Ty)
            throw new InterpreterException(
                $"Type mismatch, expected value of struct type `{Value.Ty.Stringify()}` " +
                $"\nbut got a struct value of type `{sV.Ty.Stringify()}`", value.Location);
        this._structValue = sV;
    }

    public override string Display()
    {
        StringBuilder sb = new();
        sb.AppendLine($$"""struct {{Ident.Stringify()}} {""");
        foreach (var (ident, value) in _structValue.Fields)
        {
            sb.AppendLine($"{value.Ty} {ident.Stringify()} = {value.ValueAsObject};");
        }
        sb.AppendLine(@"}");
        return sb.ToString();
    }
}

