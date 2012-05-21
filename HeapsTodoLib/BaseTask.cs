using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace HeapsTodoLib
{
    public abstract class BaseTask : HeapsTodoLib.ITask
    {

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

                        //TODO: handle failed parsing of invalid dates
                    }

                //TODO: Implement generic prefix:value storage with a read-only dictionary

                _mainBody = value;
            }
        }

        #endregion

        public BaseTask()
        {
            MainBody = "";
        }

        protected static Regex onlyDateMatcher = new Regex(@"^(\d{4})-(\d{2})-(\d{2})$");
        protected static Regex startingDateMatcher = new Regex(@"^(\d{4})-(\d{2})-(\d{2}) ");
        protected static Regex startingPriorityMatcher = new Regex(@"^\(([A-Z])\) ");
        protected static Regex projectsMatcher = new Regex(@"(^|\s)\+(\S+)(\s|$)");
        protected static Regex contextsMatcher = new Regex(@"(^|\s)\@(\S+)(\s|$)");
        protected static Regex keyValuePairsMatcher = new Regex(@"(^|\s)([^\s\:]+)\:([^\s\:]+)(\s|$)");

        public abstract string PrintTask();
    }

}
