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
        : base($"{message} `{token?.Stringify()}` {token?.Location}") { }
    public ParserException(string message, Token expected, Token token)
        : base($"{message}, expected: `{expected.Stringify()}` got: `{token.Stringify()}` " +
            $"{token.Location}") { }
    public static ParserException Expected<T>(Token? wrongToken)
        where T: Token
    {
        return new ExpectedTokenException(wrongToken);
    }
    public static ParserException PrematureEndOfInput(Location? location = default)
    {
        return new PrematureEndOfInputException(location);
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
public class AssignmentTargetException : ParserException
{
    public AssignmentTargetException(string message, Location? loc) 
        : base(message, loc) { }
    public AssignmentTargetException(Location? loc) 
        : base("This expression cannot be a target for an assignment", loc) { }
}
public class UnknownAssignmentException : ParserException
{
    public UnknownAssignmentException(string message, Location? loc)
        : base(message, loc) { }
    public UnknownAssignmentException(Location? loc)
        : base("Unknown assignment type", loc) { }
}
public class PrematureEndOfInputException : ParserException
{
    public PrematureEndOfInputException(string message, Location? loc)
        : base(message, loc) { }
    public PrematureEndOfInputException(Location? loc)
        : base("Premature end of input", loc) { }
}
public class KeywordException : ParserException
{
    public KeywordException(string message, Location? loc)
        : base(message, loc) { }
    public KeywordException(Location? loc)
        : base("Failed to parse keyword instruction", loc) { }
}
public class DisallowedPubUsageException : ParserException
{
    public DisallowedPubUsageException(string message, Location? loc)
        : base(message, loc) { }
    public DisallowedPubUsageException(Location? loc)
        : base("Disallowed pub keyword usage", loc) { }
}
public class ExpectedTokenException : ParserException
{
    public ExpectedTokenException(Token? token)
        : base($"Expected token, got `{token?.Stringify()}`", token?.Location) { }
}
public class UnexpectedTokenException : ParserException
{
    public UnexpectedTokenException(Token? token)
        : base($"Unexpected token `{token?.Stringify()}`", token?.Location) { }
}
public class FailedToParseToTypeException : ParserException
{
    public FailedToParseToTypeException(string message, Location? loc)
        : base(message, loc) { }
    public FailedToParseToTypeException(Location? loc)
        : base("Failed to parse to a type", loc) { }
}
public class IfClauseSyntaxException : ParserException
{
    public IfClauseSyntaxException(string message, Location? loc)
        : base(message, loc) { }
}
public class ImportStatementSyntaxException : ParserException
{
    public ImportStatementSyntaxException(string message, Location? loc)
        : base(message, loc) { }
}
public class DeleteStatementSyntaxException : ParserException
{
    public DeleteStatementSyntaxException(string message, Location? loc)
        : base(message, loc) { }
}
public class FailedToParseExpressionException : ParserException
{
    public FailedToParseExpressionException(string message, Location? loc)
        : base(message, loc) { }
    public FailedToParseExpressionException(Location? loc)
        : base("Failed to parse an expression", loc) { }
}
public class OperatorException : ParserException
{
    public OperatorException(string message, Location? loc)
        : base(message, loc) { }
    public OperatorException(Location? loc)
        : base("Wrong usage of an operator", loc) { }
}
public class ArrayException : ParserException
{
    public ArrayException(string message, Location? loc)
        : base(message, loc) { }
    public ArrayException(Location? loc)
        : base("Array initialization wrong syntax", loc) { }
}
public class FunctionDeclarationException : ParserException
{
    public FunctionDeclarationException(string message, Location? loc)
        : base(message, loc) { }
    public FunctionDeclarationException(Location? loc)
        : base("Function declaration wrong syntax", loc) { }
}