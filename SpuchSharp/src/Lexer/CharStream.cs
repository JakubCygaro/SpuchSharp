using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Lexing;

internal sealed class CharStream : IEnumerator<char>
{
    private char[] _text;
    private int _position = 0;
    public char Current => _text[_position];

    object IEnumerator.Current => Current;

    public CharStream(string input)
    {
        _text = input.ToCharArray();
    }

    public void Dispose()
    {
        
    }

    public bool MoveNext()
    {
        if (_position < _text.Length)
        {
            _position++;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        _position = 0;       
    }
    public char? Next()
    {
        try
        {
            return Current;
        }
        catch 
        {
            return null;
        }
        finally
        {
            MoveNext();
        }
    }
    public char? Peek()
    {
        try
        {
            return _text[_position + 1];
        }
        catch
        {
            return null;
        }
    }
}
