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
