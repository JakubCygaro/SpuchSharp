using System;
using System.Collections;
using SpuchSharp.Instructions;
using SpuchSharp.Tokens;
using SpuchSharp;
using VariableScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SVariable>;
using FunctionScope =
    System.Collections.Generic.Dictionary<SpuchSharp.Tokens.Ident, SpuchSharp.Interpreting.SFunction>;
using System.Data;

namespace SpuchSharp.Parsing;

internal sealed class Parser : IEnumerable<Instruction>, IEnumerator<Instruction>
{
    public Instruction Current => _currentInstruction;
    object IEnumerator.Current => _currentInstruction;

    private Instruction _currentInstruction = default!;
    private readonly TokenStream _tokenStream;
    public Parser(TokenStream tokenStream) 
    {
        _tokenStream = tokenStream;
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
        //if (firstToken is Square.Open squareOpen)
        //    return ParseArrayDeclaration(squareOpen, stream);

        var remainder = new List<Token> { firstToken };
        remainder.AddRange(ReadToSemicolon(stream));

        return ParseExpressionOrAssignment(remainder.ToTokenStream());
    }
    private Instruction ParseExpressionOrAssignment(TokenStream stream)
    {
        if (stream.Has<AssignToken>())
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
        (var unparsedTarget, var assignToken) = ReadToToken<AssignToken>(stream);
        var expression = ParseExpression(stream);
        var target = ParseExpression(unparsedTarget.ToTokenStream());

        AssignTarget assignTarget = target switch
        {
            IdentExpression ie => new IdentTarget
                {
                    Target = ie,
                },
            IndexerExpression ix => new ArrayIndexTarget
                {
                    Target = ix,
                    IndexExpression = ix.IndexExpression,
                },
            _ => throw new AssignmentTargetException(target.Location)
        };

        return assignToken switch
        {
            Assign => new RegularAssignment 
            { 
                Left = assignTarget,
                Expr = expression,
                Location = expression.Location,
            },
            AssignAdd => new AddAssignment
            {
                Left = assignTarget,
                Expr = expression,
                Location = expression.Location,
            },
            AssignSub => new SubAssignment
            {
                Left = assignTarget,
                Expr = expression,
                Location = expression.Location,
            },
            AssignMul => new MulAssignment
            {
                Left = assignTarget,
                Expr = expression,
                Location = expression.Location,
            },
            AssignDiv => new DivAssignment
            {
                Left = assignTarget,
                Expr = expression,
                Location = expression.Location,
            },
            AssignModulo => new ModuloAssignment
            {
                Left = assignTarget,
                Expr = expression,
                Location = expression.Location,
            },
            _ => throw new UnknownAssignmentException(assignToken.Location) 
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
            Mod mod => ParseMod(mod, stream),
            Use use => ParseUse(use, stream),
            Public => ParseWithPublic(stream.Next() ?? 
                    throw new PrematureEndOfInputException(stream.Current.Location), 
                    stream),
            Const @const => ParseWithConst(@const, stream),
            _ => throw new KeywordException(keyword.Location),
        };
    }
    private Instruction ParseWithConst(Const constKeyword, TokenStream stream, bool pub = false)
    {
        var next = stream.Next();
        return next switch
        {
            Var var => ParseDeclaration(var, stream, pub: pub, @const: true),
            Ty ty => ParseDeclaration(ty, stream, pub: pub, @const: true),
            //Square.Open sq => ParseArrayDeclaration(sq, stream, pub: pub, @const: true),
            _ => throw ParserException.PrematureEndOfInput(),
        };
    }
    private Instruction ParseWithPublic(Token keyword, TokenStream stream, bool @const = false)
    {
        return keyword switch
        {
            Fun or Var or Ty => ParseDeclaration(keyword, stream, pub: true),
            //Square.Open sq => ParseArrayDeclaration(sq, stream, pub: true),
            Mod m => ParseMod(m, stream, pub: true),
            Const c => ParseWithConst(c, stream, pub: true),
            _ => throw new DisallowedPubUsageException(stream.Current.Location),
        };
    }
    private Instruction ParseMod(Mod modKeyword, TokenStream stream, bool pub = false)
    {
        if (stream.Next() is not Ident ident)
            throw ParserException.Expected<Ident>(stream.Current);
        if (stream.Next() is not Semicolon)
            throw ParserException.Expected<Semicolon>(stream.Current);
        return new ModuleDecl
        {
            Ident = ident,
            Location = modKeyword.Location,
            IsPublic = pub,
        };
    }
    private Instruction ParseUse(Use useKeyword, TokenStream stream)
    {
        var tokens = ReadToSemicolon(stream).ToTokenStream();
        List<Ident> idents = new();
        while(tokens.Next() is Token token)
        {
            if (token is not Ident ident)
                throw ParserException.Expected<Ident>(token);

            idents.Add(ident);

            var next = tokens.Next();
            if (next is Colon2)
                continue;
            else if (next is null)
                break;
            else
                throw ParserException.UnexpectedToken(next);
        }
        return new UseStmt
        {
            ModulePath = idents.ToArray(),
            Location = useKeyword.Location,
        };
    }
    private Ty ParseType(Ty firstToken, TokenStream stream)
    {
        //if (firstToken is Ty normalTy)
        //    return normalTy;
        //else if (firstToken is Square.Open)
        //{
        //    var arrayTy = ParseType(stream);
        //    if (stream.Next() is not Square.Closed)
        //        throw ParserException.Expected<Square.Closed>(stream.Current);
        //    return ArrayTy.ArrayOf(arrayTy);
        //}
        //else
        //    throw new ParserException("Failed to parse to type", stream.Current);

        //if (firstToken is not Ty asTy)
        //    throw new ParserException("Failed to parse to type", stream.Current);
        Ty ret = firstToken;
        while(stream.Peek() is Square.Open)
        {
            stream.MoveNext();
            if (stream.Next() is not Square.Closed)
                throw new FailedToParseToTypeException(stream.Current.Location);
            ret = ArrayTy.ArrayOf(ret);
        }
        return ret;
    }
    private Declaration ParseArrayDeclaration(ArrayTy arrayTy,
        TokenStream stream,
        bool pub = false, bool @const = false)
    {

        var nextToken = stream.Next();
        if (nextToken is not Ident ident)
            throw ParserException.Expected<Ident>(nextToken);

        nextToken = stream.Next();
        if (nextToken is not Assign)
        {
            if (nextToken is Semicolon)
                return new TypedArrayDecl
                {
                    Type = arrayTy,
                    ArrayExpression = null,
                    Name = ident.Value,
                    Sized = null,
                    Location = ident.Location,
                    IsPublic = pub,
                    Const = @const,
                };
            else
                throw ParserException.Expected<Assign>(nextToken);
        }
        if (stream.Peek() is Square.Open)
        {
            stream.Next();
            var nestedArrayTy = arrayTy;
            List<Expression> sizes = new();
            var size = ParseExpression(ReadToToken<Square.Closed>(stream).Item1.ToTokenStream());
            sizes.Add(size);
            while (nestedArrayTy.OfType is ArrayTy arrayType)
            {
                nextToken = stream.Next();
                if (nextToken is not Square.Open)
                    throw ParserException.Expected<Square.Open>(nextToken);
                size = ParseExpression(ReadToToken<Square.Closed>(stream).Item1.ToTokenStream());
                sizes.Add(size);
                nestedArrayTy = arrayType;
            }
            if (stream.Next() is not Semicolon)
                throw ParserException.Expected<Semicolon>(stream.Current);
            return new TypedArrayDecl
            {
                Type = arrayTy,
                Name = ident.Value,
                ArrayExpression = ArrayExpression.Empty,
                Sized = sizes,
                Location = ident.Location,
                IsPublic = pub,
                Const = @const,
            };
        }

        return new TypedArrayDecl
        {
            Type = arrayTy,
            ArrayExpression = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
            Name = ident.Value,
            Sized = null,
            Location = ident.Location,
            IsPublic = pub,
            Const = @const,
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
                    new ConstantExpression { Val = Value.Void, Location = null, },
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
            throw new IfClauseSyntaxException("An if statement clause must be only a single logical expression",
                stream.Current.Location);

        var expressionStream = expressions[0];
        var expr = ParseExpression(expressionStream);

        if (stream.Next() is not Curly.Open)
            throw new IfClauseSyntaxException("An if statement requires a block of instructions in curly braces",
                stream.Current.Location);

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
            throw new ImportStatementSyntaxException("An import statement takes an external library path as an argument.",
                stream.Current.Location);
        if (!value.Ty.Equals(Ty.Text))
            throw new ImportStatementSyntaxException("The path to an external library must be a string value.",
                stream.Current.Location);
        if (stream.Next() is not Semicolon)
            throw ParserException.Expected<Semicolon>(stream.Current);
        return new ImportStatement
        {
            Path = value.Value,
            Location = Keyword.Location,
        };
    }
    private Instruction ParseDelete(KeyWord keyWord, TokenStream stream)
    {
        if (stream.Next() is not Ident ident)
            throw new DeleteStatementSyntaxException("A delete statement takes a variable name as a parameter.",
                stream.Current.Location);
        if(stream.Next() is not Semicolon)
            throw ParserException.Expected<Semicolon>(stream.Current);
        return new DeleteStatement
        {
            VariableIdent = ident,
            Location = keyWord.Location,
        };
    }
    private Expression ParseExpression(TokenStream stream)
    {
        var parsing = Transform(stream);
        while (parsing.Count > 1)
        {
            int index = 0;
            short highestPrecedence = short.MinValue;
            for (int i = 0; i < parsing.Count; i++)
            {
                if (parsing[i].Value is Operator op)
                {
                    if (op.Precedence > highestPrecedence)
                    {
                        highestPrecedence = op.Precedence;
                        index = i;
                    }
                }
            }
            var reff = parsing[index].Value;
            try
            {
                HandleOperator(index, parsing[index].Left!, parsing);
            }
            catch
            {
                throw new FailedToParseExpressionException((reff as Token)?.Location);
            }
        }
        return parsing[0].Right ?? 
            throw new FailedToParseExpressionException(default(Location));

    }
    private void HandleOperator(int index, Operator op, List<Optional<Operator, Expression>> parsing)
    {
        Expression v1;
        Expression v2;
        switch (op) 
        {
            case Add or Sub or Div or Mult or 
                    Equality or InEquality or
                    And or Or or 
                    Greater or GreaterOrEq or
                    Less or LessOrEq or
                    Percent:
                v1 = parsing.TakeOutAt(index - 1).RightOrThrow;
                v2 = parsing.TakeOutAt(index).RightOrThrow;
                parsing[index - 1] = ComplexExpression.From(v1, op, v2);
                break;

            case Exclam:
                v1 = parsing.TakeOutAt(index + 1).RightOrThrow;
                parsing[index] = new NotExpression { Expr = v1, Location = op.Location };
                break;

            case Add2:
                if(parsing.ElementAtOrDefault(index - 1).HasRight && 
                    parsing.ElementAtOrDefault(index + 1).HasRight)
                    throw new OperatorException("Invalid usage of `++` operator", op.Location);
                if(parsing.ElementAtOrDefault(index - 1).HasRight)
                {
                    v1 = parsing.TakeOutAt(index - 1).RightOrThrow; //take expr
                    parsing[index - 1] = new IncrementExpression
                    {
                        Expression = v1 as IdentExpression ??
                            throw new OperatorException("Increment operator can only target variable names", 
                            op.Location),
                        Location = op.Location,
                        Pre = false,
                    };
                }
                else
                {
                    v1 = parsing.TakeOutAt(index + 1).RightOrThrow;
                    parsing[index] = new IncrementExpression
                    {
                        Expression = v1 as IdentExpression ??
                            throw new OperatorException("Increment operator can only target variable names",
                            op.Location),
                        Location = op.Location,
                        Pre = true,
                    };
                }
                break;

            case Sub2:
                if (parsing.ElementAtOrDefault(index - 1).HasRight &&
                    parsing.ElementAtOrDefault(index + 1).HasRight)
                    throw new OperatorException("Invalid usage of `--` operator", op.Location);
                if (parsing.ElementAtOrDefault(index - 1).HasRight)
                {
                    v1 = parsing.TakeOutAt(index - 1).RightOrThrow; 
                    parsing[index - 1] = new DecrementExpression
                    {
                        Expression = v1 as IdentExpression ??
                            throw new OperatorException("Decrement operator can only target variable names",
                            op.Location),
                        Location = op.Location,
                        Pre = false,
                    };
                }
                else
                {
                    v1 = parsing.TakeOutAt(index + 1).RightOrThrow;
                    parsing[index] = new DecrementExpression
                    {
                        Expression = v1 as IdentExpression ??
                            throw new OperatorException("Decrement operator can only target variable names",
                            op.Location),
                        Location = op.Location,
                        Pre = true,
                    };
                }
                break;

            case CastOperator cast:
                v1 = parsing.TakeOutAt(index + 1).RightOrThrow;
                parsing[index] = new CastExpression
                {
                    Expression = v1,
                    TargetType = cast.TargetType,
                    Location = op.Location,
                };
                break;

            default:
                throw new FailedToParseExpressionException($"Unable to parse expression with operator {op.Stringify()}", 
                    op.Location);
        }
    }

    /// <summary>
    /// Takes a token stream and transforms it into a list of operators and expressions, that can be further
    /// parsed into a singular expression
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    private List<Optional<Operator, Expression>> Transform(TokenStream stream)
    {
        List<Optional<Operator, Expression>> ret = new();

        while (stream.Next() is Token token)
        {
            if(token is Round.Open)
            {
                if(stream.Peek() is Ty typename)
                {
                    stream.Next();
                    if (stream.Next() is not Round.Closed)
                        throw ParserException.Expected<Round.Closed>(stream.Current);
                    ret.Add(new CastOperator
                    {
                        TargetType = typename,
                    });
                    continue;
                }
                var tokens = ReadToToken<Round.Closed>(stream).Item1.ToTokenStream();
                ret.Add(ParseExpression(tokens));
                continue;
            }
            if (token is Operator op)
            {
                ret.Add(op);
                continue;
            }

            if (token is Curly.Open)
            {
                ret.Add(ParseArrayExpression(stream));
                if (stream.Next() is not null)
                    throw new ArrayException(
                        "Array initialisation expression cannot be used with operators or other expressions", 
                        stream.Current.Location);
                break;
            }
            SimpleExpression simpleExpression = ParseIdentOrValueExpression(token);
            var nextToken = stream.Next();

            if (nextToken is Round.Open && simpleExpression is IdentExpression identExpression)
            {
                var insideParen = ParseInsideRound(stream);
                simpleExpression = ParseCall(identExpression.Ident, insideParen);
                //nextToken = stream.Next();
                ret.Add(simpleExpression);
                continue;
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
                nextToken = stream.Peek();
                while (nextToken is Square.Open)
                {
                    stream.Next();
                    (tokens, _) = ReadToToken<Square.Closed>(stream);
                    indexExpression = ParseExpression(tokens.ToTokenStream());

                    indexer = new IndexerExpression
                    {
                        ArrayProducer = indexer,
                        IndexExpression = indexExpression,
                        Location = simpleExpression.Location,
                    };
                    simpleExpression = indexer;
                    nextToken = stream.Peek();
                }
                ret.Add(simpleExpression);
                continue;
            }
            if (nextToken is Operator nextOperator)
            {
                ret.Add(simpleExpression);
                ret.Add(nextOperator);
                continue;
            }
            else if (nextToken is not null)
            {
                throw ParserException.UnexpectedToken(nextToken);
            }
            ret.Add(simpleExpression);

        }
        return ret;

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
            return new ConstantExpression { Val = value, Location = value.Location };
        else if (token is Ident ident)
            return new IdentExpression { Ident = ident, Location = ident.Location };
        else
            throw new UnexpectedTokenException(token);
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
        throw new PrematureEndOfInputException(location);
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
    private Declaration ParseUntypedArrayDecl(Ident ident, TokenStream stream,
        bool pub = false, bool @const = false)
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
            Location = ident.Location,
            Const = @const,
            IsPublic = pub,
        };
    }
    private Instruction ParseDeclaration(Token token, TokenStream stream, 
        bool pub = false, bool @const = false)
    {
        if (token is Var var)
        {
            if (stream.Next() is not Ident ident)
                throw new UnexpectedTokenException(stream.Current);
            if (stream.Next() is not Assign)
                throw new UnexpectedTokenException(stream.Current);
            if (stream.Peek() is Curly.Open)
                return ParseUntypedArrayDecl(ident, stream, pub: pub, @const: @const);

            return new VariableDecl()
            {
                Name = ident.Value,
                Expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream()),
                Location = ident.Location,
                Const = @const,
                IsPublic = pub,
            };
        }
        else if (token is Ty ty)
        {
            Expression? expr = null;

            var type = ParseType(ty, stream);

            if(type is ArrayTy arrayTy)
            {
                var ret = ParseArrayDeclaration(arrayTy, stream, pub: pub, @const: @const);
                return ret;
            }

            if (stream.Next() is not Ident ident)
                throw new UnexpectedTokenException(stream.Current);

            if (stream.Next() is not Assign)
            {
                if (stream.Current is not Semicolon)
                    throw new UnexpectedTokenException(stream.Current);
            }
            else
                expr = ParseExpression(ReadToSemicolon(stream).ToTokenStream());


            return new TypedVariableDecl()
            {
                Name = ident.Value,
                Expr = expr,
                Type = type,
                Location = ident.Location,
                Const = @const,
                IsPublic = pub,
            };
        }
        else if (token is Fun fun)
        {
            if (stream.Next() is not Ident ident)
                throw new UnexpectedTokenException(stream.Current);
            //if (stream.Next() is not Round.Open)
            //    throw new ParserException("Invalid token error", stream.Current);
            //parse function args
            var arguments = ParseFunctionArguments(stream);

            Ty type = Ty.Void;

            var next = stream.Next();

            if(next is Ty typeName)
            {
                type = ParseType(typeName, stream);
                next = stream.Next();
            }
            //else if(next is Square.Open)
            //{
            //    var type2 = stream.Next() as Ty ??
            //        throw new ParserException("Incorrect function array return type", next);
            //    type = ArrayTy.ArrayOf(type2);
            //    next = stream.Next();
            //    if (next is not Square.Closed)
            //        throw new ParserException("Incorrect function array return type", next);
            //    next = stream.Next();
            //}
            if (next is not Curly.Open)
                throw new UnexpectedTokenException(stream.Current);
            var instructions = ParseFunctionInstructions(stream);

            return new FunctionDecl()
            {
                Args = arguments,
                Block = instructions,
                Name = ident,
                ReturnTy = type,
                Location = ident.Location,
                IsPublic = pub
            };
        }
        else
            throw new ParserException("Invalid syntax", token);
    }
    private FunArg[] ParseFunctionArguments(TokenStream stream)
    {
        HashSet<FunArg> args = new HashSet<FunArg>();
        var tokensList = ParseBetweenParenWithSeparator<Round.Open, Round.Closed, Comma>(stream);

        foreach (var tokens in tokensList)
            if (!args.Add(ParseFunctionArgument(tokens)))
                throw new FunctionDeclarationException("Repeating argument names", tokens.Current.Location);
        return args.ToArray();
    }
    private FunArg ParseFunctionArgument(TokenStream stream)
    {
        var @ref = false;
        var @const = false;
        Ty type;

        var nextToken = stream.Next();
        if(nextToken is Const)
        {
            @const = true;
            nextToken = stream.Next();
        }
        if (nextToken is Ref)
        {
            @ref = true;
            nextToken = stream.Next();
        }
        if (nextToken is Ty ty)
        {
            type = ParseType(ty, stream);
            nextToken = stream.Next();
        }
        else 
            throw new FunctionDeclarationException("Could not determine function argument type", 
                stream.Current.Location);

        if (nextToken is not Ident ident)
            throw new FunctionDeclarationException("Failed to parse function argument name", 
                stream.Current.Location);
        return new FunArg
        {
            Name = ident,
            Ref = @ref,
            Ty = type,
            Location = ident.Location,
            Const = @const,
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
    private List<TokenStream> ParseBetweenParenWithSeparator<TOpen, TClosed, TSeparator>(TokenStream stream,
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


