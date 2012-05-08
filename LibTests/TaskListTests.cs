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
            var taskListText = @"One task
another task
";

            var list = new TaskList(taskListText);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("One task", list[0].MainBody);
            Assert.AreEqual("another task", list[1].MainBody);
            Assert.AreEqual(taskListText, list.ToString());
        }

        [Test]
        public void TasksStringWithNotes()
        {
            var taskListText = @"One task ```with notes
that span multiple lines
and have content```
another task ```with an unclosed Note
";
            var list = new TaskList(taskListText);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("One task", list[0].MainBody);
            Assert.AreEqual("another task", list[1].MainBody);
            Assert.AreEqual(taskListText + "```\r\n", list.ToString());
        }
    }
}
