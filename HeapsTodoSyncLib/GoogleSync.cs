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

        public static void Sync(TaskList currentTasks, string sharedAncestorFilename)
        {
            throw new NotImplementedException();
            //System.Windows.MessageBox.Show("Found Tasks: " + ConvertGoogleTasksToTodoTxtString());
        }

        public static TaskList ConvertGoogleTasksToTodoTxtTaskList(string listName, string clientIdentifier, string clientSecret)
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
            List<GoogleTask> outList = new List<Google.Apis.Tasks.v1.Data.Task>();
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

        public static void CreateGoogleTasksList(string listName, string clientIdentifier, string clientSecret, TaskList taskList)
        {
            TasksService tasksService = GetTasksService(clientIdentifier, clientSecret);
            var existingList = tasksService.Tasklists.List().Fetch().Items.SingleOrDefault(tl => tl.Title == listName);
            if (existingList != null)
                throw new Exception("There is already a list with the provided name in this Google Tasks account");

            GoogleTaskList newList = new Google.Apis.Tasks.v1.Data.TaskList();
            newList.Title = listName;
            tasksService.Tasklists.Insert(newList).Fetch();

            SaveTodoTxtTaskListToGoogleTasks(listName, clientIdentifier, clientSecret, taskList);
        }

        public static void SaveTodoTxtTaskListToGoogleTasks(string listName, string clientIdentifier, string clientSecret, TaskList taskList)
        {
            TasksService tasksService = GetTasksService(clientIdentifier, clientSecret);
            var targetList = GetList(tasksService, listName);
            BiDictionary<GoogleTask, Task> googleTaskMapping = null;
            var targetTasks = GetTasks(tasksService, targetList.Id);
            var localListEquivalentToCurrentGList = gTasksIListToLocalTaskList(targetTasks, out googleTaskMapping);

            RecursivelyUpdateGTasks(tasksService, targetList, taskList, googleTaskMapping, localListEquivalentToCurrentGList, null);
        }

        private static GoogleTask RecursivelyUpdateGTasks(TasksService tasksService, GoogleTaskList gTaskList, IList<Task> taskList, BiDictionary<GoogleTask, Task> gTaskToConvertedMapping, IList<Task> convertedRemoteTaskList, GoogleTask parentGTask)
        {
            GoogleTask lastGoogleTaskUpdated = null;
            foreach (var task in taskList)
            {
                int possibleMatchID = convertedRemoteTaskList.FindIndex(t => t.MainBody == task.MainBody);
                if (possibleMatchID >= 0)
                {
                    GoogleTask matchingTask = null;
                    gTaskToConvertedMapping.TryGetBySecond(convertedRemoteTaskList[possibleMatchID], out matchingTask);

                    if (possibleMatchID > 0)
                    {
                        //if we found a match, but it is not at the top of the current list, then move it to be;
                        // Same parent, but new position.
                        var moveRequest = tasksService.Tasks.Move(gTaskList.Id, matchingTask.Id);
                        moveRequest.Parent = matchingTask.Parent;
                        if (lastGoogleTaskUpdated == null)
                            moveRequest.Previous = lastGoogleTaskUpdated.Id;
                        moveRequest.Fetch();
                    }

                    if (task.PrintTask(true) != convertedRemoteTaskList[possibleMatchID].PrintTask(true))
                    {
                        //if there is a difference between the task here and the just-imported google task (including subtasks), overwrite the google task.
                        updateGTask(tasksService, gTaskList, matchingTask, task, gTaskToConvertedMapping, convertedRemoteTaskList[possibleMatchID]);
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

        private static void RecursivelyDeleteGTask(TasksService tasksService, GoogleTaskList gTaskList, BiDictionary<GoogleTask, Task> gTaskToConvertedMapping, Task straggler)
        {
            //first delete children (recursively)
            foreach (Task subStraggler in straggler.SubTasks)
                RecursivelyDeleteGTask(tasksService, gTaskList, gTaskToConvertedMapping, subStraggler);

            //then delete self.
            GoogleTask gStraggler = null;
            gTaskToConvertedMapping.TryGetBySecond(straggler, out gStraggler);
            if (gStraggler != null)
                tasksService.Tasks.Delete(gTaskList.Id, gStraggler.Id).Fetch();
            else
                throw new Exception("Unexpected error: Google Task to local Task mapping dictionary did not contain expected entry");
        }

        private static GoogleTask createGTask(TasksService tasksService, GoogleTaskList gTaskList, GoogleTask previousTask, GoogleTask parentTask, Task task)
        {
            var gTask = new GoogleTask();

            SetGTaskBasicPropertiesFromTask(gTask, task);
            
            //insert the new Google Task
            var insertRequest = tasksService.Tasks.Insert(gTask, gTaskList.Id);
            if (parentTask != null)
                insertRequest.Parent = parentTask.Id;
            if (previousTask != null)
                insertRequest.Previous = previousTask.Id;
            gTask = insertRequest.Fetch();

            //create any subtasks
            GoogleTask lastChildSoFar = null;
            foreach (Task sub in task.SubTasks)
                lastChildSoFar = createGTask(tasksService, gTaskList, lastChildSoFar, gTask, sub);

            //all done
            return gTask;
        }

        private static GoogleTask updateGTask(TasksService tasksService, GoogleTaskList gTaskList, GoogleTask gTask, Task task, BiDictionary<GoogleTask, Task> googleTaskMapping, Task matchedTask)
        {
            if (task.PrintTask(false) != matchedTask.PrintTask(false))
            {
                SetGTaskBasicPropertiesFromTask(gTask, task);
                tasksService.Tasks.Update(gTask, gTaskList.Id, gTask.Id).Fetch();
            }
            RecursivelyUpdateGTasks(tasksService, gTaskList, task.SubTasks, googleTaskMapping, matchedTask.SubTasks, gTask);
            return gTask;
        }

        private static void SetGTaskBasicPropertiesFromTask(GoogleTask gTask, Task task)
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

            gTask.Notes = task.Notes;
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

        private static TaskList gTasksIListToLocalTaskList(IList<GoogleTask> gTasksIList)
        {
            BiDictionary<GoogleTask, Task> dummy = null;
            return gTasksIListToLocalTaskList(gTasksIList, out dummy);
        }

        private static TaskList gTasksIListToLocalTaskList(IList<GoogleTask> gTasksIList, out BiDictionary<GoogleTask, Task> googleTaskMapping)
        {
            var outList = new TaskList();
            googleTaskMapping = new BiDictionary<GoogleTask, Task>();

            foreach (var remoteTask in gTasksIList)
            {
                var localTask = new Task();
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
