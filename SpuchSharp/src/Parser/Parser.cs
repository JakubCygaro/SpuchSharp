using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpuchSharp.Instructions;
using SpuchSharp.Lexing;
using SpuchSharp.Tokens;
using SpuchSharp;
using System.IO;

namespace SpuchSharp.Parsing;

internal sealed class Parser : IEnumerable<Instruction>, IEnumerator<Instruction>
{
    public Instruction Current => _currentInstruction;
    object IEnumerator.Current => _currentInstruction;

    private Instruction _currentInstruction = default!;
    private readonly Lexer _lexer;
    public Parser(Lexer lexer) 
    {
        _lexer = lexer;
    }

    private bool Parse()
    {
        foreach(Token token in _lexer)
        {
            var location = token.Location;
            _currentInstruction = ParseInstruction(token, _lexer);
            _currentInstruction.Location = location;
            return true;
        }
        return false;

    }
    private Instruction ParseInstruction(Token firstToken, INullEnumerator<Token> stream)
    {
        if (firstToken is KeyWord keyWord)
            return ParseKeyWordInstruction(keyWord, stream);
        if (firstToken is Ty type)
            return ParseDeclaration(type, stream);
        if (firstToken is Ident ident)
        {
            var secondToken = stream.Next();
            if (secondToken is Assign)
                return new Assignment
                {
                    Expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
                    Left = ident
                };
            else if (secondToken is not Semicolon and not null)
            {
                var tokens = new List<Token>() 
                { 
                    firstToken, 
                    secondToken
                };
                tokens.AddRange(ReadToSemicolon(stream));
                return ParseExpression(tokens.ToTokenStream());
            }
            else if (secondToken is Semicolon)
            {
                var tokens = new List<Token>() 
                {
                    firstToken
                };
                return ParseExpression(tokens.ToTokenStream());
            }
            else
                throw new ParserException("TODO 60");
        }
        else
        {
            var tokens = new List<Token>() 
            {
                firstToken,
            };
            tokens.AddRange(ReadToSemicolon(stream));
            return ParseExpression(tokens.ToTokenStream());
        }
    }
    private Instruction ParseKeyWordInstruction(KeyWord keyword, INullEnumerator<Token> stream)
    {
        if (keyword is Fun)
            return ParseDeclaration(keyword, stream);
        else if (keyword is Var)
            return ParseDeclaration(keyword, stream);
        else if (keyword is Delete)
            return ParseDelete(keyword);
        else if (keyword is Import)
            return ParseImport(keyword);
        else if (keyword is Return)
            return ParseReturn(keyword);
        else if (keyword is If)
            return ParseIfStatement(keyword, stream);
        else
            throw new ParserException("Failed to parse keyword instruction!", keyword);
    }
    private ReturnStatement ParseReturn(KeyWord keyword)
    {
        var expr = ParseExpression(ReadToSemicolon(_lexer).ToTokenStream());
        return new ReturnStatement
        {
            Expr = expr,
            Location = keyword.Location,
        };
    }
    private IfStatement ParseIfStatement(KeyWord keyWord, INullEnumerator<Token> stream)
    {
        if (keyWord is not If)
            throw ParserException.Unexpected<If>(keyWord);
        if (stream.Next() is not Round.Open)
            throw ParserException.Unexpected<Round.Open>(keyWord);
        var expressions = ParseInsideRound(stream);
        if (expressions.Count != 1)
            throw new ParserException("An if statement clause must be only a single logical expression",
                stream.Current);

        var expressionStream = expressions[0];
        var expr = ParseExpression(expressionStream);

        if (stream.Next() is not Curly.Open)
            throw new ParserException("An if statement requires a block of instructions in curly braces",
                stream.Current);

        var block = ParseFunctionInstructions(stream);

        return new IfStatement
        {
            Expr = expr,
            Location = keyWord.Location,
            Block = block,
        };

    }
    private Instruction ParseImport(KeyWord Keyword)
    {
        if (_lexer.Next() is not TextValue value)
            throw new ParserException("An import statement takes an external library path as an argument.",
                _lexer.Current);
        if (!value.Ty.Equals(Ty.Text))
            throw new ParserException("The path to an external library must be a string value.",
                _lexer.Current);
        if (_lexer.Next() is not Semicolon)
            throw new ParserException("Expected semicolon.",
                _lexer.Current);
        return new ImportStatement
        {
            Path = value.Value,
        };
    }
    private Instruction ParseDelete(KeyWord keyWord)
    {
        if (_lexer.Next() is not Ident ident)
            throw new ParserException("A delete statement takes a variable name as a parameter.",
                _lexer.Current);
        if(_lexer.Next() is not Semicolon)
            throw new ParserException("Expected semicolon.",
                _lexer.Current);
        return new DeleteStatement
        {
            VariableIdent = ident,
        };
    }
    private Expression ParseExpression(INullEnumerator<Token> stream)
    {
        Expression? ret = null;
        Operator? currentOperator = null;
        while(stream.Next() is Token token)
        {
            SimpleExpression simpleExpression = ParseIdentOrValueExpression(token);

            var nextToken = stream.Next();

            if (nextToken is Round.Open && simpleExpression is IdentExpression identExpression)
            {
                var insideParen = ParseInsideRound(stream);
                simpleExpression = ParseCall(identExpression.Ident, insideParen);
                nextToken = stream.Next();
            }
            if(currentOperator is not null && ret is not null)
            {
                ret = ComplexExpression.From(ret, currentOperator, simpleExpression);
            }
            else
            {
                ret = simpleExpression;
            }
            if (nextToken is Operator nextOperator)
            {
                currentOperator = nextOperator;
            }
        }
        return ret ?? 
            throw new ParserException("Failed to parse an expression");
    }

