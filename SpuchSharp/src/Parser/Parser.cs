using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        Instruction? ret = null;
        foreach(Token token in _lexer)
        {
            if (IsDeclaration(token, out var declaration)) { ret = declaration; /*Console.WriteLine("Found decl");*/ ; }
            if(IsExpressionOrStatement(token, out var expression)) { ret = expression; /*Console.WriteLine("Found expr");*/ }

            if (ret is not null)
            {
                _currentInstruction = ret;
                return true;
            }
            else
                throw new ParserException($"Unrecognized token", token);
        }
        return false;

    }

    private bool IsExpressionOrStatement(Token token, out Instruction? ins)
    {
        ins = null;
        //the left side of a possible ComplexExpr
        Expression left;
        //ValueExpr
        if (token is Value val)
        {
            left = new ValueExpression { Val = val };
        }
        else if (token is Ident ident)
        {
            left = new IdentExpression { Ident = ident };
        }
        else return false;
        var next = _lexer.Next();
        //assign statement!
        if (next is Assign && token is Ident id)
            if (IsExpressionOrStatement(_lexer.Next()
                ?? throw new ParserException("Premature end of input"),
                out var e))
            {
                ins = new Assignment
                {
                    Left = id,
                    Expr = e as Expression ??
                    throw new ParserException("Failed to parse expression in assignment!", _lexer.Current)
                };
                return true;
            }
            else throw new ParserException("Expected an expression in assignment!", _lexer.Current);
        //CallExpr
        else if (next is Round.Open)
        {
            throw new ParserException("Function calls not working atm", next);
            //Console.WriteLine($"Got call! {ident.Stringify()}");
            //List<Expression> args = new();
            //while (true)
            //{
            //    var t = _lexer.Next();
            //    if (t is Round.Closed) { Console.WriteLine("CHUJ"); break; };
            //    if (t is null) 
            //        throw new ParserException("Premature end of input", _lexer.Current);
            //    if (!IsExpression(t, out var e))
            //    //    break;
            //        throw new ParserException("Could not parse to expression", _lexer.Current);
            //    args.Add(e!);
            //}
            //left = new CallExpression { Args = args.ToArray(), Function = ident };
        }
        if (next is Semicolon)
        {
            ins = left;
            return true;
        }
        //next could be an operator at this point
        //so lets try parsing this as a complex expression
        else if(next is Operator op)
        {
            ins = ParseComplexExpression(left, op);
            return true;
        }
        else return false;

    }
    private ComplexExpression ParseComplexExpression(Expression left, Operator op)
    {
        //an operator means that there should be a second expression
        //Console.WriteLine($"With operator {op.Stringify()}");
        if (!IsExpressionOrStatement(_lexer.Next()
            ?? throw new ParserException("Premature end of input", _lexer.Current),
            out var expr)) throw new ParserException("Expected an expression", _lexer.Current);
        return ComplexExpression.From(left, op, expr as Expression ??
                    throw new ParserException("Failed to parse expression", _lexer.Current));
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
            if(!IsExpressionOrStatement(tok, out var expression))
                throw new ParserException("Invalid token error", tok);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            declaration = new Variable()
            {
                Name = ident.Value,
                Expr = expression as Expression ??
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
            if (!IsExpressionOrStatement(tok, out var expression))
                throw new ParserException("Invalid token error", _lexer.Current);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            //if (ty.Equals(val.Ty))
            //    throw new ParserException(
            //        $"Mismatched types, declared type is different than assigned type ({ty} {val.Ty})", val.Ty.Location);
            declaration = new Variable()
            {
                Name = ident.Value,
                Expr = expression as Expression ??
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
            //throw new ParserException("Function declarations are TODO", _lexer.Current);
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
            args.Add(new FunArg()
            {
                Name = ident,
                Ty = type,
                Location = ident.Location,
            });
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
            //if (IsExpressionOrStatement(token, out var expression)) { list.Add(expression!); }
            if (IsDeclaration(token, out var declaration)) { list.Add(declaration!); /*Console.WriteLine("Found decl");*/ ; }
            if (IsExpressionOrStatement(token, out var expression)) { list.Add(expression!); }
            else
                throw new ParserException("Failed to parse instruction", _lexer.Current);
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
