using System;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Feels.Services {
    public static class BackgroundTasks {
        #region variables
        private static string _TileTaskName {
            get {
                return "UpdateWeather";
            }
        }

        private static string _TileTaskEntryPoint {
            get {
                return "Tasks.UpdateWeather";
            }
        }

        private static string _LockscreenTaskName {
            get {
                return "LockScreenUpdater";
            }
        }

        private static string _LockscreenTaskEntryPoint {
            get {
                return "OptimizedTasks.LockScreenUpdater";
            }
        }

        private static string TileTaskInterval {
            get {
                return "TileTaskInterval";
            }
        }
        #endregion variables

        #region lockscreen task
        public static bool IsLockscreenTaskActivated() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == _LockscreenTaskName) {
                    return true;
                }
            }
            return false;
        }

        public static void RegisterLockscreenTask() {
            RegisterBackgroundTask(_LockscreenTaskName, _LockscreenTaskEntryPoint);
        }

        public static void UnregisterLockscreenTask() {
            UnregisterBackgroundTask(_LockscreenTaskName);
        }

        #endregion lockscreen task

        #region tile task
        public static void RegisterTileTask(uint interval) {
            RegisterBackgroundTask(_TileTaskName, _TileTaskEntryPoint, interval);
        }

        public static void UnregisterTileTask() {
            UnregisterBackgroundTask(_TileTaskName);
        }

        public static uint GetTileTaskInterval() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(TileTaskInterval) ? (uint)settingsValues[TileTaskInterval] : 60;
        }

        public static ApplicationDataCompositeValue GetTileTaskActivity() {
            var key = "TileUpdaterTask" + "Activity";
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(key) ? (ApplicationDataCompositeValue)settingsValues[key] : null;
        }

        public static bool IsTileTaskActivated() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == _TileTaskName) {
                    return true;
                }
            }
            return false;
        }

        #endregion tile task


        #region tasks
        private static async void RegisterBackgroundTask(string taskName, string entryPoint, uint interval = 60) {
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
            builder.SetTrigger(new TimeTrigger(interval, false));
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

        #endregion tasks
    }
}