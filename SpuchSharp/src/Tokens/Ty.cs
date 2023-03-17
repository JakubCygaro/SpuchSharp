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
    public static FloatTy Float = new FloatTy();
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
        if (float.TryParse(lit, out float _))
            return Ty.Float;
        throw new Lexing.LexerException($"Failed to parse to Ty -> {lit}");
    }
    public static Ty FromCSharpType(Type type)
    {
        if (type == typeof(string))
            return Ty.Text;
        else if (type == typeof(string[]))
            return ArrayTy.Text;

        else if (type == typeof(int))
            return Ty.Int;
        else if (type == typeof(int[]))
            return ArrayTy.Int;

        else if (type == typeof(float))
            return Ty.Float;
        else if (type == typeof(float[]))
            return ArrayTy.Float;

        else if (type == typeof(bool))
            return Ty.Boolean;
        else if (type == typeof(bool[]))
            return ArrayTy.Boolean;

        else if (type == typeof(void))
            return Ty.Void;

        else if (type == typeof(object))
            return Ty.Any;
        else if (type == typeof(object[]))
            return ArrayTy.Any;

        throw new InvalidCastException(
            $"Could not translate external function type {type} to any internal type");
    }
    public static Ty? From(string lit) => lit switch
    {
        "int" => Ty.Int,
        "text" => Ty.Text,
        "bool" => Ty.Boolean,
        "void" => Ty.Void,
        "float" => Ty.Float,
        _ => null,
    };
    public override string Stringify() => Ident.Value;
    public override bool Equals(object? obj)
    {
        if (obj is Ty other)
            return this == other;
        else
            return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    public bool Equals(Ty? other)
    {
        if (other is null)
        {
            return false;
        }
        if (this is AnyTy)
            return true;
        if (this is ArrayTy arr)
            return ArrayTyEquals(arr, other);
        return this.Ident.Equals(other.Ident);
    }
    public static bool operator == (Ty a, Ty b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(Ty a, Ty b) => !(a == b);
    private bool ArrayTyEquals(ArrayTy arr, Ty other)
    {
        if (other is not ArrayTy otherArray)
            return false;
        if (otherArray.OfType.Equals(ArrayTy.Any))
            return true;
        return arr.OfType.Equals(otherArray.OfType);
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
internal sealed class FloatTy : Ty
{
    static Ident _ident = new Ident() { Value = "float" };
    public override Ident Ident => _ident;
}
internal sealed class ArrayTy : Ty
{
    private Ty _type;
    public Ty OfType => _type;

    private Ident _ident;
    public override Ident Ident => _ident;
    public static ArrayTy ArrayOf(Ty type)
    {
        return type switch
        {
            IntTy => Int,
            FloatTy => Float,
            BooleanTy => Boolean,
            TextTy => Text,
            AnyTy => Any,
            _ =>  new ArrayTy(type)
        };
    }
    private ArrayTy(Ty type)
    {
        _type = type;
        _ident = new Ident() { Value = $"[{_type.Ident.Value}]" };
    }

    new public static ArrayTy Int = new ArrayTy(Ty.Int);
    new public static ArrayTy Float = new ArrayTy(Ty.Float);
    new public static ArrayTy Boolean = new ArrayTy(Ty.Boolean);
    new public static ArrayTy Text = new ArrayTy(Ty.Text);
    new public static ArrayTy Any = new ArrayTy(Ty.Any);
}