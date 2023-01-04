using SpuchSharp.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Parsing;

[Serializable]
public class ParserException : Exception
{
    public ParserException() { }
    public ParserException(string message, Location? location = default) 
        : base($"{message}. {location}")
    {
    }
    public ParserException(string message, Token token)
        : base($"{message}. {token.Location} | Type: {token} Str: {token.Stringify()}") { }
    public ParserException(string message) : base(message) { }
    public ParserException(string message, Exception inner) : base(message, inner) { }
    protected ParserException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
