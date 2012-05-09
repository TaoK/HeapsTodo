using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

using NDesk.Options;
using HeapsTodoLib;

namespace HeapsTodoSyncTool
{
    class Program
    {
        public enum SyncTargetType
        {
            GoogleTasks = 'G'
        }

        public enum ForcedConflictResolutionDirection
        {
            Local,
            Remote
        }

        static int Main(string[] args)
        {
            string syncTargetFile = null;
            SyncTargetType targetType = SyncTargetType.GoogleTasks;
            string syncTargetTypeCode = null;
            string listName = null;
            string forcedConflictResolutionString = null;
            ForcedConflictResolutionDirection? forcedConflictResolution = null;
            bool showUsageFriendly = false;
            bool showUsageError = false;
            bool noInteractivity = false;
            bool verbose = false;

            OptionSet p = new OptionSet()
              .Add("t|syncType=", delegate(string v) { syncTargetTypeCode = v; })
              .Add("n|listName=", delegate(string v) { listName = v; })
              .Add("fcr|forcedConflictResolution=", delegate(string v) { forcedConflictResolutionString = v; })
              .Add("ni|noInteractivity", delegate(string v) { noInteractivity = v != null; })
              .Add("v|verbose", delegate(string v) { verbose = v != null; })
              .Add("h|?|help", delegate(string v) { showUsageFriendly = v != null; })
                  ;

            //first parse the args
            List<string> remainingArgs = p.Parse(args);

            //then check manual-verification params
            if (forcedConflictResolutionString != null)
            {
                if (string.Compare(forcedConflictResolutionString, ForcedConflictResolutionDirection.Local.ToString(), true) == 0)
                    forcedConflictResolution = ForcedConflictResolutionDirection.Local;
                else if (string.Compare(forcedConflictResolutionString, ForcedConflictResolutionDirection.Remote.ToString(), true) == 0)
                    forcedConflictResolution = ForcedConflictResolutionDirection.Remote;
                else
                {
                    showUsageError = true;
                    Console.Error.WriteLine("Unrecognized value for 'forcedConflictResolution' parameter.");
                }
            }

            if (syncTargetTypeCode != null && syncTargetTypeCode.Length == 1)
            {
                if (syncTargetTypeCode.ToUpperInvariant().ToCharArray()[0] == (char)SyncTargetType.GoogleTasks)
                    targetType = SyncTargetType.GoogleTasks;
            }
            else
            {
                showUsageError = true;
                Console.Error.WriteLine("Missing or incorrect value for 'syncType' parameter.");
            }

            //then complain about missing input or unrecognized args
            if (remainingArgs.Count == 0)
            {
                showUsageError = true;
                Console.Error.WriteLine("No input filename has been provided!");
            }
            else if (remainingArgs.Count > 1)
            {
                showUsageError = true;
                Console.Error.WriteLine("Unrecognized arguments found.");
            }
            else
            {
                syncTargetFile = remainingArgs[0];
            }

            if (showUsageFriendly || showUsageError)
            {
                //TODO: provide real usage instructions
                TextWriter outStream = showUsageFriendly ? Console.Out : Console.Error;
                outStream.WriteLine("HeapsTodoSyncTool - a simple scriptable sync for HeapsTodo and todo.txt files.");
                outStream.WriteLine("v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
                outStream.WriteLine(@"
Usage: This is confidential. see if you can figure it out.

Example: HeapsTodoSyncTool myFile.txt /t:G /n:""Mobile List""
");
                return 1;
            }

            if (string.IsNullOrEmpty(listName))
                listName = Path.GetFileName(syncTargetFile);
            string googleSyncCacheFileName = syncTargetFile + "." + CleanListForPath(listName) + ".htdgsync";

            string clientID = "1053655555490.apps.googleusercontent.com";
            string clientSecret = "UUJ3kv-i5l2PdBpC23tBUHsz";

            //TODO: TEMPORARY SIMPLIFICATION!!
            // really, most of this is part of the sync logic and should be implemented with callbacks for UI interaction
            // also progress indicator and cancellation option?

            //TODO: Verbose logging, so that a scheduled forced sync can comfortably be let loose, knowing that 
            // you will always be able to piece together exactly what happened.
            // -> should be supported to STDOUT for simplicity, and maybe also to a specified file in case you want to 
            // enable it with interactive use (redirects preclude interactive use)

            bool newGoogleList = false;
            bool newLocalFile = false;

            //handle missing at g
            TaskList importedlist = null;
            try
            {
                importedlist = HeapsTodoSyncLib.GoogleSync.ConvertGoogleTasksToTodoTxtTaskList(listName, clientID, clientSecret);
            }
            catch (HeapsTodoSyncLib.GoogleSync.MissingListException)
            {
                if (Confirm("The Google Tasks list you requested was not found in your Google Tasks account - would you like to create it?", noInteractivity))
                {
                    //the sync cache can only lead to errors in this situation - would cause anything that WAS in the 
                    // cache to be deleted by the 3-way merge!
                    if (File.Exists(googleSyncCacheFileName))
                        File.Delete(googleSyncCacheFileName);

                    //actually create the list at google.
                    importedlist = new TaskList();
                    HeapsTodoSyncLib.GoogleSync.CreateGoogleTasksList(listName, clientID, clientSecret, importedlist);
                    newGoogleList = true;
                }
                else
                {
                    return 1;
                }
            }

            if (!File.Exists(syncTargetFile))
            {
                if (Confirm("The provided todo filename was not found. Would you like to create it?", noInteractivity))
                {
                    //the sync cache can only lead to errors in this situation - would cause anything that WAS in the 
                    // cache to be deleted by the 3-way merge!
                    if (File.Exists(googleSyncCacheFileName))
                        File.Delete(googleSyncCacheFileName);

                    //TODO: make  decision as to whether this is a todo.txt or a heapstodo file.
                    File.WriteAllText(syncTargetFile, "", Encoding.UTF8);
                    newLocalFile = true;
                }
                else
                {
                    return 1;
                }
            }

            //START MERGE
            TaskList resultList = null;
            MergeResultInfo mergeResultInfo;
            if (!newGoogleList && !newLocalFile && !File.Exists(googleSyncCacheFileName))
            {
                if (Confirm("This appears to be the first time you are synchronizing this file with this list. Any identical entries will be merged, and any entries that differ or are unique will be included in both lists, and the order of entries will be based on the local list. Would you like to continue?", noInteractivity))
                {
                    TaskList existingLocalList = new TaskList(File.ReadAllText(syncTargetFile));
                    mergeResultInfo = TaskList.MergeLists(existingLocalList, importedlist, out resultList);
                }
                else
                {
                    return 1;
                }
            }
            else if (!File.Exists(googleSyncCacheFileName))
            {
                //one will be filled with the other, no need for 3-way merge
                TaskList existingLocalList = new TaskList(File.ReadAllText(syncTargetFile));
                mergeResultInfo = TaskList.MergeLists(existingLocalList, importedlist, out resultList);
            }
            else
            {
                //regular 3-way merge

                string[] targetFileStrings = File.ReadAllLines(syncTargetFile);
                
                string[] googleSyncCacheStrings = File.ReadAllLines(googleSyncCacheFileName);

                string importedListString = importedlist.PrintList();
                //remove the trailing newline (empty string = 0 lines in array) to match behaviour of ReadAllLines()
                //TODO: add this behaviour into TaskList object and clarify
                string[] importedTaskStringArray = null;
                if (importedListString.Length > 0) 
                {
                    importedListString = importedListString.Substring(0, importedListString.Length - Environment.NewLine.Length);
                    importedTaskStringArray = Regex.Split(importedListString, "\r\n|\r|\n");
                }
                else
                {
                    importedTaskStringArray = new string[0];
                }

                var rawMergeResult = SynchrotronNet.Diff.diff3_merge(
                    targetFileStrings,
                    googleSyncCacheStrings,
                    importedTaskStringArray,
                    true
                    );

                string[] mergeResult;
                if (rawMergeResult.Count == 1 && rawMergeResult[0] is SynchrotronNet.Diff.MergeOKResultBlock)
                {
                    mergeResult = ((SynchrotronNet.Diff.MergeOKResultBlock)rawMergeResult[0]).ContentLines;
                }
                else
                {
                    //standard 3-way merge reported conflicts - we need to deal with it.

                    List<string> okLines = new List<string>();

                    foreach (var mergeResultBlock in rawMergeResult)
                    {
                        if (mergeResultBlock is SynchrotronNet.Diff.MergeOKResultBlock)
                            okLines.AddRange(((SynchrotronNet.Diff.MergeOKResultBlock)mergeResultBlock).ContentLines);
                        else
                        {
                            var conflictBlock = (SynchrotronNet.Diff.MergeConflictResultBlock)mergeResultBlock;
                            if (conflictBlock.OldLines.Length == 0)
                            {
                                //in programming, a same-point non-identical insert is a conflict due to the importance
                                // of order-of-execution - but for tasks, the relative order of two tasks is reasonably 
                                // arbitrary/unimportant, so this "conflict" can be silently handled; left (local) always
                                // comes first.
                                // NOTE: in heapstodo this might also occur in a "Notes" section. Even then, the chances 
                                // of an order-of-insert problem being relevant to the human (in the EXTREMELY rare case
                                // where this type of different insert change will be made to the notes of the same task
                                // on two different systems) are infinitesimally small.
                                // NOTE (2): A non-identical insert might well contain identical regions (identical tasks 
                                // inserted into both systems independently) - to help handle this cleanly, only add the
                                // merged result of the two conflict blocks. If there are nocommon changes, then this 
                                // will automatically resolve to just the first file's lines followed by the second 
                                // file's lines.

                                okLines.AddRange(
                                    SynchrotronNet.Diff.diff_merge_keepall(
                                        conflictBlock.LeftLines,
                                        conflictBlock.RightLines
                                        )
                                    );
                            }
                            else if (forcedConflictResolution != null)
                            {
                                switch (forcedConflictResolution.Value)
                                {
                                    case ForcedConflictResolutionDirection.Local:
                                        okLines.AddRange(conflictBlock.LeftLines);
                                        Console.Out.WriteLine("Force-resolved conflicting changes, using local version.");
                                        break;
                                    case ForcedConflictResolutionDirection.Remote:
                                        okLines.AddRange(conflictBlock.RightLines);
                                        Console.Out.WriteLine("Force-resolved conflicting changes, using remote version.");
                                        break;
                                    default:
                                        throw new NotImplementedException("Unknown conflict resolution strategy requested");
                                        break;
                                }
                            }
                            else
                            {
                                //TODO: add friendlier conflict resolution, eg including some context.
                                Console.Out.WriteLine("Conflict encountered. Please choose a version to keep ('Local' or 'Remote'), or enter anything else to abort the merge/sync:");
                                Console.Out.WriteLine("---ORIGINAL/OLD:");
                                Console.Out.WriteLine(string.Join(Environment.NewLine, conflictBlock.OldLines));
                                Console.Out.WriteLine("---");
                                Console.Out.WriteLine("---LOCAL:");
                                Console.Out.WriteLine(string.Join(Environment.NewLine, conflictBlock.LeftLines));
                                Console.Out.WriteLine("---");
                                Console.Out.WriteLine("---REMOTE:");
                                Console.Out.WriteLine(string.Join(Environment.NewLine, conflictBlock.RightLines));
                                Console.Out.WriteLine("---");
                                Console.Out.WriteLine("Which version would you like to keep, 'Local' or 'Remote'? ");
                                string answer = Console.ReadLine();

                                if (string.Compare(answer, ForcedConflictResolutionDirection.Local.ToString(), true) == 0)
                                    okLines.AddRange(conflictBlock.LeftLines);
                                else if (string.Compare(answer, ForcedConflictResolutionDirection.Remote.ToString(), true) == 0)
                                    okLines.AddRange(conflictBlock.RightLines);
                                else
                                {
                                    Console.Out.WriteLine("Neither 'Local' nor 'Remote' was provided; aborting merge/sync.");
                                    return 1;
                                }
                            }
                        }
                    }

                    mergeResult = okLines.ToArray();
                }

                //CHANGE ANALYSIS
                // report the changes we are making at a high level - 3-way merge process is hard to 
                // analyse, so just use regular 2-way diff on "original to new" for local and remote 
                // separately.
                mergeResultInfo = new MergeResultInfo();
                foreach (var localChange in SynchrotronNet.Diff.diff_patch(targetFileStrings, mergeResult))
                {
                    if (localChange.file1.Length > 0 && localChange.file2.Length == 0)
                        mergeResultInfo.DeletionFromList1 = true;

                    if (localChange.file1.Length > 0 && localChange.file2.Length > 0)
                        mergeResultInfo.ModificationToList1 = true;

                    if (localChange.file1.Length == 0 && localChange.file2.Length > 0)
                        mergeResultInfo.AdditionToList1 = true;
                }

                foreach (var remoteChange in SynchrotronNet.Diff.diff_patch(importedTaskStringArray, mergeResult))
                {
                    if (remoteChange.file1.Length > 0 && remoteChange.file2.Length == 0)
                        mergeResultInfo.DeletionFromList2 = true;

                    if (remoteChange.file1.Length > 0 && remoteChange.file2.Length > 0)
                        mergeResultInfo.ModificationToList2 = true;

                    if (remoteChange.file1.Length == 0 && remoteChange.file2.Length > 0)
                        mergeResultInfo.AdditionToList2 = true;
                }

                //get a resulting task list
                resultList = new TaskList(string.Join(Environment.NewLine, mergeResult));
            }

            if (!mergeResultInfo.AnyChange)
            {
                Console.Out.WriteLine("Nothing to do!");
            }
            else
            {
                if (mergeResultInfo.DeletionFromList2) Console.Out.WriteLine("Deleting remote task(s)");
                if (mergeResultInfo.ModificationToList2) Console.Out.WriteLine("Updating remote task(s)");
                if (mergeResultInfo.AdditionToList2) Console.Out.WriteLine("Adding remote task(s)");

                if (mergeResultInfo.DeletionFromList2 || mergeResultInfo.ModificationToList2 || mergeResultInfo.AdditionToList2)
                    //save to google
                    HeapsTodoSyncLib.GoogleSync.SaveTodoTxtTaskListToGoogleTasks(listName, clientID, clientSecret, resultList);

                if (mergeResultInfo.DeletionFromList1) Console.Out.WriteLine("Deleting local task(s)");
                if (mergeResultInfo.ModificationToList1) Console.Out.WriteLine("Updating local task(s)");
                if (mergeResultInfo.AdditionToList1) Console.Out.WriteLine("Adding local task(s)");

                if (mergeResultInfo.DeletionFromList1 || mergeResultInfo.ModificationToList1 || mergeResultInfo.AdditionToList1)
                    //save to local
                    File.WriteAllText(syncTargetFile, resultList.PrintList());

                Console.Out.WriteLine("Sync Complete!");
            }

            //save local cache as 3-way merge base for future sync (always save on success, even if no changes made)
            File.WriteAllText(googleSyncCacheFileName, resultList.PrintList());

            //return all happy
            return 0;
        }

        private static string CleanListForPath(string inputString)
        {
            //from stackoverflow: http://stackoverflow.com/a/146162/74296
            // bad perf, who cares.
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
                inputString = inputString.Replace(c.ToString(), "");
            return inputString;
        }

        private static bool Confirm(string question, bool noInteractivity)
        {
            if (noInteractivity)
                return false;

            Console.Write(question + " (Y/N): ");
            string answerLine = Console.ReadLine();
            if (answerLine != null && string.Compare(answerLine, "Y", true) == 0)
                return true;
            else if (answerLine != null && string.Compare(answerLine, "N", true) == 0)
                return false;
            else
            {
                Console.Error.WriteLine("Response not understood. please try again: ");
                return Confirm(question, noInteractivity);
            }
        }
    }
}
