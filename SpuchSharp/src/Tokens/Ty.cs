using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

internal abstract class Ty : Token, IEquatable<Ty>
{
    public static TextTy Text = new TextTy();
    public static IntTy Int = new IntTy();
    public static BooleanTy Boolean = new BooleanTy();
    public static VoidTy Void = new VoidTy();
    public static AnyTy Any = new AnyTy();
    public static NothingTy Nothing = new NothingTy();
    public abstract Ident Ident { get; }
    public static Ty FromValue(string lit)
    {
        if (lit.ToCharArray().All(char.IsDigit))
        {
            return Ty.Int;
        }
        if (lit.StartsWith('"') && lit.EndsWith('"'))
        {
            return Ty.Text;
        }
        if (lit == "false" || lit == "true")
        {
            return Ty.Boolean;
        }
        throw new Lexing.LexerException("Failed to parse to Ty");
    }
    public static Ty FromCSharpType(Type type)
    {
        if (type == typeof(string))
            return Ty.Text;
        else if (type == typeof(int))
            return Ty.Int;
        else if (type == typeof(bool))
            return Ty.Boolean;
        else if (type == typeof(void))
            return Ty.Void;
        else if (type == typeof(object))
            return Ty.Any;
        throw new InvalidCastException(
            $"Could not translate external function type {type} to any internal type");
    }
    public static Ty From(string lit) => lit switch
    {
        "int" => Ty.Int,
        "text" => Ty.Text,
        "bool" => Ty.Boolean,
        "void" => Ty.Void,
        _ => throw new Lexing.LexerException("Failed to parse to Ty"),
    };
    public override string Stringify() => Ident.Value;

    public bool Equals(Ty? other)
    {
        if (this is AnyTy)
            return true;
        if (other is null)
        {
            return false;
        }
        return this.Ident.Equals(other.Ident);
    }

}

internal sealed class TextTy : Ty
{
    static Ident _ident = new Ident() { Value = "text" };
    public override Ident Ident => _ident;
}
internal sealed class IntTy : Ty
{
    static Ident _ident = new Ident() { Value = "int" };
    public override Ident Ident => _ident;
}
internal sealed class BooleanTy : Ty
{
    static Ident _ident = new Ident() { Value = "bool" };
    public override Ident Ident => _ident;
}
internal sealed class VoidTy : Ty
{
    static Ident _ident = new Ident() { Value = "void" };
    public override Ident Ident => _ident;
}
internal sealed class AnyTy : Ty
{
    static Ident _ident = new Ident() { Value = "any" };
    public override Ident Ident => _ident;
}
internal sealed class NothingTy : Ty
{
    static Ident _ident = new Ident() { Value = "nothing_type" };
    public override Ident Ident => _ident;
}
