using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace HeapsTodoLib
{
    public class TaskList : List<Task>
    {
        public TaskList() : base() { }

        public TaskList(string taskData)
        {
            foreach (Task t in ReadTasks(taskData))
                Add(t);
        }

        //public TaskList(TextReader tr)
        //{
        //    foreach (Task t in ReadTasksOld(tr))
        //        Add(t);
        //}

        public IEnumerable<Task> Flattened
        {
            get
            {
                foreach (Task topLevel in this)
                    foreach (Task flattenedCandidate in RecursiveFlatten(topLevel))
                        yield return flattenedCandidate;
            }
        }

        private IEnumerable<Task> RecursiveFlatten(Task taskToFlatten)
        {
            //this feels and looks REALLY convoluted... there must be a better way of doing this, right??
            yield return taskToFlatten;
            foreach (Task subTask in taskToFlatten.SubTasks)
                foreach (Task flattenedCandidate in RecursiveFlatten(subTask))
                    yield return flattenedCandidate;
        }

        public string PrintList()
        {
            StringBuilder outString = new StringBuilder();
            foreach (Task t in this)
                t.AppendTask(outString, true, 0);
            return outString.ToString();
        }

        public TaskList DeepClone()
        {
            //simple/inefficient implementation
            return new TaskList(this.PrintList());
        }

        public static IList<Task> ReadTasks(string taskData)
        {
            string[] strings = Regex.Split(taskData, "\r\n|\r|\n");
            int stringIndex = 0;

            //TODO: do any initial-comment-parsing here

            return ReadTasksRecurse(strings, ref stringIndex, 0);
        }

        private static Regex _leadingSpaceMatcher = new Regex(@"^\s*");
        public static IList<Task> ReadTasksRecurse(string[] strings, ref int stringIndex, int indentLevel)
        {
            List<Task> outList = new List<Task>();

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

                    var outTask = new Task(string.Join(Environment.NewLine, interestingStrings));
                    stringIndex++;
                    foreach (Task subtask in ReadTasksRecurse(strings, ref stringIndex, leadingSpaceCount + 1))
                        outTask.SubTasks.Add(subtask);
                    outList.Add(outTask);
                }
                else
                    stringIndex++;
            }

            return outList;
        }

        public static IEnumerable<Task> ReadTasksOld(TextReader taskStream)
        {
            return ReadTasksOld(taskStream, 0);
        }

        public static IEnumerable<Task> ReadTasksOld(TextReader taskStream, int stringIndent)
        {
            //in retrospect, doing this with stream-handling was probably a mistake. The same thing could be done with a 
            // couple of lines of regex and the memory requirements for all sane todo lists will always be very limited 
            // anyway, so any performance benefits are probably wishful thinking.
            //TODO: clean this up to just use regular in-memory strings w Regex.
            //TODO: add comment-handling
            //TODO: add switch for regular todo.txt vs heapstodo format
            //TODO: add parsing of hierarchies

            StringBuilder taskStringBuilder = new StringBuilder();
            bool inNotesSection = false;
            int backticksSoFar = 0;

            while (true)
            {
                int nextChar = taskStream.Read();
                if (nextChar == -1)
                    break;

                char currentChar = (char)nextChar;

                if (!inNotesSection && (currentChar == '\r' || currentChar == '\n'))
                {
                    //treat all known line endings, \r\n, \r and \n as equivalent.
                    if (currentChar == '\r')
                    {
                        int followingChar = taskStream.Peek();
                        if (followingChar > -1 && (char)followingChar == '\n')
                            taskStream.Read();
                    }

                    if (taskStringBuilder.Length > 0)
                    {

                        yield return new Task(taskStringBuilder.ToString());
                    }

                    taskStringBuilder.Length = 0;
                }
                else
                {
                    if (currentChar == '`')
                    {
                        if (backticksSoFar == 2)
                        {
                            inNotesSection = !inNotesSection;
                            backticksSoFar = 0;
                        }
                        else
                        {
                            backticksSoFar++;
                        }
                    }
                    else
                    {
                        backticksSoFar = 0;
                    }

                    taskStringBuilder.Append(currentChar);
                }
            }

            if (taskStringBuilder.Length > 0)
                yield return new Task(taskStringBuilder.ToString());
        }

        public static MergeResultInfo MergeLists(TaskList list1, TaskList list2, out TaskList newList)
        {
            //simple (simplistic) 2-way merge:
            // identical tasks are merged
            // non-identical tasks are duplicated
            // order of the first list is retained (with aditional subtasks potentially inserted)
            // any unmerged top-level tasks from the second list are added at the end of the resulting merged list

            MergeResultInfo resultInfo = new MergeResultInfo();
            TaskList outList = list1.DeepClone();
            TaskList tempList = list2.DeepClone();
            RecursivelyMerge(outList, tempList, ref resultInfo);
            newList = outList;
            return resultInfo;
        }

        private static void RecursivelyMerge(IList<Task> outList, IList<Task> tempList, ref MergeResultInfo resultInfo)
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