/*
HeapsTodo - a todo.txt-inspired text-based todo file manager, written in C#. 
Copyright (C) 2012 Tao Klerks

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using HeapsTodoLib;

namespace LibTests
{
    [TestFixture]
    public class TaskListTests
    {
        [Test]
        public void TasksString()
        {
            var taskListText = @"- One task
- another task
";

            var list = new HeapsTodoTaskList(taskListText);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("One task", list[0].MainBody);
            Assert.AreEqual("another task", list[1].MainBody);
            Assert.AreEqual(taskListText, list.PrintList());
        }

        [Test]
        public void TasksStringWithNotes()
        {
            var taskListText = @"- One task ```with notes
that span multiple lines
and have content```
- another task ```with an unclosed Note
";
            var list = new HeapsTodoTaskList(taskListText);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("One task", list[0].MainBody);
            Assert.AreEqual("another task", list[1].MainBody);
            Assert.AreEqual(taskListText + "```\r\n", list.PrintList());
        }

        [Test]
        public void SubTasks()
        {
            var taskListText = @"- One task ```with notes
that span multiple lines
and have content```
  - and a subtask task ```with 
notes
```
 - And another subtask at the same level
  - and another at a higher level now
";
            var list = new HeapsTodoTaskList(taskListText);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("One task", list[0].MainBody);
            Assert.AreEqual(2, list[0].SubTasks.Count);
            Assert.AreEqual(1, list[0].SubTasks[1].SubTasks.Count);
        }
    }
}
