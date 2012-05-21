using System;
using System.Collections.Generic;

namespace HeapsTodoLib
{
    public interface ITaskList1<T> : IList<T>
    {
        ITaskList1<T> DeepClone();
        string PrintList();
        MergeResultInfo MergeToNewList(ITaskList1<T> otherList, out ITaskList1<T> newList);
    }

    public interface ITaskList2
    {
        string PrintList();
        MergeResultInfo MergeToNewList(ITaskList2 otherList, out ITaskList2 newList);
    }

}
