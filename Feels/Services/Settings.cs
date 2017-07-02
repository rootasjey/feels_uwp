using Windows.Storage;
using Windows.UI.Xaml;
using Windows.System.UserProfile;
using Windows.Devices.Geolocation;
using DarkSkyApi;
using Windows.ApplicationModel;

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
        public static void SaveLanguage(string language) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[LanguageKey] = language;
        }

        public static string GetLanguage() {
            string defaultLanguage = GlobalizationPreferences.Languages[0];

            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LanguageKey) ? (string)settingsValues[LanguageKey] : defaultLanguage;
        }

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

        #region favorites cities
        //public static async Task SaveFavoritesAsync(ObservableKeyedCollection favorites, string source) {
        //    string json = JsonConvert.SerializeObject(favorites);
        //    StorageFile file =
        //        await ApplicationData
        //                .Current
        //                .LocalFolder
        //                .CreateFileAsync("favorites-" + source + ".json", CreationCollisionOption.ReplaceExisting);

        //    await FileIO.WriteTextAsync(file, json);
        //}

        //public static async Task<ObservableKeyedCollection> LoadFavoritesAsync(string source) {
        //    StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync("favorites-" + source + ".json");
        //    if (file == null) return null;

        //    string json = await FileIO.ReadTextAsync(file);
        //    ObservableKeyedCollection favorites = JsonConvert.DeserializeObject<ObservableKeyedCollection>(json);
        //    return favorites;
        //}
        #endregion favorites cities

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