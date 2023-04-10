using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp;
internal struct Optional<T1, T2>
{
    private readonly T1? _leftValue;
    private readonly T2? _rightValue;
    public bool HasLeft { get => _leftValue is not null; }
    public bool HasRight { get => _rightValue is not null; }
    public object Value { get => _leftValue is not null ? _leftValue : _rightValue!; }
    public T1? Left { get => _leftValue; }
    public T2? Right { get => _rightValue; }
    public T1 LeftOrThrow 
    { 
        get => _leftValue ??
            throw new OptionalException($"Tried to access an Optional value of type {typeof(T1)}, but it was null");
    }
    public T2 RightOrThrow
    {
        get => _rightValue ??
            throw new OptionalException($"Tried to access an Optional value of type {typeof(T2)}, but it was null");
    }

    public Optional(T1 t1)
    {
        _leftValue = t1;
    }
    public Optional(T2 t2)
    {
        _rightValue = t2;
    }
    //public void SetLeft(T1 value)
    //{
    //    if (_rightValue is not null)
    //        throw new Exception(
    //            $"Trying to set optional value {nameof(T1)}, but value {nameof(T2)} is already set");
    //    _leftValue = value;
    //}
    public static implicit operator Optional<T1, T2>(T1 t1) => new Optional<T1, T2>(t1);
    public static implicit operator Optional<T1, T2>(T2 t2) => new Optional<T1, T2>(t2);
}

[Serializable]
public class OptionalException : Exception
{
    public OptionalException() { }
    public OptionalException(string message) : base(message) { }
    public OptionalException(string message, Exception inner) : base(message, inner) { }
    protected OptionalException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
