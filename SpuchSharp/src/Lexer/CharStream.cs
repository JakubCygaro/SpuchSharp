using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp;

namespace SpuchSharp.Lexing;

internal sealed class CharStream
{
    private readonly string _source;
    private int _position = -1;
    private int _lastNewLine = -1;

    public int Line { get; private set; } = 1;
    public int Column { get => _position - _lastNewLine; }
    public char Current => _source[_position];

    public string SourceFile { get; }
    const string NO_FILE = "";

    public CharStream(string source, string sourceFile = NO_FILE)
    {
        _source = source;
        SourceFile = sourceFile;
    }
    public void Dispose(){ }

    public bool MoveNext()
    {
        if (_position + 1 < _source.Length)
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
        if (MoveNext())
        {
            if (Current == '\n')
            {
                Line++;
                _lastNewLine = _position;
            }
            
            return Current;
        }
        else
        {
            return null;
        }
    }
    public bool EndOfInput()
    {
        return _position == _source.Length - 1;
    }
    public char? PeekNext() 
    {
        if (_position + 1 < _source.Length)
        {
            return _source[_position + 1];
        }
        return null;
    }
    public int Tell() => _position;
    public void SeekFromStart(int position) => _position = position;
    public string ReadToString(int length)
    {
        var ret = _source.Substring(_position, length + 1);
        _position += length;
        return ret;
    }
    public ReadOnlySpan<char> ReadToSpan(int length)
    {
        var ret = _source.AsSpan(_position, length + 1);
        _position += length;
        return ret;
    }


    //public bool SkipLine()
    //{
    //    if(_currentLine + 1 > LineCount)
    //    {
    //        _currentLine++;
    //        _position = 0;
    //        return true;
    //    }
    //    return false;
    //}
}
