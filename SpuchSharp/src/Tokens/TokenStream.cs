using SpuchSharp.Lexing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Tokens;

public class TokenStream : INullEnumerator<Token>, 
    ICloneable<TokenStream>, 
    ICloneable<INullEnumerator<Token>>,
    IPeakable<Token>
{
    private int _position = -1;
    private readonly List<Token> _stream;

    internal TokenStream(List<Token> tokens) => _stream = tokens;

    public Token Current => _stream[_position];
    object IEnumerator.Current => Current;

    public void Reset()
    {
        _position = -1;
    }
    public bool MoveNext()
    {
        if(_position + 1 >= _stream.Count)
            return false;
        _position++;
        return true;
    }
    public Token? Peek()
    {
        if(_position + 1 >= _stream.Count())
        {
            return null;
        } 
        else
        {
            return _stream[_position + 1];
        }
    }
    public Token? Next()
    {
        if (MoveNext())
        {
            return Current;
        }
        else
        {
            return null;
        }
    }

    public void Dispose()
    { }

    public IEnumerator<Token> GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this;
    }

    public TokenStream Clone()
    {
        return new TokenStream(new List<Token>(_stream));
    }
    INullEnumerator<Token>  ICloneable<INullEnumerator<Token>>.Clone()
    {
        return this.Clone();
    }
    public static TokenStream ParseFromQuote(string[] quote)
    {
        return Lexer.Tokenize(quote);
    }
    public bool Has<T>()
        where T: Token
    {
        var ret =  _stream.Find(t => t is T) is not null;
        this.Reset();
        return ret;
    }
    public void Print()
    {
        foreach(var token in _stream)
            Console.WriteLine(token.Stringify());
        Reset();
    }
}

internal static class TokenStreamExt
{
    public static TokenStream ToTokenStream(this List<Token> list)
    {
        return new TokenStream(list);
    }
}
