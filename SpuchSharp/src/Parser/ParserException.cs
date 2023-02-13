using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Parsing;

[Serializable]
public class ParserException: Exception
{
    public ParserException() { }
    public ParserException(string message, Location? location = default) 
        : base($"{message}. {location}")
    {
    }
    public ParserException(string message, Token? token)
        : base($"{message}. {token?.Location} | Type: {token} Str: {token?.Stringify()}") { }
    public ParserException(string message, Token expected, Token token)
        : base($"{message}, expected: `{expected.Stringify()}`. " +
            $"{token.Location} | Type: {token} Str: {token.Stringify()}") { }
    public static ParserException Expected<T>(Token? wrongToken)
        where T: Token
    {
        var message = $"Unrecognized token `{wrongToken?.Stringify()}`, expected {nameof(T)}";
        return new ParserException(message, wrongToken!);
    }
    public static ParserException PrematureEndOfInput(Location? location= default)
    {
        var message = $"Premature end of input";
        return new ParserException(message, location);
    }
    public static ParserException UnexpectedToken(Token token)
    {
        var message = $"Unexpected token";
        return new ParserException(message, token);
    }
    public ParserException(string message) : base(message) { }
    public ParserException(string message, Exception inner) : base(message, inner) { }
    protected ParserException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
