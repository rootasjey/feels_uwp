using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Feels.Services {
    public static class Wallpaper {
        private static string LockscreenPath = "LockscreenBackgroundPath";
        private static string AppPath = "AppBackgroundPath";

        private static string UnsplashURL {
            get {
                return "https://unsplash.it/1500?random";
            }
        }

        public static void SavePath(string path) {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[LockscreenPath] = path;
        }

        /// <summary>
        /// Get the last downladed image's path
        /// </summary>
        /// <returns></returns>
        public static string GetPath() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            return settingsValues.ContainsKey(LockscreenPath) ? (string)settingsValues[LockscreenPath] : null;
        }

        /// <summary>
        /// Return a new image from Unsplash
        /// </summary>
        /// <returns>image path from Isolated Storage</returns>
        public static async Task<string> GetNew() {
            string name = GenerateName();
            var wallpaper = await Download(UnsplashURL, name);

            SavePath(wallpaper.Path);

            return wallpaper.Path;
        }

        /// <summary>
        /// Get a random name beggining with "wall-"
        /// </summary>
        /// <returns></returns>
        public static string GenerateName() {
            var random = new Random();
            return "wall-" + random.Next();
        }

        private static async Task<StorageFile> Download(string URI, string filename) {
            var rootFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Citations365\\CoverPics", CreationCollisionOption.OpenIfExists);
            var coverpic = await rootFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            try {
                var client = new HttpClient();
                byte[] buffer = await client.GetByteArrayAsync(URI); // Download file
                using (Stream stream = await coverpic.OpenStreamForWriteAsync())
                    stream.Write(buffer, 0, buffer.Length); // Save

                return coverpic;
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Set the last download image as device's wallpaper
        /// </summary>
        /// <returns>Tell if the wallpaper has been correctly setted</returns>
        public static async Task<bool> SetWallpaperAsync() {
            bool success = false;

            if (!UserProfilePersonalizationSettings.IsSupported()) {
                return false;
            }

            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(GetPath());
            UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
            success = await profileSettings.TrySetLockScreenImageAsync(file);
            return success;
        }
    }
}