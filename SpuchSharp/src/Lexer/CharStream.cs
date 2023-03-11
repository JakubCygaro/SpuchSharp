using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpuchSharp;

namespace SpuchSharp.Lexing;

internal sealed class CharStream : INullEnumerator<char>, IEnumerable<char>
{
    private CodeLine[] _lines;


    //private char[] _text;
    private int _position = -1;
    private int _currentLine = 0;
    public int Column { get => _position; }
    //public int Length { get => _text.Length; }
    public int LineCount { get => _lines.Length; }
    public int LineNumber { get => _lines[_currentLine].LineNumber; }
    public int LineLength { get => _lines[_currentLine].Length; }
    public char Current => _lines[_currentLine][_position];

    object IEnumerator.Current => Current;

    public CharStream(string[] input)
    {
        //_lines = input.Where(l => !string.IsNullOrEmpty(l))
        //    .Where(l => !l.StartsWith("#"))
        //    .Select(l => l.Trim())
        //    .Select(l => l.Normalize())
        //    //.Select(l => l + " ")
        //    .Select(l => l.ToCharArray())
        //    .ToArray();
        var normalized = input
            .Select(line => line.Trim())
            .Select((line, num) => new { Line = CutComment(line), Number = num + 1 })
            .Where(l => !string.IsNullOrEmpty(l.Line))
            .Select(l => new CodeLine { Characters = l.Line.ToCharArray(), LineNumber = l.Number })
            .ToArray();
        _lines = normalized;

    }
    static string CutComment(string input)
    {
        var index = input.IndexOf('#');
        if (index == -1) return input;
        return input.Substring(0, index);
    }
    public void Dispose(){ }

    public bool MoveNext()
    {
        if (_position + 1 < _lines[_currentLine].Length)
        {
            _position++;
            return true;
        }
        else if (_currentLine + 1 < _lines.Length)
        {
            _currentLine++;
            _position = 0;
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
            return Current;
        }
        else
        {
            return null;
        }
    }
    public bool MoveBack()
    {
        if(_position - 1 >= 0)
        {
            _position--;
            return true;
        }
        else if(_currentLine - 1 >= 0)
        {
            _currentLine--;
            _position = _lines[_currentLine].Length - 1;
            return true;
        }
        return false;
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
struct CodeLine
{
    public int LineNumber { get; init; }
    public char[] Characters { get; init; }

    public char this[int index] { get { return Characters[index]; } }
    public int Length { get { return Characters.Length;} }
}