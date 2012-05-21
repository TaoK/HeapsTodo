using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HeapsTodoLib
{
    public static class TaskListFactory
    {
        public static ITaskList2 ReadListFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public static ITaskList2 ReadList(string fileContent)
        {
            string[] strings = Regex.Split(fileContent, "\r\n|\r|\n");
            return ReadList(strings);
        }

        public static ITaskList2 ReadList(string[] fileContentLines)
        {
            if (fileContentLines.Length > 0 && fileContentLines[0].StartsWith(HeapsTodoTaskList.HEAPSTODO_HEADER_COMMENT, StringComparison.InvariantCultureIgnoreCase))
                return new HeapsTodoTaskList(fileContentLines);
            else
                return new TodoTxtTaskList(fileContentLines);
        }
    }
}
