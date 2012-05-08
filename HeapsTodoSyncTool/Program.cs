using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using NDesk.Options;
using HeapsTodoLib;

namespace HeapsTodoSyncTool
{
    class Program
    {
        static int Main(string[] args)
        {
            string syncTargetFile = null;
            string syncTargetTypeCode = null;
            string listName = null;
            bool showUsageFriendly = false;
            bool showUsageError = false;

            OptionSet p = new OptionSet()
              .Add("t|syncType=", delegate(string v) { syncTargetTypeCode = v; })
              .Add("n|listName=", delegate(string v) { listName = v; })
              .Add("h|?|help", delegate(string v) { showUsageFriendly = v != null; })
                  ;

            //first parse the args
            List<string> remainingArgs = p.Parse(args);

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

            if (showUsageFriendly || showUsageError)
            {
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

            string clientID = "1053655555490.apps.googleusercontent.com";
            string clientSecret = "UUJ3kv-i5l2PdBpC23tBUHsz";

            //TODO: TEMPORARY SIMPLIFICATION!!
            // really, most of this is part of the sync logic and should be implemented with callbacks for UI interaction
            // also progress indicator and cancellation option?

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
                if (Confirm("The Google Tasks list you requested was not found in your Google Tasks account - would you like to create it?"))
                {
                    importedlist = new TaskList();
                    HeapsTodoSyncLib.GoogleSync.CreateGoogleTasksList(listName, clientID, clientSecret, importedlist);
                    newGoogleList = true;
                }
                else
                {
                    return 1;
                }
            }

            //TODO: remove debugging
            Console.WriteLine(importedlist.PrintList());

            if (!File.Exists(syncTargetFile))
            {
                if (Confirm("The provided todo filename was not found. Would you like to create it?"))
                {
                    //TODO: make  decision as to whether this is a todo.txt or a heapstodo file.
                    File.WriteAllText(syncTargetFile, "", Encoding.UTF8);
                    newLocalFile = true;
                }
                else
                {
                    return 1;
                }
            }



            if (!newGoogleList && !newLocalFile && !File.Exists(syncTargetFile + "." + Path. CleanUp(listName) + ".htdgsync"))
            {
                if (!Confirm("This appears to be the first time you are synchronizing this file with this list. Any identical entries will be merged, and any entries that differ or are unique will be included in both lists. Would you like to continue?"))
                    return 1;
            }

            //TODO: implement Merge
            //TODO: make sure it handles straight 2-way also.

            //TODO: handle merge conflict

            //return all happy
            return 0;
        }

        private static string CleanListForPath(string inputString)
        {
            //from stackoverflow: http://stackoverflow.com/a/146162/74296
            // bad perf, who cares.
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {
                inputString = inputString.Replace(c.ToString(), "");
            }
        }

        private static bool Confirm(string question)
        {
            Console.WriteLine(question + " (Y/N)");
            string answerLine = Console.ReadLine();
            if (answerLine != null && string.Compare(answerLine, "Y") == 0)
                return true;
            else if (answerLine != null && string.Compare(answerLine, "N") == 0)
                return false;
            else
            {
                Console.Error.WriteLine("Response not understood. please try again:");
                return Confirm(question);
            }
        }
    }
}
