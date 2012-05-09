using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using HeapsTodoLib;

namespace LibTests
{
    [TestFixture]
    public class TaskTests
    {
        [Test]
        public void CompletedWithDate()
        {
            var testTask = new Task("x 2012-04-04 test");
            Assert.IsTrue(testTask.Completed);
            Assert.IsNotNull(testTask.CompletionDate);
        }

        [Test]
        public void CompletedWithoutDate()
        {
            var testTask = new Task("x 2012-04-04test");
            Assert.IsTrue(testTask.Completed);
            Assert.IsNull(testTask.CompletionDate);
        }

        [Test]
        public void CompletedComplex()
        {
            string taskText = @"x 2012-04-05 (A) 2012-04-04 @testcontext Test +testProject.broken due:2008-09-03 ```some notes```";
            var testTask = new Task(taskText);

            //confirm everything is as expected after parsing
            Assert.IsTrue(testTask.Completed);
            Assert.AreEqual(new DateTime(2012, 4, 5), testTask.CompletionDate);
            Assert.AreEqual('A', testTask.Priority);
            Assert.AreEqual(new DateTime(2012, 4, 4), testTask.CreationDate);
            Assert.AreEqual("@testcontext Test +testProject.broken due:2008-09-03", testTask.MainBody);
            Assert.That(testTask.Contexts.Contains("testcontext"));
            Assert.That(testTask.Projects.Contains("testProject.broken"));
            Assert.AreEqual("some notes", testTask.Notes);
            Assert.AreEqual(new DateTime(2008, 9, 3), testTask.DueDate);

            //confirm that serialization is also as expected:
            Assert.AreEqual(taskText + Environment.NewLine, testTask.PrintTask(true));
        }
    }
}
