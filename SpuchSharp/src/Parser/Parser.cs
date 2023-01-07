using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpuchSharp.Instructions;
using SpuchSharp.Lexing;
using SpuchSharp.Tokens;

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
        else 
            return ParseExpressionOrStatement(firstToken);
    }
    private Instruction ParseKeyWordInstruction(KeyWord keyword)
    {
        if(keyword is Fun)
            return ParseDeclaration(keyword);
        else if (keyword is Var)
            return ParseDeclaration(keyword);
        else if (keyword is Delete)
            return ParseDelete(keyword);
        else if (keyword is Import)
            return ParseImport(keyword);
        else
            throw new ParserException("Failed to parse keyword instruction!", keyword);
    }
    private Instruction ParseImport(KeyWord Keyword)
    {
        if (_lexer.Next() is not Value value)
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
            Path = (string)value.Val,
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
    private CallExpression ParseCallExpression(Ident ident) //
    {
        // foo(EXPR, EXPR, EXPR)
        List<Expression> args = new List<Expression>();

        while(_lexer.Next() is Token token)
        {
            if (token is Round.Closed) // no check whether a semicolon is next
                break;
            if (token is Comma)
                continue;
            var expr = ParseExpressionOrStatement(token) as Expression
                ?? throw new ParserException("Failed to parse expression", token);
            args.Add(expr);
            if (_lexer.Current is Round.Closed or Semicolon) break;
            if (_lexer.Current is Comma) continue;
        }
        return new CallExpression
        {
            Args = args.ToArray(),
            Function = ident,
        };
    }
    private Instruction ParseExpressionOrStatement(Token token)
    {
        SimpleExpression simpleExpression;
        if (token is Value value)
        {
            simpleExpression = new ValueExpression { Val = value };
        }
        else if (token is Ident ident)
        {
            simpleExpression = new IdentExpression { Ident = ident };
        }
        else
            throw new ParserException("Invalid syntax", token);

        var nextToken = _lexer.Next();
        if (nextToken is Semicolon or Comma)
            return simpleExpression;
        if(nextToken is Round.Closed)
            nextToken = _lexer.Next();
        if(nextToken is Semicolon)
            return simpleExpression;
        if (nextToken is Assign && simpleExpression is IdentExpression identExpr)
        {
            return new Assignment
            {
                Expr = ParseExpressionOrStatement(_lexer.Next() ?? 
                    throw new ParserException("Premature end of input", nextToken))
                        as Expression ?? throw new ParserException("Failed to parse as expression", 
                                                                                _lexer.Current),
                Left = identExpr.Ident
            };
        }
        else if (nextToken is Round.Open && simpleExpression is IdentExpression identExpr2)
        {
            //return ParseCallExpression(identExpr2.Ident);
            //this is idiotic but it does work
            simpleExpression = ParseCallExpression(identExpr2.Ident);
            if (_lexer.Current is Semicolon) return simpleExpression;
            nextToken = _lexer.Next();
            if (nextToken is Semicolon) return simpleExpression;
        }
        if (nextToken is Operator op)
        {
            Expression complex = simpleExpression;
            while (true)
            {
                var right = ParseRightExpression(_lexer.Next() 
                    ?? throw new ParserException("Failed to parse to expression", _lexer.Current),
                        out var @operator);
                complex = ComplexExpression.From(complex, op, right);
                if(@operator is null)
                    return complex;
                op = @operator;
            }

        }
        else
            throw new ParserException("Syntax error", token);
    }
    private Expression ParseRightExpression(Token token, out Operator? op)
    {
        op = null;
        Expression expr;
        if (token is Value value)
        {
            expr = new ValueExpression { Val = value };
        }
        else if (token is Ident ident)
        {
            expr = new IdentExpression { Ident = ident };
        }
        else
            throw new ParserException("Syntax error", token);

        

        var nextToken = _lexer.Next();
        //if (nextToken is Semicolon or Round.Closed or Comma)
        //    return expr;
        if (nextToken is Semicolon or Comma)
            return expr;
        if (nextToken is Round.Closed)
            nextToken = _lexer.Next();
        if (nextToken is Semicolon)
            return expr;

        else if (nextToken is Round.Open && expr is IdentExpression identExpr2)
        {
            return ParseCallExpression(identExpr2.Ident);
        }
        else if (nextToken is Operator @operator)
        {
            op = @operator;
            return expr;
        }
        else
            throw new ParserException("Syntax error", token);

    }
    private Instruction ParseDeclaration(Token token)
    {
        if (token is Var var)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assign)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Token tok)
                throw new ParserException("Invalid token error", _lexer.Current);
            //if (!IsExpressionOrStatement(tok, out var expression))
            //    throw new ParserException("Invalid token error", tok);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            return new Variable()
            {
                Name = ident.Value,
                Expr = ParseExpressionOrStatement(tok) as Expression ??
                        throw new ParserException("Failed to parse declaration instruction", _lexer.Current),
            };
        }
        else if (token is Ty ty)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assign)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Token tok)
                throw new ParserException("Invalid token error", _lexer.Current);
            //if (!IsExpressionOrStatement(tok, out var expression))
            //    throw new ParserException("Invalid token error", _lexer.Current);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            //if (ty.Equals(val.Ty))
            //    throw new ParserException(
            //        $"Mismatched types, declared type is different than assigned type ({ty} {val.Ty})", val.Ty.Location);
            return new Typed()
            {
                Name = ident.Value,
                Expr = ParseExpressionOrStatement(tok) as Expression ??
                        throw new ParserException("Failed to parse declaration instruction", _lexer.Current),
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

            if (_lexer.Next() is not Curly.Open)
                throw new ParserException("Invalid token error", _lexer.Current);
            var instructions = ParseFunctionInstructions();
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            return new Function()
            {
                Args = arguments,
                Block = instructions,
                Name = ident,
            };
        }
        else
            throw new ParserException("Invalid syntax error!", token);
    }
    private bool IsDeclaration(Token token, out Declaration? declaration)
    {
        declaration = null;
        if(token is Var var)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assign)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Token tok)
                throw new ParserException("Invalid token error", _lexer.Current);
            //if(!IsExpressionOrStatement(tok, out var expression))
            //    throw new ParserException("Invalid token error", tok);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            declaration = new Variable()
            {
                Name = ident.Value,
                Expr = ParseExpressionOrStatement(tok) as Expression ??
                        throw new ParserException("Failed to parse declaration instruction", _lexer.Current),
            };
            return true;
        }
        else if (token is Ty ty)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assign)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Token tok)
                throw new ParserException("Invalid token error", _lexer.Current);
            //if (!IsExpressionOrStatement(tok, out var expression))
            //    throw new ParserException("Invalid token error", _lexer.Current);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            //if (ty.Equals(val.Ty))
            //    throw new ParserException(
            //        $"Mismatched types, declared type is different than assigned type ({ty} {val.Ty})", val.Ty.Location);
            declaration = new Variable()
            {
                Name = ident.Value,
                Expr = ParseExpressionOrStatement(tok) as Expression ??
                        throw new ParserException("Failed to parse declaration instruction", _lexer.Current),
            };
            return true;
        }
        else if (token is Fun fun)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Round.Open)
                throw new ParserException("Invalid token error", _lexer.Current);
            //parse function args
            var arguments = ParseFunctionArguments();

            if (_lexer.Next() is not Curly.Open)
                throw new ParserException("Invalid token error", _lexer.Current);
            var instructions = ParseFunctionInstructions();
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            declaration = new Function()
            {
                Args = arguments,
                Block = instructions,
                Name = ident,
            };
        }
        return false;
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
