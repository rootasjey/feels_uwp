using System;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.Devices.Geolocation;
using DarkSkyApi;
using Windows.ApplicationModel;
using Tasks.Models;
using Newtonsoft.Json;

namespace Tasks.Services {
    public sealed class Settings {
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

        private static string PressureUnit {
            get {
                return "PressureUnit";
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
        public static object GetUnit() {
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
            settingsValues[PressureUnit] = unit;
        }

        public static string GetPressureUnit() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;

            if (settingsValues.ContainsKey(PressureUnit)) {
                var savedPressureUnit = (string)settingsValues[PressureUnit];
                return savedPressureUnit;
            }

            return null;
        }
        #endregion units

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
        public static LocationItem GetFavoriteLocation() {
            StorageFile file = (StorageFile)ApplicationData.Current.LocalFolder
                .TryGetItemAsync(FavoriteLocationKey)
                .AsTask()
                .AsAsyncOperation().
                GetResults();

            if (file == null) return null;

            string json = FileIO.ReadTextAsync(file).AsTask().AsAsyncOperation().GetResults();
            var favoriteLocation = JsonConvert.DeserializeObject<LocationItem>(json);
            return favoriteLocation;
        }

        public static LocationItem GetLocation(string name) {
            StorageFile file = (StorageFile)ApplicationData.Current.LocalFolder
                .TryGetItemAsync(name)
                .AsTask()
                .AsAsyncOperation().
                GetResults();

            if (file == null) return null;

            string json = FileIO.ReadTextAsync(file).AsTask().AsAsyncOperation().GetResults();
            return JsonConvert.DeserializeObject<LocationItem>(json);
        }

        #endregion locations

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