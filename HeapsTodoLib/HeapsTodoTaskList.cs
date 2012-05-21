using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace HeapsTodoLib
{
    public class HeapsTodoTaskList : List<HeapsTodoTask>, ITaskList1<HeapsTodoTask>, ITaskList2
    {
        public const string HEAPSTODO_HEADER_COMMENT = "#HeapsTodo Task List";

        public HeapsTodoTaskList() : base() { }

        public HeapsTodoTaskList(string taskData)
        {
            string[] taskDataLines = Regex.Split(taskData, "\r\n|\r|\n");
            foreach (HeapsTodoTask t in ReadTasks(taskDataLines))
                Add(t);
        }

        public HeapsTodoTaskList(string[] taskDataLines)
        {
            foreach (HeapsTodoTask t in ReadTasks(taskDataLines))
                Add(t);
        }

        public IEnumerable<HeapsTodoTask> Flattened
        {
            get
            {
                foreach (HeapsTodoTask topLevel in this)
                    foreach (HeapsTodoTask flattenedCandidate in RecursiveFlatten(topLevel))
                        yield return flattenedCandidate;
            }
        }

        private IEnumerable<HeapsTodoTask> RecursiveFlatten(HeapsTodoTask taskToFlatten)
        {
            //this feels and looks REALLY convoluted... there must be a better way of doing this, right??
            yield return taskToFlatten;
            foreach (HeapsTodoTask subTask in taskToFlatten.SubTasks)
                foreach (HeapsTodoTask flattenedCandidate in RecursiveFlatten(subTask))
                    yield return flattenedCandidate;
        }

        public string PrintList()
        {
            StringBuilder outString = new StringBuilder();
            outString.AppendLine(HEAPSTODO_HEADER_COMMENT);
            foreach (HeapsTodoTask t in this)
            {
                t.AppendTask(outString, true, 0);
                outString.AppendLine();
            }
            return outString.ToString();
        }

        public ITaskList1<HeapsTodoTask> DeepClone()
        {
            //simple/inefficient implementation
            return new HeapsTodoTaskList(this.PrintList());
        }

        public static IList<HeapsTodoTask> ReadTasks(string[] taskDataLines)
        {
            int stringIndex = 0;
            return ReadTasksRecurse(taskDataLines, ref stringIndex, 0);
        }

        private static Regex _leadingSpaceMatcher = new Regex(@"^\s*");
        public static IList<HeapsTodoTask> ReadTasksRecurse(string[] strings, ref int stringIndex, int indentLevel)
        {
            List<HeapsTodoTask> outList = new List<HeapsTodoTask>();

            while (stringIndex < strings.Length)
            {
                var leadingWhiteSpaceMatch = _leadingSpaceMatcher.Match(strings[stringIndex]); //will always match, even when 0-length
                int leadingSpaceCount = leadingWhiteSpaceMatch.Value.Replace("\t", "    ").Length;

                //exit if nothing interesting
                if (leadingSpaceCount < indentLevel)
                    break;

                if (!strings[stringIndex].StartsWith("#") && !(stringIndex == strings.Length - 1 && strings[stringIndex].Length == 0))
                {
                    int fromIndex = stringIndex;
                    if (Regex.Matches(strings[stringIndex], "```").Count % 2 == 1)
                    {
                        stringIndex++;
                        while (stringIndex < strings.Length && Regex.Matches(strings[stringIndex], "```").Count % 2 == 0)
                            stringIndex++;
                    }

                    if (stringIndex == strings.Length) //we overshot
                        stringIndex--;

                    string[] interestingStrings = new string[stringIndex - fromIndex + 1];
                    Array.Copy(strings, fromIndex, interestingStrings, 0, stringIndex - fromIndex + 1);

                    var outTask = new HeapsTodoTask(string.Join(Environment.NewLine, interestingStrings));
                    stringIndex++;
                    foreach (HeapsTodoTask subtask in ReadTasksRecurse(strings, ref stringIndex, leadingSpaceCount + 1))
                        outTask.SubTasks.Add(subtask);
                    outList.Add(outTask);
                }
                else
                    stringIndex++;
            }

            return outList;
        }

        public MergeResultInfo MergeToNewList(ITaskList2 otherList, out ITaskList2 newList)
        {
            if (!(otherList is HeapsTodoTaskList))
                throw new ArgumentException("otherList must be a HeapsTodoTaskList.");

            ITaskList1<HeapsTodoTask> resultList = null;
            var mergeResult = MergeToNewList((ITaskList1<HeapsTodoTask>)otherList, out resultList);
            //this is technically wrong...  the merge COULD technically return something that was not a "ITaskList2" - I just know it doesn't right now...
            newList = (ITaskList2)resultList;
            return mergeResult;
        }

        public MergeResultInfo MergeToNewList(ITaskList1<HeapsTodoTask> otherList, out ITaskList1<HeapsTodoTask> newList)
        {
            //simple (simplistic) 2-way merge:
            // identical tasks are merged
            // non-identical tasks are duplicated
            // order of the first list is retained (with aditional subtasks potentially inserted)
            // any unmerged top-level tasks from the second list are added at the end of the resulting merged list

            MergeResultInfo resultInfo = new MergeResultInfo();
            ITaskList1<HeapsTodoTask> outList = (HeapsTodoTaskList)this.DeepClone();
            ITaskList1<HeapsTodoTask> tempList = (HeapsTodoTaskList)otherList.DeepClone();
            RecursivelyMerge(outList, tempList, ref resultInfo);
            newList = outList;
            return resultInfo;
        }

        private static void RecursivelyMerge(IList<HeapsTodoTask> outList, IList<HeapsTodoTask> tempList, ref MergeResultInfo resultInfo)
        {
            foreach (var task in outList)
            {
                string taskWithoutChildren = task.PrintTask(false);
                int? matchID = null;
                for (int i = 0; i < tempList.Count; i++)
                {
                    if (tempList[i].MainBody == task.MainBody //cheap comparison
                        && tempList[i].PrintTask(false) == taskWithoutChildren //more expensive complete comparison
                        )
                    {
                        matchID = i;
                        break;
                    }
                }

                if (matchID != null)
                {
                    RecursivelyMerge(task.SubTasks, tempList[matchID.Value].SubTasks, ref resultInfo);
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
        }
    }
}