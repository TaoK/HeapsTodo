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
