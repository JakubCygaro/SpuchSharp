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
            _currentInstruction = ParseInstruction(token);
            _currentInstruction.Location = location;
            return true;
        }
        return false;

    }
    private Instruction ParseInstruction(Token firstToken)
    {
        if (firstToken is KeyWord keyWord)
            return ParseKeyWordInstruction(keyWord);
        if (firstToken is Ty type)
            return ParseDeclaration(type);
        if (firstToken is Ident ident)
        {
            var secondToken = _lexer.Next();
            if (secondToken is Assign)
                return new Assignment
                {
                    Expr = ParseExpression(ReadToSemicolon(_lexer).ToTokenStream()),
                    Left = ident
                };
            else if (secondToken is not Semicolon and not null)
            {
                var tokens = new List<Token>() 
                { 
                    firstToken, 
                    secondToken
                };
                tokens.AddRange(ReadToSemicolon(_lexer));
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
            tokens.AddRange(ReadToSemicolon(_lexer));
            return ParseExpression(tokens.ToTokenStream());
        }
    }
    private Instruction ParseKeyWordInstruction(KeyWord keyword)
    {
        if (keyword is Fun)
            return ParseDeclaration(keyword);
        else if (keyword is Var)
            return ParseDeclaration(keyword);
        else if (keyword is Delete)
            return ParseDelete(keyword);
        else if (keyword is Import)
            return ParseImport(keyword);
        else if (keyword is Return)
            return ParseReturn(keyword);
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
<<<<<<< Updated upstream
=======
            else if (nextToken is Square.Open && simpleExpression is IdentExpression identForIndex)
            {
                var (tokens, _) = ReadToToken<Square.Closed>(stream);
                var indexerExpression = ParseExpression(tokens.ToTokenStream());
                var indexer = new IndexerExpression
                {
                    Target = identForIndex,
                    IndexExpression = indexerExpression,
                    Location = identForIndex.Location,
                };
                simpleExpression = indexer;
                nextToken = stream.Next();
                while(nextToken is Square.Open)
                {
                    (tokens, _) = ReadToToken<Square.Closed>(stream);
                    indexerExpression = ParseExpression(tokens.ToTokenStream());
                    simpleExpression = new IndexerExpression
                    {
                        Target = indexer,
                        IndexExpression = indexerExpression,
                        Location = indexer.Location,
                    };
                    nextToken = stream.Next();
                }
            }
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
=======
    private ArrayExpression ParseArrayExpression(TokenStream stream)
    {
        var tokens = ParseBetweenParenWithSeparator<Curly.Open, Curly.Closed, Comma>(stream, 1);
        List<Expression> expressions = new();

        foreach(var tokenStream in tokens)
        {
            expressions.Add(ParseExpression(tokenStream));
        }
        return new ArrayExpression
        {
            Expressions = expressions.ToArray(),
            Location = null
        };
    }
>>>>>>> Stashed changes

    private List<TokenStream> ParseInsideRound(INullEnumerator<Token> stream)
    {
        List<TokenStream> streams = new();
        List<Token> tokens = new();
        int openParen = 1;
        while(stream.Next() is Token token)
        {
            if (token is Round.Open)
                openParen += 1;
            else if (token is Round.Closed)
                openParen -= 1;
            else if (token is Comma && openParen == 1)
            {
                streams.Add(tokens.ToTokenStream());
                tokens = new();
            }
            else
                tokens.Add(token);
            if (openParen == 0)
            {
                if(tokens.Count > 0)
                    streams.Add(tokens.ToTokenStream());
                return streams;
            }
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
<<<<<<< Updated upstream
    private Instruction ParseDeclaration(Token token)
=======
    /// <summary>
    /// Does the same as <c>ReadToToken()</c> but does not throw an exception if the <c>T</c> was not encountered before the end of the stream
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <returns></returns>
    private (List<Token>, T?) ReadToTokenOrEnd<T>(INullEnumerator<Token> stream)
        where T : Token
    {
        List<Token> tokens = new List<Token>();
        var location = default(Location);
        while (stream.Next() is Token token)
        {
            if (token.Location is not null)
                location = (Location)token.Location;
            if (token is not T)
                tokens.Add(token);
            else if (token is T tokenT)
                return (tokens, tokenT);
        }
        return (tokens, null);
    }
    private Declaration ParseUntypedArrayDecl(Ident ident, TokenStream stream)
    {
        return new ArrayDecl
        {
            ArrayExpression = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
            Name = ident.Value,
            Location = ident.Location
        };
    }
    private Instruction ParseDeclaration(Token token, TokenStream stream)
>>>>>>> Stashed changes
    {
        if (token is Var var)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assign)
                throw new ParserException("Invalid token error", _lexer.Current);
            //if (_lexer.Next() is not Token tok)
            //    throw new ParserException("Invalid token error", _lexer.Current);
            return new Variable()
            {
                Name = ident.Value,
                Expr = ParseExpression(ReadToSemicolon(_lexer).ToTokenStream()),
            };
        }
        else if (token is Ty ty)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assign)
                throw new ParserException("Invalid token error", _lexer.Current);
            //if (_lexer.Next() is not Token tok)
            //    throw new ParserException("Invalid token error", _lexer.Current);
            return new Typed()
            {
                Name = ident.Value,
                Expr = ParseExpression(ReadToSemicolon(_lexer).ToTokenStream()),
                Type = ty,
            };
        }
        else if (token is Fun fun)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Round.Open)
                throw new ParserException("Invalid token error", _lexer.Current);
            //parse function args
            var arguments = ParseFunctionArguments();

            var next = _lexer.Next();
            Ty type = Ty.Void;
            if(next is Ty typeName)
            {
                type = typeName;
                next = _lexer.Next();
            }
            if (next is not Curly.Open)
                throw new ParserException("Invalid token error", _lexer.Current);
            var instructions = ParseFunctionInstructions();

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
    private FunArg[] ParseFunctionArguments()
    {
        List<FunArg> args = new List<FunArg>();
        while(_lexer.Next() is Token token)
        {
            if (token is Round.Closed)
                break;
            if (token is Comma)
                continue;
            if (token is not Ty type)
                throw new ParserException(
                    $"Invalid function argument declaration, not a type: {_lexer.Current}", _lexer.Current);
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid function argument declaration", _lexer.Current);
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
    private Instruction[] ParseFunctionInstructions()
    {
        List<Instruction> list = new List<Instruction>();
        while(_lexer.Next() is Token token)
        {
            if (token is Curly.Closed)
                break;
            list.Add(ParseInstruction(token));
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
