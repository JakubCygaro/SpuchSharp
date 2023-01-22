using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp;

internal interface INullEnumerator<T> : IEnumerator<T>, IEnumerable<T>
    //where T:
{
    /// <summary>
    /// Returns null if the end of the enumerator has been reached
    /// </summary>
    /// <returns></returns>
    T? Next()
    {
        if (MoveNext())
        {
            return Current;
        }
        else
        {
            return default(T?);
        }
    }
}
