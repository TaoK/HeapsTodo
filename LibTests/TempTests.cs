using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace LibTests
{
    [TestFixture]
    public class TempTests
    {
        [Test]
        public void TwoWayMergeTest()
        {
            string[] file1 = { "first line", "second line", "third line" };
            string[] file2 = { "a completely different line" };
            string[] file3 = { "second line", "first line", "third line" };
            string[] file4 = { "first line", "third line" };

            Assert.AreEqual(3, SynchrotronNet.Diff.diff_merge_keepall(file1, file1).Count);
            Assert.AreEqual(4, SynchrotronNet.Diff.diff_merge_keepall(file1, file2).Count);
            Assert.AreEqual(4, SynchrotronNet.Diff.diff_merge_keepall(file1, file3).Count);
            Assert.AreEqual(3, SynchrotronNet.Diff.diff_merge_keepall(file1, file4).Count);

        }
    }
}
