using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization;

using HeapsTodoLib;
using Google.Apis.Tasks.v1;
using GoogleTask = Google.Apis.Tasks.v1.Data.Task;
using GoogleTaskList = Google.Apis.Tasks.v1.Data.TaskList;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Util;
using Google.Apis.Samples.Helper;
using DotNetOpenAuth.OAuth2;

namespace HeapsTodoSyncLib
{
    public class GoogleSync
    {
        public static DateTime UNKNOWN_COMPLETION_DATE = new DateTime(2000, 1, 1);

        public static void Sync<T>(ITaskList1<T> currentTasks, string sharedAncestorFilename)
        {
            throw new NotImplementedException();
            //System.Windows.MessageBox.Show("Found Tasks: " + ConvertGoogleTasksToTodoTxtString());
        }

        public static HeapsTodoTaskList ConvertGoogleTasksToTodoTxtTaskList(string listName, string clientIdentifier, string clientSecret)
        {
            TasksService tasksService = GetTasksService(clientIdentifier, clientSecret);
            var targetList = GetList(tasksService, listName);
            return gTasksIListToLocalTaskList(GetTasks(tasksService, targetList.Id));
        }

        private static GoogleTaskList GetList(TasksService tasksService, string listName)
        {
            var targetList = tasksService.Tasklists.List().Fetch().Items.SingleOrDefault(tl => tl.Title == listName);
            if (targetList != null)
                return targetList;
            else
                throw new MissingListException();
        }

        private static IList<GoogleTask> GetTasks(TasksService tasksService, string listID)
        {
            List<GoogleTask> outList = new List<GoogleTask>();
            string nextPageToken = null;
            while (true)
            {
                //defaults: 
                // - page to 100 tasks per requests (but we encapsulate this)
                // - include completed tasks
                // - don't include deleted or hidden (what's the difference??)

                var listRequest = tasksService.Tasks.List(listID);
                if (nextPageToken != null)
                    listRequest.PageToken = nextPageToken;

                var response = listRequest.Fetch();
                if (response.Items != null)
                    outList.AddRange(response.Items);

                if (response.NextPageToken != null)
                    nextPageToken = response.NextPageToken;
                else
                    break;
            }

            return outList;
        }

        public static void CreateGoogleTasksList<T>(string listName, string clientIdentifier, string clientSecret, ITaskList1<T> taskList) where T : ITask
        {
            TasksService tasksService = GetTasksService(clientIdentifier, clientSecret);
            var existingList = tasksService.Tasklists.List().Fetch().Items.SingleOrDefault(tl => tl.Title == listName);
            if (existingList != null)
                throw new Exception("There is already a list with the provided name in this Google Tasks account");

            GoogleTaskList newList = new Google.Apis.Tasks.v1.Data.TaskList();
            newList.Title = listName;
            tasksService.Tasklists.Insert(newList).Fetch();

            SaveTodoTxtTaskListToGoogleTasks<T>(listName, clientIdentifier, clientSecret, taskList);
        }

        public static void SaveTodoTxtTaskListToGoogleTasks<T>(string listName, string clientIdentifier, string clientSecret, ITaskList1<T> taskList) where T : ITask
        {
            TasksService tasksService = GetTasksService(clientIdentifier, clientSecret);
            var targetList = GetList(tasksService, listName);
            BiDictionary<GoogleTask, HeapsTodoTask> googleTaskMapping = null;
            var targetTasks = GetTasks(tasksService, targetList.Id);
            var localListEquivalentToCurrentGList = gTasksIListToLocalTaskList(targetTasks, out googleTaskMapping);

            RecursivelyUpdateGTasks<T>(tasksService, targetList, taskList, googleTaskMapping, localListEquivalentToCurrentGList, null);
        }

