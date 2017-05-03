using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.System.UserProfile;

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

        public static void SaveLanguage(string language) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            settingsValues[LangKey] = language;
        }

        public static string GetLanguage() {
            string defaultLanguage = GlobalizationPreferences.Languages[0];

            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LangKey) ? (string)settingsValues[LangKey] : defaultLanguage;
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