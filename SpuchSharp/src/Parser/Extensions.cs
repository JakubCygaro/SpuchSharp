using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpuchSharp.Parsing;

public static class ListExt
{
    public static T TakeOutAt<T>(this List<T> list, int index)
    {
        var ret = list.ElementAt(index);
        list.RemoveAt(index);
        return ret;
    }
}
