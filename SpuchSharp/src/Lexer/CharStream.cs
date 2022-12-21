using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Lexing;

internal sealed class CharStream : IEnumerator<char>, IEnumerable<char>
{
    private char[][] _lines;


    //private char[] _text;
    private int _position = -1;
    private int _currentLine = 0;
    public int Position { get => _position; }
    //public int Length { get => _text.Length; }
    public int LineCount { get => _lines.Length; }
    public int Line { get => _currentLine; }
    public char Current => _lines[_currentLine][_position];

    object IEnumerator.Current => Current;

    public CharStream(string[] input)
    {
        _lines = input.Where(l => !string.IsNullOrEmpty(l))
            .Select(l => l.Trim())
            .Select(l => l.Normalize())
            .Select(l => l.ToCharArray())
            .ToArray();
        Console.WriteLine($"""
            lines: {LineCount},
            line: {Line}
            position: {Position}
            """);

        //input.Trim();
        //input.Normalize();
        //_text = input.ToCharArray();
    }

    public void Dispose()
    {
        
    }

    public bool MoveNext()
    {
        if (_position + 1 < _lines[_currentLine].Length)
        {
            _position++;
            return true;
        }
        //else if (_currentLine < _lines.Length)
        //{
        //    _currentLine++;
        //    _position = -1;
        //    return true;
        //}
        return false;
    }

    public void Reset()
    {
        _position = 0;       
    }
    public char? Next()
    {
        if (MoveNext())
        {
            return Current;
        }
        else
        {
            return null;
        }
        //try
        //{
        //    return Current;
        //}
        //catch 
        //{
        //    return null;
        //}
        //finally
        //{
        //    MoveNext();
        //}
    }
    public bool MoveBack()
    {
        if(_position -1  > 0)
        {
            _position--;
            return true;
        }
        return false;
    }
    public char? Peek()
    {
        try
        {
            return _lines[_currentLine][_position + 1];
        }
        catch
        {
            return null;
        }
    }
    public bool EndOfInput()
    {
        return _currentLine == _lines.Length - 1 && _position == _lines[_currentLine].Length - 1;
    }

    public IEnumerator<char> GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this;
    }
}