        private static GoogleTask RecursivelyUpdateGTasks<T>(TasksService tasksService, GoogleTaskList gTaskList, IList<T> taskList, BiDictionary<GoogleTask, HeapsTodoTask> gTaskToConvertedMapping, IList<HeapsTodoTask> convertedRemoteTaskList, GoogleTask parentGTask) where T: ITask
        {
            //TODO: figure out how to do this without the horrible hack of duplicating the method.
            // The problem is the taskList argument, which need to work for both "IList<HeapsTodoTask>" and "IList<ITask>" types.
            GoogleTask lastGoogleTaskUpdated = null;
            foreach (ITask task in taskList)
            {
                int possibleMatchID = convertedRemoteTaskList.FindIndex(t => t.MainBody == task.MainBody);
                if (possibleMatchID >= 0)
                {
                    //TODO: the moving logic should use the same logic as "thisTaskIsDifferent" stuff in update:
                    // structured task collapse for comparison.

                    GoogleTask matchingTask = null;
                    gTaskToConvertedMapping.TryGetBySecond(convertedRemoteTaskList[possibleMatchID], out matchingTask);

                    if (possibleMatchID > 0)
                    {
                        //TODO: remove or qualify.
                        System.Diagnostics.Debug.Print("Moving Google Task.");
                        System.Diagnostics.Debug.Print("  Details: " + task.PrintTask());
                        if (lastGoogleTaskUpdated != null)
                            System.Diagnostics.Debug.Print("  Moving To After: " + lastGoogleTaskUpdated.Title);

                        //if we found a match, but it is not at the top of the current list, then move it to be;
                        // Same parent, but new position.
                        var moveRequest = tasksService.Tasks.Move(gTaskList.Id, matchingTask.Id);
                        moveRequest.Parent = matchingTask.Parent;
                        if (lastGoogleTaskUpdated != null)
                            moveRequest.Previous = lastGoogleTaskUpdated.Id;
                        var movedgTask = moveRequest.Fetch();
                    }

                    if (task.PrintTask() != convertedRemoteTaskList[possibleMatchID].PrintTask())
                    {
                        //if there is a difference between the task here and the just-imported google task (including any subtasks), overwrite the google task.
                        matchingTask = updateGTask(tasksService, gTaskList, matchingTask, task, gTaskToConvertedMapping, convertedRemoteTaskList[possibleMatchID]);
                    }

                    convertedRemoteTaskList.RemoveAt(possibleMatchID);
                    lastGoogleTaskUpdated = matchingTask;
                }
                else
                {
                    lastGoogleTaskUpdated = createGTask(tasksService, gTaskList, lastGoogleTaskUpdated, parentGTask, task);
                }
            }

            foreach (var straggler in convertedRemoteTaskList)
                RecursivelyDeleteGTask(tasksService, gTaskList, gTaskToConvertedMapping, straggler);

            return lastGoogleTaskUpdated;
        }

        private static void RecursivelyDeleteGTask(TasksService tasksService, GoogleTaskList gTaskList, BiDictionary<GoogleTask, HeapsTodoTask> gTaskToConvertedMapping, HeapsTodoTask straggler)
        {
            //first delete children (recursively)
            foreach (HeapsTodoTask subStraggler in straggler.SubTasks)
                RecursivelyDeleteGTask(tasksService, gTaskList, gTaskToConvertedMapping, subStraggler);

            //TODO: remove or qualify.
            System.Diagnostics.Debug.Print("Deleting Google Task.");
            System.Diagnostics.Debug.Print("  Details: " + straggler.PrintTask());

            //then delete self.
            GoogleTask gStraggler = null;
            gTaskToConvertedMapping.TryGetBySecond(straggler, out gStraggler);
            if (gStraggler != null)
                tasksService.Tasks.Delete(gTaskList.Id, gStraggler.Id).Fetch();
            else
                throw new Exception("Unexpected error: Google Task to local Task mapping dictionary did not contain expected entry");
        }

