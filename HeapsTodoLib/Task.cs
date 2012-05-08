using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace HeapsTodoLib
{
    public class Task
    {
        /*
         * HeapsTodo format rules
         * 
         * HeapsTodo tasks are closely based on todo.txt, but with one major difference (and a few smaller ones):
         * In HeapsTodo, a task is NOT necessarily limited to a single line. This one detail breaks the cardinal
         * rule of the todo.txt format, and means that HeapsTodo needs its own rules.
         * 
         * The todo.txt format, which HeapsTodo is loosely based on, is specified here:
         * https://github.com/ginatrapani/todo.txt-cli/wiki/The-Todo.txt-Format
         * 
         * Here are the four major DIFFERING rules of HeapsTodo tasks:
         * 
         * 1) A task is USUALLY limited to a single line, but it can have a "Notes" section which can span multiple lines.
         *  The "Notes" section of a task should usually be at the end of the task (if it is not, automated tools will always 
         *  put it there when rewriting / updating the task), and is bounded by SOME DELIMITER WHICH IS STILL TO BE DEFINED, 
         *  PROBABLY THREE BACKTICKS.
         * 
         * 2) Tasks are hierarchical - one task can have multiple sub-tasks, and those can have further sub-tasks, etc. 
         *  The relationship between the tasks is indicated by indentation: A task is a sub-task of the previous task if is 
         *  further indented. Tabs and spaces are both acceptable for indentation, and if a mixture is used then tabs 
         *  will be considered equivalent to 4 spaces. Programs are expected to handle these indentations consistently 
         *  RULES HERE. The default should be 2 spaces.
         *  
         * 3) The todo file may contain comments - comments are lines that start with a hash/number sign ("#"), outside of 
         *  the scope of a task "Notes" section. Comments are to be ignored when displaying/rendering a todo file as a set of 
         *  tasks or synchronizing a file with some other system, but should not be deleted from the file by any automated 
         *  processing. Commands MAY be moved, however, so they should never assume a position with respect to any given 
         *  task; they are really only useful at the top of a todo file.
         * 
         * 4) Like completed tasks are specified with a leading "x", incomplete tasks can OPTIONALLY have a leading hyphen
         *  "-". This can help with legibility when editing large complex todo lists with word wrap enabled, and/or files
         *  that contain many "Notes". The user should decide whether the leading hyphen should be automatically added or 
         *  not. (either way, the file will always work with any HeapsTodo-format-compliant program).
         * 
         * Beyond these specifications, the HeapsTodo format is effectively the same as todo.txt:
         *  
         * - A task may optionally start with a priority, a single uppercase letter in parens. (so you cannot have 
         *  more than 26 priority levels!)
         * - After the optional priority, a task may have an optional creation date.
         * - Dates are always specified in ISO-standard format, YYYY-MM-DD.
         * - A task is "Complete"/done if the first non-whitespace character is a lowercase "x".
         * - A task's completion date is a date specified immediately after the initial "x". This means that if you're 
         *  keeping track of task creation dates, you MUST also specify completion dates, because your creation dates 
         *  will otherwise be misinterpreted as completion dates (unless you also used priorities :))
         * - Contexts are specified with an initial "@" sign (and include everything to the next whitespace or end-of-
         *  line), and can feature anywhere in the task body.
         * - Projects are specified with an initial "+" sign (and include everything to the next whitespace or end-of-
         *  line), and can feature anywhere in the task body.
         *  
         * These simple rules mean that any line of text can be a todo item (unless it is in a Notes section), and any 
         *  violations of the format rules (for example, having an uppercase X at the start or a creation date in another 
         *  date format) simply mean that text will be taken to be part of the task name/body. Is is not possible to 
         *  cause a "Parsing Failure" in todo.txt or HeapsTodo files, any text file is a legal (if likely 
         *  garbled/uninteligible) todo file.
         *  
         * Also, any sensible todo.txt also works with HeapsTodo, there is no "import" process necessary because 
         *  the HeapsTodo format is a superset/extension of todo.txt.
         *  
         * This "library" is still missing a lot of stuff:
         *  - Documentation
         *  - Thread-safety
         *  - File-Handling
         *  - Change-detection (and handling approach)
         *  - Preference-handling, esp. "todo.txt-compatible mode" disabling extensions
         *  
         */

        #region Public Properties

        private char? _priority;
        public char? Priority
        {
            get
            {
                return _priority;
            }
            set
            {
                if (value == null || (value.Value >= 'A' && value.Value <= 'Z'))
                    _priority = value;
                else
                    throw new ArgumentOutOfRangeException("Priority must be an uppercase letter between A and Z");
            }
        }

        public DateTime? CreationDate { get; set; }
        public DateTime? DueDate { get; set; }

        private bool _completed;
        public bool Completed
        {
            get
            {
                return _completed;
            }
            set
            {
                if (!value)
                    _completionDate = null;
                _completed = value;
            }
        }

        private DateTime? _completionDate;
        public DateTime? CompletionDate
        {
            get
            {
                return _completionDate;
            }
            set
            {
                if (value == null)
                    _completed = false;
                else
                    _completed = true;

                _completionDate = value;
            }
        }

        private List<string> _projects = null;
        public IList<string> Projects
        {
            get
            {
                return _projects.AsReadOnly();
            }
        }

        private List<string> _contexts = null;
        public IList<string> Contexts
        {
            get
            {
                return _contexts.AsReadOnly();
            }
        }

        private string _mainBody;
        public string MainBody
        {
            get
            {
                return _mainBody;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Task body may not be null.");

                _projects = new List<string>();
                foreach (Match match in projectsMatcher.Matches(value))
                    if (!_projects.Contains(match.Groups[2].Value))
                        _projects.Add(match.Groups[2].Value);

                _contexts = new List<string>();
                foreach (Match match in contextsMatcher.Matches(value))
                    if (!_contexts.Contains(match.Groups[2].Value))
                        _contexts.Add(match.Groups[2].Value);

                DueDate = null;
                foreach (Match match in keyValuePairsMatcher.Matches(value))
                    if (match.Groups[2].Value == "due")
                    {
                        Match dateMatch = onlyDateMatcher.Match(match.Groups[3].Value);
                        if (dateMatch.Success)
                            DueDate = new DateTime(int.Parse(dateMatch.Groups[1].Value), int.Parse(dateMatch.Groups[2].Value), int.Parse(dateMatch.Groups[3].Value));
                    }

                //TODO: Implement generic prefix:value storage with a read-only dictionary

                _mainBody = value;
            }
        }

        public string Notes { get; set; }

        private Task _parentTask;
        public Task ParentTask
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

        public Task()
        {
            MainBody = "";
            _subTasks = new SubTaskList(this);
        }

        private static Regex onlyDateMatcher = new Regex(@"^(\d{4})-(\d{2})-(\d{2})$");
        private static Regex startingDateMatcher = new Regex(@"^(\d{4})-(\d{2})-(\d{2}) ");
        private static Regex startingPriorityMatcher = new Regex(@"^\(([A-Z])\) ");
        private static Regex closedNotesMatcher = new Regex(@"\ ?\`\`\`(.*?)\`\`\`", RegexOptions.Singleline);
        private static Regex openNotesMatcher = new Regex(@"\ ?\`\`\`(.*)$", RegexOptions.Singleline);
        private static Regex projectsMatcher = new Regex(@"(^|\s)\+(\S+)(\s|$)");
        private static Regex contextsMatcher = new Regex(@"(^|\s)\@(\S+)(\s|$)");
        private static Regex keyValuePairsMatcher = new Regex(@"(^|\s)([^\s\:]+)\:([^\s\:]+)(\s|$)");

        public Task(string rawTaskText)
            : this()
        {
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

        public string PrintTask(bool includeSubTasks)
        {
            StringBuilder outString = new StringBuilder();
            AppendTask(outString, includeSubTasks, 0);
            return outString.ToString();
        }

        public void AppendTask(StringBuilder outString, bool includeSubTasks, int indentLevel)
        {
            for (int i = 0; i < indentLevel; i++)
                outString.Append("\t");

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

            outString.AppendLine();

            if (includeSubTasks)
            {
                foreach (Task sub in SubTasks)
                    sub.AppendTask(outString, true, indentLevel + 1);
            }
        }

    }

    public class SubTaskList : IList<Task>
    {
        List<Task> _backingList = new List<Task>();
        Task _ownerTask = null;

        internal SubTaskList(Task ownerTask)
        {
            _ownerTask = ownerTask;
        }

        private void CheckForLoops(Task candidateTask)
        {
            if (candidateTask == null)
                throw new ArgumentNullException("Provided SubTask may not be Null.");

            if (candidateTask == _ownerTask)
                throw new ArgumentException("Cannot set same or ancestor task as SubTask, circular dependencies are not allowed.");

            foreach (Task subtask in candidateTask.SubTasks)
                CheckForLoops(subtask);
        }

        public int IndexOf(Task item)
        {
            return _backingList.IndexOf(item);
	    }

        public void Insert(int index, Task item)
        {
            CheckForLoops(item);
            _backingList.Insert(index, item);
            item.ParentTask = _ownerTask;
        }

        public void RemoveAt(int index)
        {
            Task garbageTask = _backingList[index];
            _backingList.RemoveAt(index);
            garbageTask.ParentTask = null;
        }

        public Task this[int index]
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

        public void Add(Task item)
        {
            CheckForLoops(item);
            _backingList.Add(item);
            item.ParentTask = _ownerTask;
        }

        public void Clear()
        {
            Task[] orphanTasks = _backingList.ToArray();
            _backingList.Clear();
            foreach (Task orphan in orphanTasks)
                orphan.ParentTask = null;
        }

        public bool Contains(Task item)
        {
            return _backingList.Contains(item);
        }

        public void CopyTo(Task[] array, int arrayIndex)
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

        public bool Remove(Task item)
        {
            bool removed = _backingList.Remove(item);
            item.ParentTask = null;
            return removed;
        }

        public IEnumerator<Task> GetEnumerator()
        {
            return _backingList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _backingList.GetEnumerator();
        }
    }
}
