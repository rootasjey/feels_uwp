using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.System.UserProfile;
using Windows.Devices.Geolocation;
using DarkSkyApi;
using Windows.ApplicationModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Feels.Models;
using Newtonsoft.Json;

namespace Feels.Services {
    public class Settings {
        #region keys
        private static string ThemeKey {
            get {
                return "Theme";
            }
        }

        private static string LanguageKey {
            get {
                return "Language";
            }
        }

        private static string UnitKey {
            get {
                return "Unit";
            }
        }

        private static string FirstLaunchKey {
            get {
                return "FirstLaunch";
            }
        }

        private static string LastPositionKey {
            get {
                return "LastPosition";
            }
        }

        private static string AppVersionKey {
            get {
                return "AppVersion";
            }
        }

        private static string SavedLocationsKey {
            get {
                return "Savedlocations.json";
            }
        }

        private static string FavoriteLocationKey {
            get {
                return "FavoriteLocation";
            }
        }

        private static string PressureUnitKey {
            get {
                return "PressureUnit";
            }
        }

        private static string SceneColorAnimationDeactivatedKey {
            get {
                return "SceneColorAnimationDeactivated";
            }
        }

        private static string PrimaryTileTaskTypeKey {
            get {
                return "PrimaryTileTaskType";
            }
        }

        public static string _GPSTaskTypeKey {
            get {
                return "gps";
            }
        }

        public static string _LocationTaskTypeKey {
            get {
                return "location";
            }
        }

        private static string SavedLocationsTasksKey {
            get {
                return "SavedLocationTasks";
            }
        }

        #endregion keys

        #region position
        public static void SavePosition(BasicGeoposition position) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            var composite = new ApplicationDataCompositeValue {
                ["lat"] = position.Latitude,
                ["lon"] = position.Longitude
            };

            settingsValues[LastPositionKey] = composite;
        }

        public static BasicGeoposition GetLastSavedPosition() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            if (!settingsValues.ContainsKey(LastPositionKey)) return new BasicGeoposition();

            var lastPos = (ApplicationDataCompositeValue)settingsValues[LastPositionKey];

            var coord = new BasicGeoposition() {
                Latitude = (double)lastPos["lat"],
                Longitude = (double)lastPos["lon"]
            };