    private List<TokenStream> ParseInsideRound(INullEnumerator<Token> stream)
    {
        List<TokenStream> streams = new();
        List<Token> tokens = new();
        int openParen = 1;
        while(stream.Next() is Token token)
        {
            // gotta fix this so it does not ommit inner ()
            if (token is Round.Open)
                openParen += 1;
            if (token is Round.Closed)
                openParen -= 1;
            if (openParen == 0)
            {
                if(tokens.Count > 0)
                    streams.Add(tokens.ToTokenStream());
                return streams;
            }
            if (token is Comma && openParen == 1)
            {
                streams.Add(tokens.ToTokenStream());
                tokens = new();
            }
            else
                tokens.Add(token);
        }
        throw new ParserException("Unclosed parentheses", stream.Current);
    }
    
    private CallExpression ParseCall(Ident ident, 
        List<TokenStream> tokenStreams)
    {
        List<Expression> expressions = new();
        foreach(var stream in tokenStreams)
        {
            expressions.Add(ParseExpression(stream));
        }
        return new CallExpression
        {
            Args = expressions.ToArray(),
            Function = ident,
            Location = ident.Location,
        };
    }
    private SimpleExpression ParseIdentOrValueExpression(Token token)
    {
        if (token is Value value)
            return new ValueExpression { Val = value };
        else if (token is Ident ident)
            return new IdentExpression { Ident = ident };
        else
            throw new ParserException("Invalid token.", token);
    }
    private Assignment ParseAssignment(Token token, INullEnumerator<Token> stream)
    {
        if (token is not Ident ident)
            throw new ParserException("Could not parse to assignment, expected ident", token);
        if (stream.Next() is not Assign)
            throw new ParserException("Could not parse to assignment, expected `=`", token);

        return new Assignment
        {
            Expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
            Left = ident
        };
    }

    /// <summary>
    /// Returns a list of all tokens in a stream before the first encounter of <c>T</c>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    private List<Token> ReadToSemicolon(INullEnumerator<Token> stream)
    {
        return ReadToToken<Semicolon>(stream);
    }
    private List<Token> ReadToToken<T>(INullEnumerator<Token> stream)
        where T: Token
    {
        List<Token> tokens = new List<Token>();

        while (stream.Next() is Token token)
            if (token is not T)
                tokens.Add(token);
            else
                return tokens;
        throw new ParserException("Premature end of input");
    }
    private Instruction ParseDeclaration(Token token, INullEnumerator<Token> stream)
    {
        if (token is Var var)
        {
            if (stream.Next() is not Ident ident)
                throw new ParserException("Invalid token error", stream.Current);
            if (stream.Next() is not Assign)
                throw new ParserException("Invalid token error", stream.Current);
            //if (_lexer.Next() is not Token tok)
            //    throw new ParserException("Invalid token error", _lexer.Current);
            return new Variable()
            {
                Name = ident.Value,
                Expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
            };
        }
        else if (token is Ty ty)
        {
            if (stream.Next() is not Ident ident)
                throw new ParserException("Invalid token error", stream.Current);
            if (stream.Next() is not Assign)
                throw new ParserException("Invalid token error", stream.Current);
            //if (_lexer.Next() is not Token tok)
            //    throw new ParserException("Invalid token error", _lexer.Current);
            return new Typed()
            {
                Name = ident.Value,
                Expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
                Type = ty,
            };
        }
        else if (token is Fun fun)
        {
            if (stream.Next() is not Ident ident)
                throw new ParserException("Invalid token error", stream.Current);
            if (stream.Next() is not Round.Open)
                throw new ParserException("Invalid token error", stream.Current);
            //parse function args
            var arguments = ParseFunctionArguments(stream);

            var next = stream.Next();
            Ty type = Ty.Void;
            if(next is Ty typeName)
            {
                type = typeName;
                next = stream.Next();
            }
            if (next is not Curly.Open)
                throw new ParserException("Invalid token error", stream.Current);
            var instructions = ParseFunctionInstructions(stream);

            return new Function()
            {
                Args = arguments,
                Block = instructions,
                Name = ident,
                ReturnTy = type,
            };
        }
        else
            throw new ParserException("Invalid syntax error!", token);
    }
    private FunArg[] ParseFunctionArguments(INullEnumerator<Token> stream)
    {
        List<FunArg> args = new List<FunArg>();
        while(stream.Next() is Token token)
        {
            if (token is Round.Closed)
                break;
            if (token is Comma)
                continue;
            if (token is not Ty type)
                throw new ParserException(
                    $"Invalid function argument declaration, not a type: {stream.Current}", stream.Current);
            if (stream.Next() is not Ident ident)
                throw new ParserException("Invalid function argument declaration", stream.Current);
            var arg = new FunArg()
            {
                Name = ident,
                Ty = type,
                Location = ident.Location,
            };
            if (args.Any(a => a.Name.Equals(ident)))
                throw new ParserException($"Function arguments cannot have repeating names", ident);
            args.Add(arg);
        }

        return args.ToArray();
    }
    private Instruction[] ParseFunctionInstructions(INullEnumerator<Token> stream)
    {
        List<Instruction> list = new List<Instruction>();
        while(stream.Next() is Token token)
        {
            if (token is Curly.Closed)
                break;
            list.Add(ParseInstruction(token, stream));
        }
        return list.ToArray();
    }


    public IEnumerator<Instruction> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;

    public bool MoveNext() => Parse();

    public void Reset()
    {
        _lexer.Reset();
    }
    public void Dispose() { }
}
