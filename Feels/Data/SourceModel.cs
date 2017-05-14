using DarkSkyApi;
using DarkSkyApi.Models;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Feels.Data {
    public class SourceModel {
        public Forecast Forecast { get; set; }

        public DarkSkyService Client { get; set; }

        private const string _APIKey = "57281e87e833689d3150c587198f04c6";

        public async Task FetchCurrentWeather(double lat, double lon) {
            if (!NetworkInterface.GetIsNetworkAvailable()) { return; }
            if (Client == null) Client = new DarkSkyService(_APIKey);
            Forecast = await Client.GetWeatherDataAsync(lat, lon, Unit.SI);

        }

        private async Task<string> Fetch(string url) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return null;
            }

            var http = new HttpClient();
            string responseBodyAsText;

            try {
                HttpResponseMessage message = await http.GetAsync(url);
                responseBodyAsText = await message.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(responseBodyAsText)) return null;

                return responseBodyAsText;

            } catch { return null; }
        }
    }
}