        private static GoogleTask createGTask(TasksService tasksService, GoogleTaskList gTaskList, GoogleTask previousTask, GoogleTask parentTask, ITask task)
        {
            var gTask = new GoogleTask();

            //TODO: remove or qualify.
            System.Diagnostics.Debug.Print("Creating Google Task.");
            System.Diagnostics.Debug.Print("  Details: " + task.PrintTask());

            SetGTaskBasicPropertiesFromTask(gTask, task);
            
            //insert the new Google Task
            var insertRequest = tasksService.Tasks.Insert(gTask, gTaskList.Id);
            if (parentTask != null)
                insertRequest.Parent = parentTask.Id;
            if (previousTask != null)
                insertRequest.Previous = previousTask.Id;
            gTask = insertRequest.Fetch();


            //create any subtasks
            if (task is HeapsTodoTask)
            {
                GoogleTask lastChildSoFar = null;
                foreach (HeapsTodoTask sub in ((HeapsTodoTask)task).SubTasks)
                    lastChildSoFar = createGTask(tasksService, gTaskList, lastChildSoFar, gTask, sub);
            }

            //all done
            return gTask;
        }

        private static GoogleTask updateGTask(TasksService tasksService, GoogleTaskList gTaskList, GoogleTask gTask, ITask task, BiDictionary<GoogleTask, HeapsTodoTask> googleTaskMapping, HeapsTodoTask matchedTask)
        {
            bool thisTaskIsDifferent = false;
            if (task is HeapsTodoTask)
            {
                thisTaskIsDifferent = (((HeapsTodoTask)task).PrintTask(false) != matchedTask.PrintTask(false));
            }
            else
            {
                thisTaskIsDifferent = task.PrintTask() != TodoTxtTaskList.ConvertFromHeapsTodo(matchedTask).PrintTask();
            }

            if (thisTaskIsDifferent)
            {
                //TODO: remove or qualify.
                System.Diagnostics.Debug.Print("Updating Google Task.");
                System.Diagnostics.Debug.Print("  Before: " + matchedTask.PrintTask());
                System.Diagnostics.Debug.Print("  After: " + task.PrintTask());

                //We need to get latest version of task before updating, because 
                // 1) other list operations (moving this task or more importantly moving OTHER tasks) might 
                //  have changed this task's position (and etag), which would normally cause the Google Tasks API 
                //  to complain about mismatching etags, and 
                // 2) we actually had to disable google's etag stuff as it didn't seem to be working right 
                //  (it was detecting differences even when the task was JUST retrieved, here!). With this
                //  disabled, we manually have to check for any concurrent-editing changes at the last minute.
                GoogleTask newestCopyOfGTask = tasksService.Tasks.Get(gTaskList.Id, gTask.Id).Fetch();
                string unexpectedDifferences = GoogleTaskDifferences(gTask, newestCopyOfGTask, true);
                if (string.IsNullOrEmpty(unexpectedDifferences))
                {
                    gTask = newestCopyOfGTask;
                    SetGTaskBasicPropertiesFromTask(gTask, task);
                    try
                    {
                        var updateRequest = tasksService.Tasks.Update(gTask, gTaskList.Id, gTask.Id);
                        //simply couldn't get google's ETag stuff to work reliably, had to bypass it 
                        // and implement cncurrency checking manually in the end.
                        updateRequest.ETagAction = Google.Apis.ETagAction.Ignore;
                        updateRequest.Fetch();
                    }
                    catch (Exception e)
                    {
                        //for debugging breakpoint only - this catch block has no functional effect.
                        throw;
                    }
                }
                else
                {
                    throw new Exception("Unexpected difference found on refreshed task - task must have been changed by another system during sync.");
                }
            }
            if (task is HeapsTodoTask)
                RecursivelyUpdateGTasks(tasksService, gTaskList, ((HeapsTodoTask)task).SubTasks, googleTaskMapping, matchedTask.SubTasks, gTask);

            return gTask;
        }

