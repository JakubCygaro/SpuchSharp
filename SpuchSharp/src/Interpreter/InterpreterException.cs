using SpuchSharp.Instructions;
using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Interpreting;


[Serializable]
public class InterpreterException : Exception
{
	public InterpreterException() { }
	public InterpreterException(string message) : base(message) { }
	public InterpreterException(string message, Tokens.Token token) 
		: base($"{message} {token.Location}") 
	{ }
    public InterpreterException(string message, Location? location = null)
        : base($"{message} {location}")
    { }
    internal InterpreterException(string message, Instructions.Instruction instruction) 
		: base($"{message} {instruction.Location}") 
	{ }
	internal static InterpreterException UnsuportedInstruction(Instruction ins)
	{
		var message = $"Instruction is not yet supported by the interpreter: {ins}";
		return new InterpreterException(message, ins);
	}
	internal static InterpreterException TypeMismatch<TExpected>(Ty ty)
		where TExpected : Ty
	{
		var message = $"Type mismatch, expected {nameof(TExpected)}, got {ty.Ident}";
		return new InterpreterException(message, ty);
	}
	internal static InterpreterException VariableNotFound(Ident ident)
	{
		var message = $"Variable {ident.Value} not found in present scope.";
		return new InterpreterException(message, ident);
	}
	internal static InterpreterException InvalidCast(Ident to, Value from)
	{
		var message = $"Invalid cast, cannot cast type {from.Ty.Stringify()} into {to.Stringify()}";
        return new InterpreterException(message, from);
    }
    internal static InterpreterException InvalidOperation(string operation, Value a, Value b)
    {
        var message = $"Invalid operation, {operation} not possible between values of type " +
			$"{a.Ty.Stringify()} {b.Ty.Stringify()}";
        return new InterpreterException(message, a);
    }
	internal static InterpreterException ConstantReassignment(SVariable variable, Location? location = null)
	{
		var message = $"Tried to reassing a constant variable `{variable.Ident.Stringify()}`";
		return new InterpreterException(message, location);
	}
    internal static InterpreterException ConstantReassignment(string name, Location? location = null)
    {
        var message = $"Tried to reassing a constant variable `{name}`";
        return new InterpreterException(message, location);
    }
    internal static InterpreterException ConstantReassignment(Ident variableName, Location? location = null)
    {
        var message = $"Tried to reassing a constant variable `{variableName.Stringify()}`";
        return new InterpreterException(message, location);
    }
    public InterpreterException(string message, Exception inner) : base(message, inner) { }
	protected InterpreterException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
