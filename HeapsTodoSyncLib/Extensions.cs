/*
HeapsTodo - a todo.txt-inspired text-based todo file manager, written in C#. 
Copyright (C) 2012 Tao Klerks

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

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
