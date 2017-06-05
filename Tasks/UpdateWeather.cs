using DarkSkyApi;
using DarkSkyApi.Models;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Tasks.Services;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;

namespace Tasks {
    public sealed class UpdateWeather : IBackgroundTask {
        BackgroundTaskDeferral _deferral;

        volatile bool _cancelRequested = false;

        private const string _APIKey = "57281e87e833689d3150c587198f04c6";

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
        }

        public async void Run(IBackgroundTaskInstance taskInstance) {
            StartTask(taskInstance);

            var position = await GetPosition();
            if (position == null) { EndTask(); return; }

            var coord = position.Coordinate.Point.Position;

            var forecast = await FetchCurrentWeather(coord.Latitude, coord.Longitude);
            TileDesigner.UpdatePrimary(forecast, coord);

            EndTask();
        }

        private async Task<Forecast> FetchCurrentWeather(double lat, double lon) {
            if (!NetworkInterface.GetIsNetworkAvailable()) { return null; }

            var client = new DarkSkyService(_APIKey);
            var forecast = await client.GetWeatherDataAsync(lat, lon, Unit.SI);
            return forecast;
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
    }
}
