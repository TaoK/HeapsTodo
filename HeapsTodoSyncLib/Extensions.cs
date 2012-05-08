using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeapsTodoSyncLib
{
    public static class Extensions
    {
        public static int FindIndex<T>(this IList<T> source, Func<T, bool> condition)
        {
            //Reimplementation of FindIndex for IList, because for some reason the built-in one only seems to apply to List
            for (int i = 0; i < source.Count; i++)
                if (condition(source[i]))
                    return i;
            return -1;
        }

        public static string ToRFC3339(this DateTime target)
        {
            return target.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", System.Globalization.DateTimeFormatInfo.InvariantInfo);
        }
    }
}
