using System;
using System.Collections.Generic;
using System.Linq;
using SpuchSharp.Tokens;
using System.IO;
using System.Collections;
using System.Linq.Expressions;
using System.Diagnostics;

namespace SpuchSharp.Lexing;

internal sealed class Lexer : IEnumerable<Token>, IEnumerator<Token>
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
            if (char.IsWhiteSpace(current)) { break; }
            literal += current;
            if (IsIdent(literal, out var ident)) { ret = ident; continue; }
            else if (IsText(literal, out var text)) { ret = text; continue; }
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
            try
            {
                ret = Ty.From(id.Value);
            }
            catch { }
            try 
            {
                if(IsValue(id.Value, out var val))
                    ret = val;
            }
            catch { }
        }

        if (ret is not null)
        {
            ret.Location = new()
            {
                Line = _charStream.Line + 1,
                Column = _charStream.Position + 1,
            };
            _currentToken = ret;
            return true;
        }
        else if (ret is not null && _charStream.EndOfInput())
        {
            ret.Location = new()
            {
                Line = _charStream.Line + 1,
                Column = _charStream.Position + 1,
            };
            _currentToken = ret;
            return true;
        }
        else if (ret is null && _charStream.EndOfInput())
        {
            return false;
        }
        else if (string.IsNullOrEmpty(literal))
        {
            return false;
        }
        else
        {
            _charStream.MoveNext();
            throw new LexerException(
                $"What the fuck is this `{_charStream.Current}` ?", 
                _charStream.Line + 1, _charStream.Position + 1);
        }
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
            value = Value.From(ty, lit);
            return true;
        }
        catch { return false; }
    }
    private bool IsText(string lit, out Value? text)
    {
        text = null;
        if (lit != "\"") return false;
        var literal = lit;
        var content = string.Empty;
        while(_charStream.Next() is char c)
        {
            if(c == '"')
            {
                literal += c;
                break;
            }
            content += c;
        }
        if (!literal.EndsWith('"'))
            throw new LexerException(
                $"Unterminated string", _charStream.Line + 1, _charStream.Position + 1);

        var type = Ty.FromValue(literal);
        text = Value.From(type, content);
        return true;
    }

    public Token? Next()
    {
        if (MoveNext())
        {
            PrintToken(Current);
            return Current;
        }
        else
        {
            return null;
        }
    }
    //[Conditional("LEXER_DEBUG")]
    [Conditional("DEBUG")]
    void PrintToken(Token token) => Console.WriteLine(token);


    public IEnumerator<Token> GetEnumerator() => this;


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
