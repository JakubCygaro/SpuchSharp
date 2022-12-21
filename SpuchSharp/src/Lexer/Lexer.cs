using System;
using System.Collections.Generic;
using System.Linq;
using SpuchSharp.Tokens;
using System.IO;
using System.Collections;
using System.Linq.Expressions;

namespace SpuchSharp.Lexing;

public sealed class Lexer : IEnumerable<Token>, IEnumerator<Token>
{
    private CharStream _charStream;

    private Token _currentToken = default!;
    public Token Current => _currentToken;

    object IEnumerator.Current => _currentToken;

    public Lexer(string[] lines)
    {
        _charStream= new CharStream(lines);
    }

    public bool Lex()
    {
        Token? ret = null;
        var literal = string.Empty;
        foreach(char current in _charStream)
        {
            if (current == ' ') { break; }
            if (char.IsWhiteSpace(current)) { continue; }
            literal += current;
            //Console.WriteLine(literal);
            if (IsIdent(literal, out var ident)) { ret = ident; continue; }
            else if (IsSimpleToken(literal, out var simple)) { ret = simple; continue; }
            else if (IsValue(literal, out var value)) { ret = value; continue; }
            else
            {
                _charStream.MoveBack();
                break;
            }
        }
        if (ret is Ident id)
        {
            try
            {
                ret = KeyWord.From(id.Value);
            }
            catch { }
        }


        if (ret is not null)
        {
            _currentToken = ret;
            return true;
        }
        else if (ret is not null && _charStream.EndOfInput())
        {
            _currentToken = ret;
            return true;
        }
        else if (ret is null && _charStream.EndOfInput())
        {
            return false;
        }
        else
        {
            throw new LexerException($"Could not parse to token `{literal}`");
        }
        //Console.WriteLine($"{_charStream.Position} / {_charStream.Length - 1}");
        //if (_charStream.EndOfInput())
        //{
        //    Console.WriteLine("End of input");
        //    return false;
        //}
        //if (ret is null)
        //    throw new LexerException($"Could not parse to token `{literal}`");
        //return true;
    }
    private bool IsIdent(string lit, out Ident? ident)
    {
        ident = null;
        try
        {
            ident = Ident.From(lit);
            return true;
        }
        catch
        {
            return false;
        }
    }
    private bool IsSimpleToken(string lit, out SimpleToken? simple)
    {
        simple = null;
        try
        {
            simple = SimpleToken.From(lit);
            return true;
        }
        catch 
        { 
            return false; 
        }
    }
    private bool IsValue(string lit, out Value? value)
    {
        value = null;
        try
        {
            var ty = Ty.FromValue(lit);
            value = new Value()
            {
                Ty = ty,
                Val = lit,
            };
            return true;
        }
        catch { return false; }
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