            return coord;
        }
        #endregion position

        #region language
        public static void SaveAppCurrentLanguage(string language) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[LanguageKey] = language;
        }

        public static string GetAppCurrentLanguage() {
            string defaultLanguage = GlobalizationPreferences.Languages[0];

            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LanguageKey) ? (string)settingsValues[LanguageKey] : defaultLanguage;
        }
        #endregion language

        #region units
        public static void SaveUnit(Unit unit) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[UnitKey] = unit.ToString();
        }

        public static Unit GetUnit() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(UnitKey)) {
                var savedUnit = (string)settingsValues[UnitKey];
                return ConvertToUnit(savedUnit);
            }

            return Unit.SI;
        }

        public static string GetTemperatureUnit() {
            var unit = GetUnit();

            switch (unit) {
                case Unit.US:
                    return "F";
                case Unit.SI:
                    return "C";
                case Unit.CA:
                    return "C";
                case Unit.UK:
                    return "C";
                case Unit.UK2:
                    return "C";
                case Unit.Auto:
                    return "F";
                default:
                    return "F";
            }
        }

        private static Unit ConvertToUnit(string value) {
            if (value == Unit.SI.ToString()) {
                return Unit.SI;
            }

            if (value == Unit.US.ToString()) {
                return Unit.US;
            }

            if (value == Unit.CA.ToString()) {
                return Unit.CA;
            }

            if (value == Unit.UK.ToString()) {
                return Unit.UK;
            }

            if (value == Unit.UK2.ToString()) {
                return Unit.UK2;
            }

            return Unit.SI;
        }

        public static void SavePressureUnit(string unit) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[PressureUnitKey] = unit;
        }

        public static string GetPressureUnit() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(PressureUnitKey)) {
                var savedPressureUnit = (string)settingsValues[PressureUnitKey];
                return savedPressureUnit;
            }

            return null;
        }
        #endregion units

        #region theme
        public static bool IsApplicationThemeLight() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.TryGetValue(ThemeKey, out var previousTheme);
            return ApplicationTheme.Light.ToString() == (string)previousTheme;
        }

        public static void UpdateAppTheme(ApplicationTheme theme) {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.TryGetValue(ThemeKey, out var previousTheme);

            if ((string)previousTheme == theme.ToString()) return;

            localSettings.Values[ThemeKey] = theme.ToString();
            App.UpdateAppTheme();
        }
        #endregion theme

        #region first launch
        public static void SaveFirstLaunchPassed() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[FirstLaunchKey] = false;
        }

        public static bool IsFirstLaunch() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(FirstLaunchKey) ? false : true;
        }
        #endregion first launch

        #region locations
        public static async Task SaveLocationsAsync(List<LocationItem> locations) {
            string json = JsonConvert.SerializeObject(locations);

            StorageFile file =
                await ApplicationData
                        .Current
                        .LocalFolder
                        .CreateFileAsync(SavedLocationsKey, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, json);
        }

        public static async Task<List<LocationItem>> GetSavedLocationAsync() {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(SavedLocationsKey);
            if (file == null) return null;

            string json = await FileIO.ReadTextAsync(file);
            return JsonConvert.DeserializeObject<List<LocationItem>>(json);
        }

        public static async Task SaveFavoriteLocation(LocationItem location) {
            string serializedLocation = JsonConvert.SerializeObject(location);

            StorageFile file =
                await ApplicationData
                        .Current
                        .LocalFolder
                        .CreateFileAsync(FavoriteLocationKey, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, serializedLocation);
        }

        public static async Task<LocationItem> GetFavoriteLocation() {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(FavoriteLocationKey);
            if (file == null) return null;

            string json = await FileIO.ReadTextAsync(file);
            return JsonConvert.DeserializeObject<LocationItem>(json);
        }

        public static async Task DeleteFavoriteLocation() {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(FavoriteLocationKey);
            if (file == null) return;

            await file.DeleteAsync();
        }

        #endregion locations

        #region tasks
        public static void SavePrimaryTileTaskType(string type) {
            var localSettingsValues = ApplicationData.Current.LocalSettings.Values;
            localSettingsValues[PrimaryTileTaskTypeKey] = type;
        }

        public static string GetPrimaryTileTaskType() {
            var localSettingsValues = ApplicationData.Current.LocalSettings.Values;
            return localSettingsValues.ContainsKey(PrimaryTileTaskTypeKey) ? 
                (string)localSettingsValues[PrimaryTileTaskTypeKey] : _GPSTaskTypeKey;
        }

        public static async Task SaveSecondaryTaskLocation(string locationId, LocationItem location) {
            string serializedLocation = JsonConvert.SerializeObject(location);

            StorageFile file =
                await ApplicationData
                        .Current
                        .LocalFolder
                        .CreateFileAsync(locationId, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, serializedLocation);
        }

        public static async Task DeleteSecondaryTaskLocation(string locationId) {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(locationId);
            if (file == null) return;

            await file.DeleteAsync();
        }

        //public static async Task SavePinnedLocationsTasksList(LocationItem location) {
        //    var previousList = await GetPinnedLocationsTaskList();
        //    if (previousList == null) previousList = new List<LocationItem>();

        //    previousList.Add(location);

        //    string json = JsonConvert.SerializeObject(previousList);

        //    StorageFile file =
        //        await ApplicationData
        //                .Current
        //                .LocalFolder
        //                .CreateFileAsync(SavedLocationsTasksKey, CreationCollisionOption.ReplaceExisting);

        //    await FileIO.WriteTextAsync(file, json);
        //}

        //public static async Task RemovePinnedLocationsTasksList(LocationItem location) {
        //    var previousList = await GetPinnedLocationsTaskList();
        //    if (previousList == null) return;

        //    previousList.RemoveAll((item) => {
        //        return item.Id == location.Id;
        //    });

        //    string json = JsonConvert.SerializeObject(previousList);

        //    StorageFile file =
        //        await ApplicationData
        //                .Current
        //                .LocalFolder
        //                .CreateFileAsync(SavedLocationsTasksKey, CreationCollisionOption.ReplaceExisting);

        //    await FileIO.WriteTextAsync(file, json);
        //}

        //public static async Task<List<LocationItem>> GetPinnedLocationsTaskList() {
        //    StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(SavedLocationsTasksKey);
        //    if (file == null) return null;

        //    string json = await FileIO.ReadTextAsync(file);
        //    return JsonConvert.DeserializeObject<List<LocationItem>>(json);
        //}

        #endregion tasks

        #region animations
        public static void SaveSceneColorAnimationDeactivated(bool status) {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[SceneColorAnimationDeactivatedKey] = status;
        }

        public static bool IsSceneColorAnimationDeactivated() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            return settingsValues.ContainsKey(SceneColorAnimationDeactivatedKey) ?
                (bool)settingsValues[SceneColorAnimationDeactivatedKey] : false;
        }
        #endregion animation

        #region appversion
        public static bool IsNewUpdatedLaunch() {
            var isNewUpdatedLaunch = true;
            var currentVersion = GetAppVersion();
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(AppVersionKey)) {
                string savedVersion = (string)settingsValues[AppVersionKey];

                if (savedVersion == currentVersion) {
                    isNewUpdatedLaunch = false;
                } else { settingsValues[AppVersionKey] = currentVersion; }

            } else { settingsValues[AppVersionKey] = currentVersion; }

            return isNewUpdatedLaunch;
        }

        public static string GetAppVersion() {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

        }
        #endregion appversion
    }
}