using System;
using System.Collections.Generic;
using System.Linq;
using SpuchSharp.Tokens;
using System.IO;
using System.Collections;

namespace SpuchSharp.Lexing;

public sealed class Lexer : IEnumerable<Token>, IEnumerator<Token>
{
    private CharStream _charStream;

    private Token _currentToken = default!;
    public Token Current => _currentToken;

    object IEnumerator.Current => _currentToken;

    public Lexer(string text)
    {
        _charStream= new CharStream(text);
    }

    public bool Lex()
    {
        Token? ret = null;
        var literal = string.Empty;
        while(_charStream.Next() is char current)
        {
            if (current == ' ') { continue; }
            literal += current;
            Console.WriteLine(literal);
            //if (IsKeyword(literal, out var keyWord)) { ret = keyWord; break; }
            if (IsSimpleToken(literal, out var simpleToken)) { ret = simpleToken; break; }
            if (IsValue(literal, out var valueWord)) { ret = valueWord; break; }
            if (IsIdent(literal, out var identWord)) { ret = identWord; break; }
        }



        if (ret is null)
            throw new LexerException("Could not parse to token");
        _currentToken = ret;
        return true;
    }
    private bool IsKeyword(string lit, out KeyWord? keyWord)
    {
        switch (lit)
        {
            case "var":
                keyWord = new Var();
                return true;
            case "fun":
                keyWord= new Fun();
                return true;
            default:
                keyWord = null;
                return false;
        }
    }

    private bool IsIdent(string lit, out Ident? ident)
    {
        if (lit == ";")
        {
            ident = null;
            return false;
        }
        ident = new Ident() { Value = lit };
        return true;
    }
    private bool IsSimpleToken(string lit, out SimpleToken? simple)
    {
        try
        {
            simple = SimpleToken.From(lit); 
            return true;
        }
        catch 
        {
            simple = null;
            return false; 
        }
    }
    public bool IsValue(string lit, out Value? value)
    {
        try
        {
            var type = Ty.FromValue(lit);
            Console.WriteLine("Chuj");
            while (_charStream.Peek() is char next)
            {
                Console.WriteLine("Ciba");
                var possible = lit + next;
                if (Ty.FromValue(possible) is Ty newType)
                {
                    lit += next;
                    type = newType;
                    _charStream.MoveNext();
                }
                else
                {
                    value = new Value()
                    {
                        Ty = type,
                        Val = possible,
                    };
                    return true;
                }
            }
            value = new Value()
            {
                Ty = type,
                Val = lit,
            };
            return true;
        }
        catch 
        {
            Console.WriteLine("Wagena");
            value = null;
            return false; 
        }
    }
    public IEnumerator<Token> GetEnumerator()
    {
        return this;
    }

    public bool MoveNext()
    {
        return Lex();
    }

    public void Reset()
    {
        _charStream.Reset();
    }

    public void Dispose()
    {
        _charStream.Dispose();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
