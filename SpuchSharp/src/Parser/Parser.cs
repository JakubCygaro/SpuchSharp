using System;
using System.Collections;
using System.Collections.Generic;
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
            if(IsDeclaration(token, out var declaration)) { ret = declaration; }

            if (ret is not null)
            {
                _currentInstruction = ret;
                return true;
            }
            else
                throw new ParserException($"Unrecognized token {token.Location}");
        }
        return false;

    }

    private bool IsDeclaration(Token token, out Instruction? declaration)
    {
        declaration = null;
        if(token is Var var)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error");
            if (_lexer.Next() is not Assigment ass)
                throw new ParserException("Invalid token error");
            if (_lexer.Next() is not Value val)
                throw new ParserException("Invalid token error");
            if (_lexer.Next() is not Semicolon)
                throw new ParserException("Invalid token error");
            declaration = new Variable()
            {
                Name = ident.Value,
                Value = val,
            };
            return true;
        }
        else if (token is Fun fun)
        {
            if (_lexer.Next() is not Ident ident)
                throw new ParserException("Invalid token error");
            if (_lexer.Next() is not Round)
                throw new ParserException("Invalid token error");
            //parse function args
            //i need the lexer to actually know what the fuck a function looks like


            if (_lexer.Next() is not Curly)
                throw new ParserException("Invalid token error");
            var expressions = ParseFunctionExpressions();
            //if (_lexer.Next() is not Semicolon)
            //    throw new ParserException("Invalid token error");

            throw new ParserException("Function declarations are TODO");
        }
        return false;
    }
    private Expression[] ParseFunctionExpressions()
    {
        List<Expression> list = new List<Expression>();
        while(_lexer.Next() is Token token)
        {

        }


        return list.ToArray();
    }
    private bool IsAssignExpression(Token token, out Expression? assigment)
    {
        assigment = null;
        if (token is not Ident ident)
            throw new ParserException("Failed parsing expression!");
        //if (_lexer.Next())
        return false;
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
