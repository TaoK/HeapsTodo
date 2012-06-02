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
using System.Collections;
using System.Collections.Generic;

class BiDictionary<TFirst, TSecond> : IDictionary<TFirst, TSecond>, IEnumerable<KeyValuePair<TFirst, TSecond>>, IEnumerable
{
    //this class adapted from an answer by by Jon Skeet, obtained from Stack Overflow in April 2012:
    // http://stackoverflow.com/a/255630/74296
    // - Adjusted to implement interfaces

    IDictionary<TFirst, TSecond> firstToSecond = new Dictionary<TFirst, TSecond>();
    IDictionary<TSecond, TFirst> secondToFirst = new Dictionary<TSecond, TFirst>();

    public void Add(TFirst first, TSecond second)
    {
        if (firstToSecond.ContainsKey(first) ||
            secondToFirst.ContainsKey(second))
        {
            throw new ArgumentException("Duplicate first or second");
        }
        firstToSecond.Add(first, second);
        secondToFirst.Add(second, first);
    }

    public bool TryGetBySecond(TSecond second, out TFirst first)
    {
        return secondToFirst.TryGetValue(second, out first);
    }

    public TSecond this[TFirst index]
    {
        get
        {
            return firstToSecond[index];
        }
        set
        {
            firstToSecond[index] = value;
        }
    }

    public IEnumerator GetEnumerator()
    {
        return firstToSecond.GetEnumerator();
    }

    public bool ContainsKey(TFirst key)
    {
        return firstToSecond.ContainsKey(key);
    }

    public ICollection<TFirst> Keys
    {
        get { return firstToSecond.Keys; }
    }

    public bool Remove(TFirst key)
    {
        TSecond keyValue = firstToSecond[key];
        return firstToSecond.Remove(key) && secondToFirst.Remove(keyValue);
    }

    public bool TryGetValue(TFirst key, out TSecond value)
    {
        return firstToSecond.TryGetValue(key, out value);
    }

    public ICollection<TSecond> Values
    {
        get { return firstToSecond.Values; }
    }

    public void Add(KeyValuePair<TFirst, TSecond> item)
    {
        firstToSecond.Add(item);
    }

    public void Clear()
    {
        firstToSecond.Clear();
    }

    public bool Contains(KeyValuePair<TFirst, TSecond> item)
    {
        return firstToSecond.Contains(item);
    }

    public void CopyTo(KeyValuePair<TFirst, TSecond>[] array, int arrayIndex)
    {
        firstToSecond.CopyTo(array, arrayIndex);
    }

    public int Count
    {
        get { return firstToSecond.Count; }
    }

    public bool IsReadOnly
    {
        get { return firstToSecond.IsReadOnly; }
    }

    public bool Remove(KeyValuePair<TFirst, TSecond> item)
    {
        KeyValuePair<TSecond, TFirst> otherPair = new KeyValuePair<TSecond, TFirst>(item.Value, item.Key);
        return firstToSecond.Remove(item) && secondToFirst.Remove(otherPair);
    }

    IEnumerator<KeyValuePair<TFirst, TSecond>> IEnumerable<KeyValuePair<TFirst, TSecond>>.GetEnumerator()
    {
        return firstToSecond.GetEnumerator();
    }

}