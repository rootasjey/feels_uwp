using System;
using Windows.ApplicationModel.Background;

namespace Feels.Services {
    public static class BackgroundTasks {
        static string _taskName = "WeatherTask";
        static string _entryPoint = "Tasks.WeatherTask";

        static string _backgroundTaskName = "LockScreenUpdater";
        static string _backgroundEntryPoint = "OptimizedTasks.LockScreenUpdater";

        public static bool IsLockscreenTaskActivated() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == GetTaskBackgroundName()) {
                    return true;
                }
            }
            return false;
        }

        public static void RegisterQuoteTask() {
            RegisterBackgroundTask(GetTaskQuoteName(), GetTaskQuoteEntryPoint());
        }

        public static void UnregisterQuoteTask() {
            UnregisterBackgroundTask(GetTaskQuoteName());
        }

        public static void RegisterLockscreenTask() {
            RegisterBackgroundTask(GetTaskBackgroundName(), GetTaskBackgroundEntryPoint());
        }

        public static void UnregisterLockscreenTask() {
            UnregisterBackgroundTask(GetTaskBackgroundName());
        }

        public static bool IsQuoteTaskActivated() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == GetTaskQuoteName()) {
                    return true;
                }
            }
            return false;
        }

        private static async void RegisterBackgroundTask(string taskName, string entryPoint) {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == taskName) {
                    return;
                }
            }

            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.DeniedBySystemPolicy ||
                status == BackgroundAccessStatus.DeniedByUser) {
                return; // show message that task couldn't be registered
            }

            var builder = new BackgroundTaskBuilder() {
                Name = taskName,
                TaskEntryPoint = entryPoint
            };
            builder.SetTrigger(new TimeTrigger(60, false));
            BackgroundTaskRegistration taskRegistered = builder.Register();
        }

        public static void UnregisterBackgroundTask(string taskName) {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == taskName) {
                    BackgroundExecutionManager.RemoveAccess();
                    task.Value.Unregister(false);
                    break;
                }
            }
        }

        public static string GetTaskQuoteName() {
            return _taskName;
        }

        public static string GetTaskQuoteEntryPoint() {
            return _entryPoint;
        }

        public static string GetTaskBackgroundName() {
            return _backgroundTaskName;
        }

        public static string GetTaskBackgroundEntryPoint() {
            return _backgroundEntryPoint;
        }

    }
}