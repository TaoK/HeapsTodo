using System;
namespace HeapsTodoLib
{
    public interface ITask
    {
        char? Priority { get; set; }
        bool Completed { get; set; }
        DateTime? CompletionDate { get; set; }
        DateTime? CreationDate { get; set; }
        DateTime? DueDate { get; set; }
        System.Collections.Generic.IList<string> Contexts { get; }
        System.Collections.Generic.IList<string> Projects { get; }
        string MainBody { get; set; }
        string PrintTask();
    }
}
