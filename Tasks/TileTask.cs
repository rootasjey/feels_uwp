using DarkSkyApi;
using DarkSkyApi.Models;
using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Tasks.Services;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.Storage;

namespace Tasks {
    public sealed class TileTask : IBackgroundTask {
        #region variables
        BackgroundTaskDeferral _deferral;

        volatile bool _cancelRequested = false;

        private const string _APIKey = "57281e87e833689d3150c587198f04c6";

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

        private static string _TileTaskActivity {
            get {
                return "TileUpdaterTaskActivity";
            }
        }

        #endregion variables

        private void StartTask(IBackgroundTaskInstance taskInstance) {
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
        }

        private void EndTask() {
            _deferral.Complete();
        }

        /// <summary>
        /// Indicate that the background task is canceled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reason"></param>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
            _cancelRequested = true;
            LogTaskError(reason);
        }

        public async void Run(IBackgroundTaskInstance taskInstance) {
            StartTask(taskInstance);

            var location = Settings.GetFavoriteLocation();
            string city = location?.Town;

            if (location == null) {
                var position = await GetPosition();
                if (position == null) { EndTask(); return; }

                var coord = position.Coordinate.Point.Position;

                location = new Models.LocationItem() {
                    Latitude = coord.Latitude,
                    Longitude = coord.Longitude
                };

                city = await GetCityName(coord);
            }
            
            var forecast = await FetchCurrentWeather(location.Latitude, location.Longitude);
            TileDesigner.UpdatePrimary(forecast, city);

            LogTaskActivity();
            EndTask();
        }

        private async Task<Forecast> FetchCurrentWeather(double lat, double lon) {
            if (!NetworkInterface.GetIsNetworkAvailable()) { return null; }

            var unit = (Unit)Settings.GetUnit();
            var lang = GetLanguage();

            var client = new DarkSkyService(_APIKey);
            return await client.GetWeatherDataAsync(lat, lon, unit, lang);
        }

        async Task<Geoposition> GetPosition() {
            var geo = new Geolocator();

            try {
                var position = await geo.GetGeopositionAsync();
                return position;
            } catch {
                return null;
            }
        }

        private async Task<string> GetCityName(BasicGeoposition position) {
            MapService.ServiceToken = "AEKtGCjDSo2UnEvMVxOh~iS-cB5ZHhjZiIJ9RgGtVgw~AkzS_JYlIhjskoO8ziK63GAJmtcF7U_t4Gni6nBb-MncX6-iw8ldj_NgnmUIzMPY";

            Geopoint pointToReverseGeocode = new Geopoint(position);

            //Reverse geocode the specified geographic location.
            MapLocationFinderResult result =
                await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

            // If the query returns results, display the name of the town
            // contained in the address of the first result.
            if (result.Status == MapLocationFinderStatus.Success && result.Locations.Count != 0) {
                return result.Locations[0].Address.Town;
            }

            return "";
        }

        Language GetLanguage() {
            var lang = Settings.GetAppCurrentLanguage();

            var culture = new CultureInfo(lang);

            if (culture.CompareInfo.IndexOf(lang, "fr", CompareOptions.IgnoreCase) >= 0) {
                return Language.French;
            }

            if (culture.CompareInfo.IndexOf(lang, "en", CompareOptions.IgnoreCase) >= 0) {
                return Language.English;
            }

            if (culture.CompareInfo.IndexOf(lang, "ru", CompareOptions.IgnoreCase) >= 0) {
                return Language.Russian;
            }

            return Language.English;
        }

        private void LogTaskActivity() {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            var taskActivity = new ApplicationDataCompositeValue {
                ["LastRun"] = DateTime.Now.ToLocalTime().ToString(),
                ["Exception"] = null
            };

            settingsValues[_TileTaskActivity] = taskActivity;
        }

        private void LogTaskError(BackgroundTaskCancellationReason reason) {
            var settingsValues = ApplicationData.Current.LocalSettings.Values;
            var taskActivity = new ApplicationDataCompositeValue {
                ["LastRun"] = DateTime.Now.ToLocalTime().ToString(),
                ["Exception"] = reason.ToString()
            };

            settingsValues[_TileTaskActivity] = taskActivity;
        }
    }
}
