using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HeapsTodoLib
{
    public class HeapsTodoTask : BaseTask
    {

        #region Local Properties

        public string Notes { get; set; }

        private HeapsTodoTask _parentTask;
        public HeapsTodoTask ParentTask
        {
            get
            {
                return _parentTask;
            }
            internal set
            {
                _parentTask = value;
            }
        }

        private SubTaskList _subTasks;
        public SubTaskList SubTasks
        {
            get
            {
                return _subTasks;
            }
        }

        #endregion

        public HeapsTodoTask() : base()
        {
            _subTasks = new SubTaskList(this);
        }

        protected static Regex closedNotesMatcher = new Regex(@"\ ?\`\`\`(.*?)\`\`\`", RegexOptions.Singleline);
        protected static Regex openNotesMatcher = new Regex(@"\ ?\`\`\`(.*)$", RegexOptions.Singleline);

        public HeapsTodoTask(string rawTaskText) : this()
        {
            //TODO: possibly handle subtask parsing here rather than in task list

            if (rawTaskText == null)
                throw new ArgumentNullException("rawTaskText may not be null");

            string remainingText = rawTaskText.TrimStart();

            if (remainingText.StartsWith("x ", StringComparison.InvariantCulture))
            {
                Completed = true;
                remainingText = remainingText.Substring(2);

                var completionDateMatch = startingDateMatcher.Match(remainingText);
                if (completionDateMatch.Success)
                {
                    CompletionDate = new DateTime(int.Parse(completionDateMatch.Groups[1].Value), int.Parse(completionDateMatch.Groups[2].Value), int.Parse(completionDateMatch.Groups[3].Value));
                    remainingText = remainingText.Substring(completionDateMatch.Length);
                }
            }
            else if (remainingText.StartsWith("- "))
            {
                //leading hyphen is decoration only.
                remainingText = remainingText.Substring(2);
            }

            var priorityMatch = startingPriorityMatcher.Match(remainingText);
            if (priorityMatch.Success)
            {
                Priority = priorityMatch.Groups[0].Value.ToCharArray()[1];
                remainingText = remainingText.Substring(priorityMatch.Length);
            }

            var creationDateMatch = startingDateMatcher.Match(remainingText);
            if (creationDateMatch.Success)
            {
                CreationDate = new DateTime(int.Parse(creationDateMatch.Groups[1].Value), int.Parse(creationDateMatch.Groups[2].Value), int.Parse(creationDateMatch.Groups[3].Value));
                remainingText = remainingText.Substring(creationDateMatch.Length);
            }

            foreach (Match match in closedNotesMatcher.Matches(remainingText))
            {
                if (Notes == null)
                    Notes = match.Groups[1].Value;
                else
                    Notes += " " + match.Groups[1].Value;
            }
            //not very efficient to rerun the match process, but much easier than doing a gradual replace in the loop
            remainingText = closedNotesMatcher.Replace(remainingText, "");

            //also want to capture unclosed notes
            var openNotesMatch = openNotesMatcher.Match(remainingText);
            if (openNotesMatch.Success)
            {
                if (Notes == null)
                    Notes = openNotesMatch.Groups[1].Value;
                else
                    Notes += " " + openNotesMatch.Groups[1].Value;

                //remove everything from the start of the match, we know this one is always at the end of the string
                remainingText = remainingText.Substring(0, openNotesMatch.Index);
            }

            //this will set projects, contexts, due date, and other in-body key/value pairs.
            MainBody = remainingText;
        }


        public override string PrintTask()
        {
            return PrintTask(true);
        }

        public string PrintTask(bool includeSubTasks)
        {
            StringBuilder outString = new StringBuilder();
            AppendTask(outString, includeSubTasks, 0);
            return outString.ToString();
        }

        public void AppendTask(StringBuilder outString)
        {
            AppendTask(outString, true, 0);
        }

        public void AppendTask(StringBuilder outString, bool includeSubTasks, int indentLevel)
        {
            for (int i = 0; i < indentLevel; i++)
                outString.Append("  ");

            if (Completed)
            {
                outString.Append("x ");

                if (CompletionDate != null)
                {
                    outString.Append(CompletionDate.Value.ToString("yyyy-MM-dd"));
                    outString.Append(" ");
                }
            }
            else
            {
                outString.Append("- ");
            }

            if (Priority != null)
            {
                outString.Append("(");
                outString.Append(Priority.Value);
                outString.Append(") ");
            }

            if (CreationDate != null)
            {
                outString.Append(CreationDate.Value.ToString("yyyy-MM-dd"));
                outString.Append(" ");
            }

            //figure out how key/value pairs fit in, like due date - maybe there should be an easy way to add them so they default to the end?
            outString.Append(MainBody);

            if (Notes != null)
            {
                outString.Append(" ```");
                outString.Append(Notes);
                outString.Append("```");
            }

            if (includeSubTasks)
            {
                foreach (HeapsTodoTask sub in SubTasks)
                {
                    outString.AppendLine();

                    sub.AppendTask(outString, true, indentLevel + 1);
                }
            }
        }
    }

    public class SubTaskList : IList<HeapsTodoTask>
    {
        List<HeapsTodoTask> _backingList = new List<HeapsTodoTask>();
        HeapsTodoTask _ownerTask = null;

        internal SubTaskList(HeapsTodoTask ownerTask)
        {
            _ownerTask = ownerTask;
        }

        private void CheckForLoops(HeapsTodoTask candidateTask)
        {
            if (candidateTask == null)
                throw new ArgumentNullException("Provided SubTask may not be Null.");

            if (candidateTask == _ownerTask)
                throw new ArgumentException("Cannot set same or ancestor task as SubTask, circular dependencies are not allowed.");

            foreach (HeapsTodoTask subtask in candidateTask.SubTasks)
                CheckForLoops(subtask);
        }

        public int IndexOf(HeapsTodoTask item)
        {
            return _backingList.IndexOf(item);
        }

        public void Insert(int index, HeapsTodoTask item)
        {
            CheckForLoops(item);
            _backingList.Insert(index, item);
            item.ParentTask = _ownerTask;
        }

        public void RemoveAt(int index)
        {
            HeapsTodoTask garbageTask = _backingList[index];
            _backingList.RemoveAt(index);
            garbageTask.ParentTask = null;
        }

        public HeapsTodoTask this[int index]
        {
            get
            {
                return _backingList[index];
            }
            set
            {
                CheckForLoops(value);
                RemoveAt(index);
                Insert(index, value);
            }
        }

        public void Add(HeapsTodoTask item)
        {
            CheckForLoops(item);
            _backingList.Add(item);
            item.ParentTask = _ownerTask;
        }

        public void Clear()
        {
            HeapsTodoTask[] orphanTasks = _backingList.ToArray();
            _backingList.Clear();
            foreach (HeapsTodoTask orphan in orphanTasks)
                orphan.ParentTask = null;
        }

        public bool Contains(HeapsTodoTask item)
        {
            return _backingList.Contains(item);
        }

        public void CopyTo(HeapsTodoTask[] array, int arrayIndex)
        {
            _backingList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _backingList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(HeapsTodoTask item)
        {
            bool removed = _backingList.Remove(item);
            item.ParentTask = null;
            return removed;
        }

        public IEnumerator<HeapsTodoTask> GetEnumerator()
        {
            return _backingList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _backingList.GetEnumerator();
        }
    }
}
