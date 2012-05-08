using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace HeapsTodoLib
{
    public class TaskList : List<Task>
    {
        public TaskList() : base() { }

        public TaskList(string taskData)
        {
            using (StringReader sr = new StringReader(taskData))
            {
                foreach (Task t in ReadTasks(sr))
                    Add(t);
            }
        }

        public TaskList(TextReader tr)
        {
            foreach (Task t in ReadTasks(tr))
                Add(t);
        }

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

        public static IEnumerable<Task> ReadTasks(TextReader taskStream)
        {
            //in retrospect, doing this with stream-handling was probably a mistake. The same thing could be done with a 
            // couple of lines of regex and the memory requirements for all sane todo lists will always be very limited 
            // anyway, so any performance benefits are probably wishful thinking.
            //TODO: clean this up to just use regular in-memory strings w Regex.
            //TODO: add comment-handling

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
                        yield return new Task(taskStringBuilder.ToString());

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

        public string PrintList()
        {
            StringBuilder outString = new StringBuilder();
            foreach (Task t in this)
                t.AppendTask(outString, true, 0);
            return outString.ToString();
        }

    }
}