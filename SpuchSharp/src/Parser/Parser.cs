using System;
using System.Collections;
using SpuchSharp.Instructions;
using SpuchSharp.Interpreting;
using SpuchSharp.Lexing;
using SpuchSharp.Tokens;

namespace SpuchSharp.Parsing;

internal sealed class Parser : IEnumerable<Instruction>, IEnumerator<Instruction>
{
    public Instruction Current => _currentInstruction;
    object IEnumerator.Current => _currentInstruction;

    private Instruction _currentInstruction = default!;
    private readonly TokenStream _tokenStream;
    public Parser(Lexer lexer) 
    {
        var tokens = lexer.ToList();
        _tokenStream = new TokenStream(tokens);
    }
    private bool Parse()
    {
        
        foreach(Token token in _tokenStream)
        {
            var location = token.Location;
            _currentInstruction = ParseInstruction(token, _tokenStream);
            _currentInstruction.Location = location;
            return true;
        }
        return false;

    }
    private Instruction ParseInstruction(Token firstToken, TokenStream stream)
    {
        if (firstToken is KeyWord keyWord)
            return ParseKeyWordInstruction(keyWord, stream);
        if (firstToken is Ty type)
            return ParseDeclaration(type, stream);
        if (firstToken is Square.Open squareOpen)
            return ParseArrayDeclaration(squareOpen, stream);

        var remainder = new List<Token> { firstToken };
        remainder.AddRange(ReadToSemicolon(stream));

        return ParseExpressionOrAssignment(remainder.ToTokenStream());
    }
    private Instruction ParseExpressionOrAssignment(TokenStream stream)
    {
        if (stream.Has<Assign>())
        {
            return ParseAssignment(stream);
        }
        else
        {
            return ParseExpression(stream);
        }
    }
    private Assignment ParseAssignment(TokenStream stream)
    {
        (var unparsedTarget, _) = ReadToToken<Assign>(stream);
        var expression = ParseExpression(stream);
        var target = ParseExpression(unparsedTarget.ToTokenStream());
        return target switch
        {
            IdentExpression ie => new Assignment 
            { 
                Expr = expression,
                Left = new IdentTarget 
                {  
                    Target = ie, 
                },
                Location = ie.Location
            },
            IndexerExpression ix => new Assignment 
            { 
                Expr = expression,
                Left = new ArrayIndexTarget 
                { 
                    Target = ix,
                    IndexExpression = ix.IndexExpression,
                },
                Location = ix.Location
            },
            _ => throw new ParserException("This expression cannot be a target for an assignment", 
                target.Location) 
        };
        
    }
    private Instruction ParseKeyWordInstruction(KeyWord keyword, TokenStream stream)
    {
        return keyword switch
        {
            Fun => ParseDeclaration(keyword, stream),
            Var => ParseDeclaration(keyword, stream),
            Delete => ParseDelete(keyword, stream),
            Import => ParseImport(keyword, stream),
            Return => ParseReturn(keyword, stream),
            If => ParseIfStatement(keyword, stream),
            Loop loop => ParseLoop(loop, stream),
            Break brek => ParseBreak(brek, stream),
            Skip skip => ParseSkip(skip, stream),
            For @for => ParseFor(@for, stream),
            While @while => ParseWhile(@while, stream),
            _ => throw new ParserException("Failed to parse keyword instruction!", keyword),
        };
    }
    private Ty ParseType(TokenStream stream)
    {
        var firstToken = stream.Next();
        if (firstToken is Ty normalTy)
            return normalTy;
        else if (firstToken is Square.Open)
        {
            var arrayTy = ParseType(stream);
            if (stream.Next() is not Square.Closed)
                throw ParserException.Expected<Square.Closed>(stream.Current);
            return ArrayTy.ArrayOf(arrayTy);
        }
        else
            throw new ParserException("Failed to parse to type", stream.Current);
    }
    private Declaration ParseArrayDeclaration(Square.Open squareOpen, TokenStream stream)
    {
        var ty = ParseType(stream) 
            as ArrayTy 
            ?? throw new ParserException("Array type not an array type!?");

        var nextToken = stream.Next();
        if (nextToken is not Square.Closed)
            throw ParserException.Expected<Square.Closed>(nextToken);

        nextToken = stream.Next();
        if (nextToken is not Ident ident)
            throw ParserException.Expected<Square.Closed>(nextToken);

        nextToken = stream.Next();
        if (nextToken is not Assign)
            throw ParserException.Expected<Assign>(nextToken);

        if (stream.Peek() is Square.Open)
        {
            stream.Next();
            var arrayTy = ArrayTy.ArrayOf(ty);
            //Console.WriteLine(arrayTy.Stringify());
            List<Expression> sizes = new();
            var size = ParseExpression(ReadToToken<Square.Closed>(stream).Item1.ToTokenStream());
            sizes.Add(size);
            while(arrayTy.OfType is ArrayTy arrayType)
            {
                nextToken = stream.Next();
                if (nextToken is not Square.Open)
                    throw ParserException.Expected<Square.Open>(nextToken);
                size = ParseExpression(ReadToToken<Square.Closed>(stream).Item1.ToTokenStream());
                sizes.Add(size);
                arrayTy = arrayType;
            }
            if (stream.Next() is not Semicolon)
                throw ParserException.Expected<Semicolon>(stream.Current);
            return new TypedArrayDecl
            {
                Type = ty,
                Name = ident.Value,
                ArrayExpression = ArrayExpression.Empty,
                Sized = sizes,
                Location = ident.Location,
            };
        }

        return new TypedArrayDecl
        {
            Type = ty,
            ArrayExpression = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
            Name = ident.Value,
            Sized = null,
            Location = ident.Location,
        };
    }
    private WhileStatement ParseWhile(While whileKeyword, TokenStream stream)
    {
        var tokens = ReadBetweenParen<Round.Open, Round.Closed>(stream);
        var expression = ParseExpression(tokens);
        var nextToken = stream.Next();
        if (nextToken is not Curly.Open)
            throw ParserException.Expected<Curly.Open>(nextToken);
        var block = ParseFunctionInstructions(stream);
        return new WhileStatement
        {
            Block = block,
            Condition = expression,
            Location = whileKeyword.Location,
        };
    }
    private ForLoopStatement ParseFor(For forKeyword, TokenStream stream)
    {
        // for x from <expr> to <expr> {
        var nextToken = stream.Next();
        if (nextToken is not Ident ident)
            throw ParserException.Expected<Ident>(nextToken);
        nextToken = stream.Next();
        if (nextToken is not From)
            throw ParserException.Expected<From>(nextToken);
        (var tokens, _) = ReadToToken<To>(stream);
        var expr1 = ParseExpression(tokens.ToTokenStream());
        (tokens, _) = ReadToToken<Curly.Open>(stream);
        var expr2 = ParseExpression(tokens.ToTokenStream());
        var block = ParseFunctionInstructions(stream);
        return new ForLoopStatement
        {
            VariableIdent = ident,
            From = expr1,
            To = expr2,
            Block = block,
            Location = forKeyword.Location,
        };

    }
    private LoopStatement ParseLoop(Loop loopKeyword, TokenStream stream)
    {
        var nextToken = stream.Next() ?? 
            throw ParserException.PrematureEndOfInput(loopKeyword.Location);
        if (nextToken is not Curly.Open)
            throw ParserException.Expected<Curly.Open>(nextToken);
        return new LoopStatement
        {
            Block = ParseFunctionInstructions(stream),
            Location = loopKeyword.Location,
        };
    }
    private BreakStatement ParseBreak(Break breakKeyword, TokenStream stream)
    {
        if (stream.Next() is not Semicolon)
            throw ParserException.Expected<Semicolon>(stream.Current);
        return new BreakStatement
        {
            Location = breakKeyword.Location,
        };
    }
    private SkipStatement ParseSkip(Skip skipKeyword, TokenStream stream)
    {
        if (stream.Next() is not Semicolon)
            throw ParserException.Expected<Semicolon>(stream.Current);
        return new SkipStatement
        {
            Location = skipKeyword.Location,
        };
    }
    private ReturnStatement ParseReturn(KeyWord keyword, TokenStream stream)
    {
        var tokens = ReadToSemicolon(stream);
        //var expr = ParseExpression(tokens.ToTokenStream());
        return new ReturnStatement
        {
            Expr = tokens.Count > 0 ? 
                    ParseExpression(tokens.ToTokenStream()) : 
                    new ValueExpression { Val = Value.Void, Location = null, },
            Location = keyword.Location,
        };
    }
    private IfStatement ParseIfStatement(KeyWord keyWord, TokenStream stream)
    {
        if (keyWord is not If)
            throw ParserException.Expected<If>(keyWord);
        if (stream.Next() is not Round.Open)
            throw ParserException.Expected<Round.Open>(keyWord);
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

        // the last token in the stream now is '}'
        Instruction[]? elseBlock = null;
        if (stream.Peek() is Else)
        {
            var elseToken = stream.Next() as Else;
            elseBlock = ParseElseBlock(elseToken!, stream);
        }

        return new IfStatement
        {
            Expr = expr,
            Location = keyWord.Location,
            Block = block,
            ElseBlock = elseBlock,
        };

    }
    private Instruction[] ParseElseBlock(Else elseToken, TokenStream stream)
    {
        var nextToken = stream.Next();
        if (nextToken is null)
            throw ParserException.PrematureEndOfInput(elseToken.Location);
        if (nextToken is Curly.Open)
            return ParseFunctionInstructions(stream);
        if (nextToken is If ifToken)
            return new Instruction[] { ParseIfStatement(ifToken, stream) };

        throw ParserException.UnexpectedToken(nextToken);
    }
    private Instruction ParseImport(KeyWord Keyword, TokenStream stream)
    {
        if (stream.Next() is not TextValue value)
            throw new ParserException("An import statement takes an external library path as an argument.",
                stream.Current);
        if (!value.Ty.Equals(Ty.Text))
            throw new ParserException("The path to an external library must be a string value.",
                stream.Current);
        if (stream.Next() is not Semicolon)
            throw new ParserException("Expected semicolon.",
                stream.Current);
        return new ImportStatement
        {
            Path = value.Value,
            Location = Keyword.Location,
        };
    }
    private Instruction ParseDelete(KeyWord keyWord, TokenStream stream)
    {
        if (stream.Next() is not Ident ident)
            throw new ParserException("A delete statement takes a variable name as a parameter.",
                stream.Current);
        if(stream.Next() is not Semicolon)
            throw new ParserException("Expected semicolon.",
                stream.Current);
        return new DeleteStatement
        {
            VariableIdent = ident,
            Location = keyWord.Location,
        };
    }
    private Expression ParseExpression(TokenStream stream)
    {
        Expression? ret = null;
        Operator? currentOperator = null;
        while(stream.Next() is Token token)
        {
            if (token is Curly.Open)
            {
                ret = ParseArrayExpression(stream);
                if (stream.Next() is not null)
                    throw new ParserException(
                        "Array initialisation expression cannot be used with operators or other expressions");
                break;
            }
            SimpleExpression simpleExpression = ParseIdentOrValueExpression(token);
            var nextToken = stream.Next();

            if (nextToken is Round.Open && simpleExpression is IdentExpression identExpression)
            {
                var insideParen = ParseInsideRound(stream);
                simpleExpression = ParseCall(identExpression.Ident, insideParen);
                nextToken = stream.Next();
            }
            if (nextToken is Square.Open/* && simpleExpression is IdentExpression identForIndex*/)
            {
                var (tokens, _) = ReadToToken<Square.Closed>(stream);
                var indexExpression = ParseExpression(tokens.ToTokenStream());
                var indexer = new IndexerExpression
                {
                    ArrayProducer = simpleExpression,
                    IndexExpression = indexExpression,
                    Location = simpleExpression.Location,
                };
                simpleExpression = indexer;
                nextToken = stream.Next();
                while(nextToken is Square.Open)
                {
                    (tokens, _) = ReadToToken<Square.Closed>(stream);
                    indexExpression = ParseExpression(tokens.ToTokenStream());
                    
                    indexer = new IndexerExpression
                    {
                        ArrayProducer = indexer,
                        IndexExpression = indexExpression,
                        Location = simpleExpression.Location,
                    };
                    simpleExpression = indexer;
                    nextToken = stream.Next();
                }
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
            else if (nextToken is not null)
            {
                throw ParserException.UnexpectedToken(nextToken);
            }
        }
        return ret ?? 
            throw new ParserException("Failed to parse an expression");
    }
    private ArrayExpression ParseArrayExpression(TokenStream stream)
    {
        var tokens = ParseBetweenParenWithSeparator<Curly.Open, Curly.Closed, Comma>(stream, 1);
        List<Expression> expressions = new();

        //foreach(var tokenStream in tokens)
        //{
        //    foreach(var token in tokenStream)
        //        Console.WriteLine(token.Stringify());
        //    tokenStream.Reset();
        //}

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

    private List<TokenStream> ParseInsideRound(TokenStream stream)
    {
        return ParseBetweenParenWithSeparator<Round.Open, Round.Closed, Comma>(stream, 1);
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
            return new ValueExpression { Val = value, Location = value.Location };
        else if (token is Ident ident)
            return new IdentExpression { Ident = ident, Location = ident.Location };
        else
            throw new ParserException("Invalid token.", token);
    }


    /// <summary>
    /// Returns a list of all tokens in a stream before the first encounter of <c>Semicolon</c>
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    private List<Token> ReadToSemicolon(INullEnumerator<Token> stream)
    {
        return ReadToToken<Semicolon>(stream).Item1;
    }
    /// <summary>
    /// Reads the stream to the first occurence of <c>T</c> and returns a touple containing
    /// all read tokens and <c>T</c>
    /// </summary>
    /// <remarks>
    /// This function will throw a <c>ParserException</c> if there is no <c>T</c> in the stream.
    /// If this behavior is not desired there is a safe version of this function <c>ReadToTokenOrEnd()</c>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <returns></returns>
    /// <exception cref="ParserException">Thrown if <c>T</c> is not found before the end of the stream</exception>
    private (List<Token>, T) ReadToToken<T>(INullEnumerator<Token> stream)
        where T: Token
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
        throw new ParserException("Premature end of input", location);
    }
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
        //var tokens = ParseBetweenParenWithSeparator<Curly.Open, Curly.Closed, Comma>(stream);
        //Expression[] expressions = new Expression[tokens.Count];
        //var i = 0;
        //foreach (var tokenStream in tokens)
        //{
        //    expressions[i] = ParseExpression(tokenStream);
        //    i++;
        //}

        //var nextToken = stream.Next();
        //if (nextToken is not Semicolon)
        //    throw ParserException.Expected<Semicolon>(nextToken);

        return new ArrayDecl
        {
            ArrayExpression = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
            Name = ident.Value,
            Location = ident.Location
        };
    }
    private Instruction ParseDeclaration(Token token, TokenStream stream)
    {
        if (token is Var var)
        {
            if (stream.Next() is not Ident ident)
                throw new ParserException("Invalid token error", stream.Current);
            if (stream.Next() is not Assign)
                throw new ParserException("Invalid token error", stream.Current);
            if (stream.Peek() is Curly.Open)
                return ParseUntypedArrayDecl(ident, stream);
            return new Variable()
            {
                Name = ident.Value,
                Expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
                Location = ident.Location,
            };
        }
        else if (token is Ty ty)
        {
            if (stream.Next() is not Ident ident)
                throw new ParserException("Invalid token error", stream.Current);
            if (stream.Next() is not Assign)
                throw new ParserException("Invalid token error", stream.Current);

            return new Typed()
            {
                Name = ident.Value,
                Expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
                Type = ty,
                Location = ident.Location,
            };
        }
        else if (token is Fun fun)
        {
            if (stream.Next() is not Ident ident)
                throw new ParserException("Invalid token error", stream.Current);
            //if (stream.Next() is not Round.Open)
            //    throw new ParserException("Invalid token error", stream.Current);
            //parse function args
            var arguments = ParseFunctionArguments(stream);

            var next = stream.Next();
            Ty type = Ty.Void;
            if(next is Ty typeName)
            {
                type = typeName;
                next = stream.Next();
            }
            else if(next is Square.Open)
            {
                var type2 = stream.Next() as Ty ??
                    throw new ParserException("Incorrect function array return type", next);
                type = ArrayTy.ArrayOf(type2);
                next = stream.Next();
                if (next is not Square.Closed)
                    throw new ParserException("Incorrect function array return type", next);
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
                Location = ident.Location,
            };
        }
        else
            throw new ParserException("Invalid syntax error!", token);
    }
    private FunArg[] ParseFunctionArguments(TokenStream stream)
    {
        HashSet<FunArg> args = new HashSet<FunArg>();
        var tokensList = ParseBetweenParenWithSeparator<Round.Open, Round.Closed, Comma>(stream);

        foreach (var tokens in tokensList)
            if (!args.Add(ParseFunctionArgument(tokens)))
                throw new ParserException("Repeating argument names", tokens.Current);
        return args.ToArray();
    }
    private FunArg ParseFunctionArgument(TokenStream stream)
    {
        var @ref = false;
        Ty type;
        var nextToken = stream.Next();
        if (nextToken is Ref)
        {
            @ref = true;
            nextToken = stream.Next();
        }
        if (nextToken is Ty ty)
        {
            type = ty;
            nextToken = stream.Next();
        }
        else if (nextToken is Square.Open)
        {
            if (stream.Next() is not Ty arrayTy)
                throw new ParserException("Failed to parse argument array type", stream.Current);
            if (stream.Next() is not Square.Closed)
                throw new ParserException("Failed to parse argument array, missing closing bracket", 
                    stream.Current);
            type = ArrayTy.ArrayOf(arrayTy);
            nextToken = stream.Next();
        }
        else 
            throw new ParserException("Could not determine function argument type", stream.Current);

        if (nextToken is not Ident ident)
            throw new ParserException("Failed to parse function argument name", stream.Current);
        return new FunArg
        {
            Name = ident,
            Ref = @ref,
            Ty = type,
            Location = ident.Location
        };
    }
    /// <summary>
    /// Parses the contents of a function in <c>{ }</c>
    /// </summary>
    /// <remarks>
    /// THE FIRST <c>Culry.Open</c> MUST BE CONSUMED BEFORE CALLING THIS METHOD!
    /// </remarks>
    /// <param name="stream"></param>
    /// <returns>The block of instructions for that method</returns>
    private Instruction[] ParseFunctionInstructions(TokenStream stream)
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
    /// <summary>
    /// Collects all tokens between the specified <c>Paren</c> types into a new <c>TokenStream</c>.
    /// It ignores commas.
    /// </summary>
    /// <remarks>
    /// Do not advance the stream before calling this method, as opposite to the <c>ReadInsideRound()</c> method
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <returns></returns>
    private TokenStream ReadBetweenParen<TOpen, TClosed>(TokenStream stream)
        where TOpen : Paren
        where TClosed : Paren
    {
        List<Token> tokens = new();
        int openParen = 0;
        while (stream.Next() is Token token)
        {
            if (token is TOpen)
                openParen += 1;
            else if (token is TClosed)
                openParen -= 1;
            else 
                tokens.Add(token);
            if (openParen == 0)
            {
                return tokens.ToTokenStream();
            }
        }
        throw new ParserException("Unclosed parentheses", stream.Current);
    }
    /// <summary>
    /// Collects all tokens between the specified <c>Paren</c> types into a new <c> List of TokenStream</c>
    /// separated by <c>TSeparator</c>
    /// </summary>
    /// <remarks>
    /// Do not advance the stream before calling this method, as opposite to the <c>ReadInsideRound()</c> method
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <returns></returns>
    public List<TokenStream> ParseBetweenParenWithSeparator<TOpen, TClosed, TSeparator>(TokenStream stream,
        int alreadyopen = 0)
        where TOpen : Paren
        where TClosed : Paren
        where TSeparator : Token
    {
        List<TokenStream> streams = new();
        List<Token> tokens = new();
        int openParen = alreadyopen;
        while (stream.Next() is Token token)
        {
            if (token is TOpen) 
            {
                openParen += 1;
                if(openParen > 1)
                    tokens.Add(token);
            }
            else if (token is TClosed)
            {
                openParen -= 1;
                if (openParen >= 1)
                    tokens.Add(token);
            }
            else if (token is TSeparator && openParen == 1)
            {
                streams.Add(tokens.ToTokenStream());
                tokens = new();
            }
            else
                tokens.Add(token);
            if (openParen == 0)
            {
                if (tokens.Count > 0)
                    streams.Add(tokens.ToTokenStream());
                return streams;
            }
        }
        throw new ParserException("Unclosed parentheses", stream.Current);
    }
    


    public IEnumerator<Instruction> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;

    public bool MoveNext() => Parse();

    public void Reset()
    {
        _tokenStream.Reset();
    }
    public void Dispose() { }


    
}


