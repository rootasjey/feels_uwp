using Feels.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Feels.Services {
    public static class BackgroundTasks {
        #region keys

        private static string _PrimaryTileTaskName {
            get {
                return "PrimaryTileTask";
            }
        }

        private static string _PrimaryTileTaskEntryPoint {
            get {
                return "Tasks.PrimaryTileTask";
            }
        }

        private static string _SecondaryTileTaskEntryPoint {
            get {
                return "Tasks.SecondaryTileTask";
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

        private static string _TileTaskInterval {
            get {
                return "TileTaskInterval";
            }
        }

        private static string _TileTaskActivity {
            get {
                return "TileUpdaterTaskActivity";
            }
        }

        private static string _TileTaskGeolocalizedName {
            get {
                return "Geolocalized";
            }
        }

        #endregion keys

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
        public static string GetPrimaryTileTaskName() {
            return _PrimaryTileTaskName;
        }

        public static void RegisterPrimaryTileTask(uint interval) {
            var name = GetPrimaryTileTaskName();
            RegisterBackgroundTask(name, _PrimaryTileTaskEntryPoint, interval);
        }

        public static void RegisterPrimaryTileTask() {
            var name = GetPrimaryTileTaskName();
            var interval = GetTileTaskInterval();

            RegisterBackgroundTask(name, _PrimaryTileTaskEntryPoint, interval);
        }

        public static void UnregisterPrimaryTileTask() {
            string name = GetPrimaryTileTaskName();
            UnregisterBackgroundTask(name);
        }

        public static uint GetTileTaskInterval() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(_TileTaskInterval) ? 
                (uint)settingsValues[_TileTaskInterval] : 60;
        }

        public static ApplicationDataCompositeValue GetTileTaskActivity() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            return settingsValues.ContainsKey(_TileTaskActivity) ? 
                (ApplicationDataCompositeValue)settingsValues[_TileTaskActivity] : null;
        }

        public static bool IsTileTaskActivated(string taskName) {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == taskName) {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPrimaryTaskActivated() {
            var name = GetPrimaryTileTaskName();
            return IsTileTaskActivated(name);
        }

        public static void RegisterSecondaryTileTask(string locationName) {
            var taskName = TileDesigner.ConvertLocationNameToTileId(locationName);
            RegisterBackgroundTask(taskName, _SecondaryTileTaskEntryPoint);
        }

        public static void RegisterSecondaryTileTask(LocationItem location) {
            RegisterSecondaryTileTask(location.Name);
        }

        public static void UnregisterSecondaryTileTask(string taskName) {
            UnregisterBackgroundTask(taskName);
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

        public static async void CheckAllTasks() {
            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (TileDesigner.IsSecondaryTileExist(task.Value.Name)) {
                    continue;
                }

                task.Value.Unregister(false);
                await Settings.DeleteSecondaryTaskLocation(task.Value.Name);
            }
        }

        public static List<IBackgroundTaskRegistration> GetAllTasks() {
            var tasksList = new List<IBackgroundTaskRegistration>();

            foreach (var task in BackgroundTaskRegistration.AllTasks) {
                if (task.Value.Name == _PrimaryTileTaskName) continue;
                tasksList.Add(task.Value);
            }

            return tasksList;
        }

        #endregion tasks
    }
}