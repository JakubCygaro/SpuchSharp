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
            if (IsDeclaration(token, out var declaration)) { ret = declaration; Console.WriteLine("Found decl"); ; }
            if(IsExpression(token, out var expression)) { ret = expression; Console.WriteLine("Found expr"); }

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

    private bool IsExpression(Token token, out Expression? expr)
    {
        expr = null;
        //the left side of a possible ComplexExpr
        SimpleExpression? left;
        //ValueExpr
        if (token is Value val)
        {
            Console.WriteLine($"Got value! {val.Stringify()}");
            left = new ValueExpression { Val = val };
            if (IsComplexExpression(left, out var complex)) { expr = complex; return true; }
            else { expr = left; return true; }
        }
        //IdentExpr or CallExpr
        if (token is Ident ident)
        {
            var next = _lexer.Next();
            //CallExpr
            if(next is Round.Open)
            {
                throw new ParserException("Function calls not working atm", ident);
                Console.WriteLine($"Got call! {ident.Stringify()}");
                List<Expression> args = new();
                while (true)
                {
                    var t = _lexer.Next();
                    if (t is Round.Closed) { Console.WriteLine("CHUJ"); break; };
                    if (t is null) 
                        throw new ParserException("Premature end of input", _lexer.Current);
                    if (!IsExpression(t, out var e))
                    //    break;
                        throw new ParserException("Could not parse to expression", _lexer.Current);
                    args.Add(e!);
                }
                left = new CallExpression { Args = args.ToArray(), Function = ident };
            }
            //IdentExpr
            else
            {
                Console.WriteLine($"Got ident! {ident.Stringify()}");
                left = new IdentExpression { Ident = ident, };
            }
            //Check for complexExpr
            if(next is Comma || next is Round.Closed) { expr = left; return true; }
            else if(IsComplexExpression(left, out var complex)) { expr = complex; return true; }
            else { expr = left; return true; }
        }
        return false;

    }
    private bool IsComplexExpression(SimpleExpression left, out ComplexExpression? complex)
    {
        complex = null;
        var next = _lexer.Next() ?? throw new ParserException("Premature end of input", _lexer.Current);
        //semicolon means that the expression has ended and cannot be complex
        if (next is Semicolon) return false;
        if (next is Comma) return false;
        if (next is Round.Closed) return false;
        //an operator means that there should be a second expression
        else if (next is Operator op)
        {
            Console.WriteLine($"With operator {op.Stringify()}");
            if (!IsExpression(_lexer.Next()
                ?? throw new ParserException("Premature end of input", _lexer.Current),
                out var expr)) throw new ParserException("Expected an expression", _lexer.Current);
            complex = new ComplexExpression
            {
                Left = left,
                Op = op,
                Expr = expr!,
            };
            return true;
        }
        return false;
    }

    private bool IsDeclaration(Token token, out Declaration? declaration)
    {
        declaration = null;
        if(token is Var var)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assigment)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Token tok)
                throw new ParserException("Invalid token error", _lexer.Current);
            if(!IsExpression(tok, out var expression))
                throw new ParserException("Invalid token error", tok);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            declaration = new Variable()
            {
                Name = ident.Value,
                Expr = expression!,
            };
            return true;
        }
        else if (token is Ty ty)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Assigment)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (_lexer.Next() is not Token tok)
                throw new ParserException("Invalid token error", _lexer.Current);
            if (!IsExpression(tok, out var expression))
                throw new ParserException("Invalid token error", _lexer.Current);
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");
            //if (ty.Equals(val.Ty))
            //    throw new ParserException(
            //        $"Mismatched types, declared type is different than assigned type ({ty} {val.Ty})", val.Ty.Location);
            declaration = new Variable()
            {
                Name = ident.Value,
                Expr = expression!,
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
            throw new ParserException("Function declarations are TODO", _lexer.Current);
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
            if (token is not Ty type)
                throw new ParserException("Invalid function argument declaration", _lexer.Current);
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
            if (IsExpression(token, out var expression)) { list.Add(expression!); }
            else
                throw new ParserException("Failed to parse expression", _lexer.Current);
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