        private static void SetGTaskBasicPropertiesFromTask(GoogleTask gTask, ITask task)
        {
            gTask.Title = task.MainBody;

            if (task.CompletionDate != null)
                gTask.Completed = task.CompletionDate.Value.ToRFC3339();
            else if (task.Completed)
                gTask.Completed = UNKNOWN_COMPLETION_DATE.ToRFC3339();
            else 
                gTask.Completed = null;

            if (!string.IsNullOrEmpty(gTask.Completed))
                gTask.Status = "completed";
            else
                gTask.Status = "needsAction";

            if (task.DueDate != null)
                gTask.Due = task.DueDate.Value.ToRFC3339();
            else
                gTask.Due = null;

            if (task is HeapsTodoTask)
                gTask.Notes = ((HeapsTodoTask)task).Notes;
        }

        private static TasksService GetTasksService(string clientIdentifier, string clientSecret)
        {
            var provider = new NativeApplicationClient(GoogleAuthenticationServer.Description);
            provider.ClientIdentifier = clientIdentifier;
            provider.ClientSecret = clientSecret;
            var authenticator = new OAuth2Authenticator<NativeApplicationClient>(provider, GetAuthorization);
            TasksService tasksService = new TasksService(authenticator);
            return tasksService;
        }

        private static IAuthorizationState GetAuthorization(NativeApplicationClient client)
        {
            string scope = TasksService.Scopes.Tasks.GetStringValue();
            byte[] clientIDBytes = Encoding.UTF8.GetBytes(client.ClientIdentifier);

            // Check if there is a cached refresh token available.
            IAuthorizationState cachedState = null;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.CachedGoogleAPISessionKey))
            {
                try
                {
                    byte[] cachedSecretBytes = System.Convert.FromBase64String(Properties.Settings.Default.CachedGoogleAPISessionKey);
                    byte[] decryptedCachedKeyContent = ProtectedData.Unprotect(cachedSecretBytes, clientIDBytes, DataProtectionScope.CurrentUser);
                    string decryptedCachedKey = Encoding.UTF8.GetString(decryptedCachedKeyContent);
                    cachedState = new AuthorizationState(new[] { scope }) { RefreshToken = decryptedCachedKey };
                }
                catch (CryptographicException)
                {
                    //if there was a problem retrieving cached details, ignore.
                }
            }

            if (cachedState != null)
            {
                try
                {
                    client.RefreshToken(cachedState, null);
                    return cachedState;
                }
                catch (DotNetOpenAuth.Messaging.ProtocolException)
                {
                    //for now, ignore error and move on to getting new auth
                    cachedState = null;
                }
            }

            //no valid state cached, so get new auth - Google's API samples helper tries to read token by listening to 
            // localhost OR using browser's window title... If auth fails, an exception is raised.
            cachedState = AuthorizationMgr.RequestNativeAuthorization(client, scope);

            //if we got this far, then the auth must have succeeded - so cache it.
            byte[] keyContent = Encoding.UTF8.GetBytes(cachedState.RefreshToken);
            byte[] encryptedKeyContent = ProtectedData.Protect(keyContent, clientIDBytes, DataProtectionScope.CurrentUser);
            Properties.Settings.Default.CachedGoogleAPISessionKey = System.Convert.ToBase64String(encryptedKeyContent);
            Properties.Settings.Default.Save();

