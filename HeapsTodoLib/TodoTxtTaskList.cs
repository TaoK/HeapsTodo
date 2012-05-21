using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HeapsTodoLib
{
    public class TodoTxtTaskList : List<TodoTxtTask>, ITaskList1<TodoTxtTask>, ITaskList2
    {
        public TodoTxtTaskList() : base() { }

        public TodoTxtTaskList(string taskData)
        {
            foreach (string ts in Regex.Split(taskData, "\r\n|\r|\n"))
                Add(new TodoTxtTask(ts));
        }

        public TodoTxtTaskList(string[] taskDataLines)
        {
            foreach (string ts in taskDataLines)
                Add(new TodoTxtTask(ts));
        }

        public string PrintList()
        {
            StringBuilder outString = new StringBuilder();
            foreach (TodoTxtTask t in this)
            {
                t.AppendTask(outString);
                outString.AppendLine();
            }
            return outString.ToString();
        }

        public ITaskList1<TodoTxtTask> DeepClone()
        {
            //simple/inefficient implementation
            return new TodoTxtTaskList(this.PrintList());
        }

        public static TodoTxtTask ConvertFromHeapsTodo(HeapsTodoTask complexTask)
        {
            //TODO: this stuff should be moved somewhere else - inter-class conversion code should not 
            // be in the subject classes themselves

            //TODO: should be done with task itself printing with prefs object, but this is easier for now.
            string outPrint = complexTask.PrintTask(false);
            if (outPrint.StartsWith("- "))
                outPrint = outPrint.Substring(2);
            outPrint = outPrint.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
            foreach (var child in complexTask.SubTasks)
                outPrint += ", " + ConvertFromHeapsTodo(child).PrintTask();

            return new TodoTxtTask(outPrint);
        }

        public static TodoTxtTaskList ConvertFromHeapsTodoList(ITaskList1<HeapsTodoTask> complexTasks)
        {
            //TODO: this stuff should be moved somewhere else - inter-class conversion code should not 
            // be in the subject classes themselves
            var newList = new TodoTxtTaskList();
            foreach (HeapsTodoTask htd in complexTasks)
                newList.Add(ConvertFromHeapsTodo(htd));
            return newList;
        }

        public MergeResultInfo MergeToNewList(ITaskList2 otherList, out ITaskList2 newList)
        {
            //TODO: this stuff should be moved somewhere else - inter-class conversion code should not 
            // be in the subject classes themselves

            ITaskList1<TodoTxtTask> mergingList = null;
            ITaskList1<TodoTxtTask> resultList = null;

            //convert any other tasks to Todo.txt format
            if (otherList is ITaskList1<HeapsTodoTask>)
                mergingList = ConvertFromHeapsTodoList((ITaskList1<HeapsTodoTask>)otherList);
            else if (otherList is ITaskList1<TodoTxtTask>)
                mergingList = (ITaskList1<TodoTxtTask>)otherList;
            else
                throw new ArgumentException("otherList must be a TodoTxtTaskList or HeapsTodoTaskList.");

            var mergeResult = MergeToNewList(mergingList, out resultList);
            //this is technically wrong...  the merge COULD technically return something that was not a "ITaskList2" - I just know it doesn't right now...
            newList = (ITaskList2)resultList;
            return mergeResult;
        }

        public MergeResultInfo MergeToNewList(ITaskList1<TodoTxtTask> otherList, out ITaskList1<TodoTxtTask> newList)
        {
            //simple (simplistic) 2-way merge:
            // identical tasks are merged
            // non-identical tasks are duplicated
            // order of the first list is retained (with aditional subtasks potentially inserted)
            // any unmerged top-level tasks from the second list are added at the end of the resulting merged list

            MergeResultInfo resultInfo = new MergeResultInfo();
            ITaskList1<TodoTxtTask> outList = this.DeepClone();
            ITaskList1<TodoTxtTask> tempList = otherList.DeepClone();

            foreach (var task in outList)
            {
                string fullTaskText = task.PrintTask();
                int? matchID = null;
                for (int i = 0; i < tempList.Count; i++)
                {
                    if (tempList[i].MainBody == task.MainBody //cheap comparison
                        && tempList[i].PrintTask() == fullTaskText //more expensive complete comparison
                        )
                    {
                        matchID = i;
                        break;
                    }
                }

                if (matchID != null)
                {
                    tempList.RemoveAt(matchID.Value);
                }
                else
                {
                    resultInfo.AdditionToList2 = true;
                }
            }

            foreach (var task in tempList)
            {
                outList.Add(task);
                resultInfo.AdditionToList1 = true;
            }

            newList = outList;
            return resultInfo;
        }

    }
}
