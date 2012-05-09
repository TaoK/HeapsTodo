using System;
using System.Collections.Generic;
using System.Text;

namespace HeapsTodoLib
{
    public struct MergeResultInfo
    {
        public bool DeletionFromList1 { get; set; }
        public bool DeletionFromList2 { get; set; }
        public bool AdditionToList1 { get; set; }
        public bool AdditionToList2 { get; set; }
        public bool ModificationToList1 { get; set; }
        public bool ModificationToList2 { get; set; }
        public bool AnyChange
        {
            get
            {
                return DeletionFromList1 || DeletionFromList2
                    || AdditionToList1 || AdditionToList2
                    || ModificationToList1 || ModificationToList2;
            }
        }
    }
}