            //and return it.
            return cachedState;
        }

        private static HeapsTodoTaskList gTasksIListToLocalTaskList(IList<GoogleTask> gTasksIList)
        {
            BiDictionary<GoogleTask, HeapsTodoTask> dummy = null;
            return gTasksIListToLocalTaskList(gTasksIList, out dummy);
        }

        private static HeapsTodoTaskList gTasksIListToLocalTaskList(IList<GoogleTask> gTasksIList, out BiDictionary<GoogleTask, HeapsTodoTask> googleTaskMapping)
        {
            var outList = new HeapsTodoTaskList();
            googleTaskMapping = new BiDictionary<GoogleTask, HeapsTodoTask>();

            foreach (var remoteTask in gTasksIList)
            {
                var localTask = new HeapsTodoTask();
                localTask.MainBody = remoteTask.Title;
                localTask.Notes = remoteTask.Notes;

                //embedded values get appended to the body (separated by a space) if they don't already exist.
                //TODO: if they mismatch, then we don't know what to do yet. (will get to that)

                if (!string.IsNullOrEmpty(remoteTask.Due)
                    && localTask.DueDate == null)
                    localTask.MainBody = localTask.MainBody + " due:" + DateTime.Parse(remoteTask.Due).ToString("yyyy-MM-dd");

                if (!string.IsNullOrEmpty(remoteTask.Due)
                    && DateTime.Parse(remoteTask.Due).Date != localTask.DueDate)
                    throw new NotImplementedException("We don't know how to handle mismatching due dates yet!");

                if (!string.IsNullOrEmpty(remoteTask.Completed))
                {
                    var completionDate = DateTime.Parse(remoteTask.Completed).Date;
                    if (completionDate != UNKNOWN_COMPLETION_DATE)
                        localTask.CompletionDate = completionDate;
                    else
                        localTask.CompletionDate = null;

                    //even if we just set the completion date to null, the task IS completed.
                    localTask.Completed = true;
                }

                if (!string.IsNullOrEmpty(remoteTask.Parent))
                    googleTaskMapping[googleTaskMapping.Keys.Single(k => k.Id == remoteTask.Parent)].SubTasks.Add(localTask);
                else
                    outList.Add(localTask);

                googleTaskMapping.Add(remoteTask, localTask);
            }

            return outList;
        }

        public static string GoogleTaskDifferences(GoogleTask first, GoogleTask second, bool omitPositionCheck)
        {
            StringBuilder result = new StringBuilder();
            if (first == null && second != null)
                result.AppendLine("First is null, second isn't.");
            else if (first != null && second == null)
                result.AppendLine("Second is null, first isn't.");
            else if (first != null && second != null)
            {
                if (first.Completed != second.Completed)
                    result.AppendLine(string.Format("Completions differ: {0} vs {1}.", first.Completed, second.Completed));
                if (first.Deleted != second.Deleted)
                    result.AppendLine(string.Format("Deletions differ: {0} vs {1}.", first.Deleted, second.Deleted));
                if (first.Due != second.Due)
                    result.AppendLine(string.Format("Dues differ: {0} vs {1}.", first.Due, second.Due));
                if (first.Hidden != second.Hidden)
                    result.AppendLine(string.Format("Hiddens differ: {0} vs {1}.", first.Hidden, second.Hidden));
                if (first.Id != second.Id)
                    result.AppendLine(string.Format("Ids differ: {0} vs {1}.", first.Id, second.Id));
                if (first.Notes != second.Notes)
                    result.AppendLine(string.Format("Notes differ: {0} vs {1}.", first.Notes, second.Notes));
                if (first.Parent != second.Parent)
                    result.AppendLine(string.Format("Parents differ: {0} vs {1}.", first.Parent, second.Parent));
                if (first.Position != second.Position && !omitPositionCheck)
                    result.AppendLine(string.Format("Positions differ: {0} vs {1}.", first.Position, second.Position));
                if (first.Status != second.Status)
                    result.AppendLine(string.Format("Statuses differ: {0} vs {1}.", first.Status, second.Status));
                if (first.Title != second.Title)
                    result.AppendLine(string.Format("Titles differ: {0} vs {1}.", first.Title, second.Title));
            }
            return result.ToString();
        }

        [Serializable]
        public class MissingListException : Exception
        {
            //all this code just for a minimal custom exception class. this is one of the ugliest faces of C#.
            public MissingListException()
            {
            }

            public MissingListException(string message)
                : base(message)
            {
            }

            public MissingListException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected MissingListException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }
    }

}
