using System;
using System.Collections.Generic;
using System.Text;

namespace HeapsTodoLib
{
    public class TodoTxtTask : BaseTask
    {
        public TodoTxtTask() : base() { }

        public TodoTxtTask(string rawTaskText) : this() 
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

            //this will set projects, contexts, due date, and other in-body key/value pairs.
            MainBody = remainingText;
        }

        public override string PrintTask()
        {
            StringBuilder outString = new StringBuilder();
            AppendTask(outString);
            return outString.ToString();
        }

        public void AppendTask(StringBuilder outString)
        {
            if (Completed)
            {
                outString.Append("x ");

                if (CompletionDate != null)
                {
                    outString.Append(CompletionDate.Value.ToString("yyyy-MM-dd"));
                    outString.Append(" ");
                }
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
        }
    }
}
