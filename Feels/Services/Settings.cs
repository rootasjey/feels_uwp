using Windows.Storage;
using Windows.UI.Xaml;
using Windows.System.UserProfile;
using Windows.Devices.Geolocation;

namespace Feels.Services {
    public class Settings {
        //private const string _bingSearchKey = "pCzCBMoEJtZ76ni+ge9sbAYr5PXDfe2ksLPW63wxcVs= ";
        private static string ThemeKey {
            get {
                return "Theme";
            }
        }

        private static string LangKey {
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

        public static void SaveLanguage(string language) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[LangKey] = language;
        }

        public static string GetLanguage() {
            string defaultLanguage = GlobalizationPreferences.Languages[0];

            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LangKey) ? (string)settingsValues[LangKey] : defaultLanguage;
        }

        public static void SaveUnit(string unit) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[UnitKey] = unit;
        }

        public static string GetUnit() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(UnitKey) ? (string)settingsValues[UnitKey] : null;
        }

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

        public static void SaveFirstLaunchPassed() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[FirstLaunchKey] = false;
        }

        public static bool IsFirstLaunch() {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(FirstLaunchKey) ? false : true;
        }

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

    }
}