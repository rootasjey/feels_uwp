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
using DarkSkyApi.Models;

namespace Feels.Services {
    public class Settings {
        #region keys

        private static string _themeKey {
            get {
                return "Theme";
            }
        }

        private static string _languageKey {
            get {
                return "Language";
            }
        }

        private static string _unitKey {
            get {
                return "Unit";
            }
        }

        private static string _firstLaunchKey {
            get {
                return "FirstLaunch";
            }
        }

        private static string _lastPositionKey {
            get {
                return "LastPosition";
            }
        }

        private static string _appVersionKey {
            get {
                return "AppVersion";
            }
        }

        private static string _savedLocationsKey {
            get {
                return "Savedlocations.json";
            }
        }

        private static string _favoriteLocationKey {
            get {
                return "FavoriteLocation";
            }
        }

        private static string _pressureUnitKey {
            get {
                return "PressureUnit";
            }
        }

        private static string _sceneColorAnimationDeactivatedKey {
            get {
                return "SceneColorAnimationDeactivated";
            }
        }

        private static string _primaryTileTaskTypeKey {
            get {
                return "PrimaryTileTaskType";
            }
        }

        public static string _gpsTaskTypeKey {
            get {
                return "gps";
            }
        }

        public static string _locationTaskTypeKey {
            get {
                return "location";
            }
        }

        private static string _savedLocationsTasksKey {
            get {
                return "SavedLocationTasks";
            }
        }

        private static string _cachedForecastDataKey {
            get {
                return "CachedForecastData";
            }
        }

        private static string _cachedLocationAndTime {
            get {
                return "CachedLocationAndTime";
            }
        }

        private static string _premiumKey {
            get {
                return "Premium";
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

            settingsValues[_lastPositionKey] = composite;
        }

        public static BasicGeoposition GetLastSavedPosition() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            if (!settingsValues.ContainsKey(_lastPositionKey)) return new BasicGeoposition();

            var lastPos = (ApplicationDataCompositeValue)settingsValues[_lastPositionKey];

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
            settingsValues[_languageKey] = language;
        }

        public static string GetAppCurrentLanguage() {
            string defaultLanguage = GlobalizationPreferences.Languages[0];

            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(_languageKey) ? (string)settingsValues[_languageKey] : defaultLanguage;
        }

        #endregion language

        #region units

        public static void SaveUnit(Unit unit) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[_unitKey] = unit.ToString();
        }

        public static Unit GetUnit() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(_unitKey)) {
                var savedUnit = (string)settingsValues[_unitKey];
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
            settingsValues[_pressureUnitKey] = unit;
        }

        public static string GetPressureUnit() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(_pressureUnitKey)) {
                var savedPressureUnit = (string)settingsValues[_pressureUnitKey];
                return savedPressureUnit;
            }

            return null;
        }

        #endregion units

        #region theme

        public static bool IsApplicationThemeLight() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.TryGetValue(_themeKey, out var previousTheme);
            return ApplicationTheme.Light.ToString() == (string)previousTheme;
        }

        public static void UpdateAppTheme(ApplicationTheme theme) {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.TryGetValue(_themeKey, out var previousTheme);

            if ((string)previousTheme == theme.ToString()) return;

            localSettings.Values[_themeKey] = theme.ToString();
            App.UpdateAppTheme();
        }

        #endregion theme

        #region first launch

        public static void SaveFirstLaunchPassed() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[_firstLaunchKey] = false;
        }

        public static bool IsFirstLaunch() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(_firstLaunchKey) ? false : true;
        }

        #endregion first launch

        #region locations

        public static async Task SaveLocationsAsync(List<LocationItem> locations) {
            string json = JsonConvert.SerializeObject(locations);

            StorageFile file =
                await ApplicationData
                        .Current
                        .LocalFolder
                        .CreateFileAsync(_savedLocationsKey, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, json);
        }

        public static async Task<List<LocationItem>> GetSavedLocationAsync() {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(_savedLocationsKey);
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
                        .CreateFileAsync(_favoriteLocationKey, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, serializedLocation);
        }

        public static async Task<LocationItem> GetFavoriteLocation() {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(_favoriteLocationKey);
            if (file == null) return null;

            string json = await FileIO.ReadTextAsync(file);
            return JsonConvert.DeserializeObject<LocationItem>(json);
        }

        public static async Task DeleteFavoriteLocation() {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(_favoriteLocationKey);
            if (file == null) return;

            await file.DeleteAsync();
        }

        #endregion locations

        #region tasks

        public static void SavePrimaryTileTaskType(string type) {
            var localSettingsValues = ApplicationData.Current.LocalSettings.Values;
            localSettingsValues[_primaryTileTaskTypeKey] = type;
        }

        public static string GetPrimaryTileTaskType() {
            var localSettingsValues = ApplicationData.Current.LocalSettings.Values;
            return localSettingsValues.ContainsKey(_primaryTileTaskTypeKey) ? 
                (string)localSettingsValues[_primaryTileTaskTypeKey] : _gpsTaskTypeKey;
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

        #endregion tasks

        #region animations

        public static void SaveSceneColorAnimationDeactivated(bool status) {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[_sceneColorAnimationDeactivatedKey] = status;
        }

        public static bool IsSceneColorAnimationDeactivated() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            return settingsValues.ContainsKey(_sceneColorAnimationDeactivatedKey) ?
                (bool)settingsValues[_sceneColorAnimationDeactivatedKey] : false;
        }

        #endregion animation

        #region appversion

        public static bool IsNewUpdatedLaunch() {
            var isNewUpdatedLaunch = true;
            var currentVersion = GetAppVersion();
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(_appVersionKey)) {
                string savedVersion = (string)settingsValues[_appVersionKey];

                if (savedVersion == currentVersion) {
                    isNewUpdatedLaunch = false;
                } else { settingsValues[_appVersionKey] = currentVersion; }

            } else { settingsValues[_appVersionKey] = currentVersion; }

            return isNewUpdatedLaunch;
        }

        public static string GetAppVersion() {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        #endregion appversion

        #region cached data

        public static async void CacheForecastData(Forecast forecast) {
            if (forecast == null) return;

            var json = JsonConvert.SerializeObject(forecast);

            StorageFile file =
                await ApplicationData
                .Current
                .LocalFolder
                .CreateFileAsync(_cachedForecastDataKey, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, json);
        }

        public static async Task<Forecast> GetCachedForecastData() {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(_cachedForecastDataKey);
            if (file == null) return null;

            string json = await FileIO.ReadTextAsync(file);
            return JsonConvert.DeserializeObject<Forecast>(json);
        }

        /// <summary>
        /// Cache last city and time
        /// </summary>
        /// <returns></returns>
        public static void CacheLocationAndTime(string town, string datetime) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            var composite = new ApplicationDataCompositeValue {
                ["town"] = town,
                ["time"] = datetime
            };

            settingsValues[_cachedLocationAndTime] = composite;
        }

        public static Tuple<string, string> GetCachedLocationAndTown() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            if (!settingsValues.ContainsKey(_cachedLocationAndTime)) return null;

            var cachedData = (ApplicationDataCompositeValue)settingsValues[_cachedLocationAndTime];

            return new Tuple<string, string>((string)cachedData["town"], (string)cachedData["time"]);
        }

        #endregion cached data

        #region premium

        public static void SavePremiumUser(bool value) {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[_premiumKey] = value;
        }

        public static bool IsPremiumUser() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(_premiumKey) ? 
                (bool)localSettings.Values[_premiumKey] : false;
        }

        #endregion premium
    }
}