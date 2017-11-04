using DarkSkyApi;
using DarkSkyApi.Models;
using Feels.Services;
using System.Globalization;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Feels.Data {
    public class SourceModel {
        public Forecast Forecast { get; set; }

        public DarkSkyService Client { get; set; }

        private const string _APIKey = "ENTER_YOUR_API_KEY_HERE";

        public async Task FetchCurrentForecast(double latitude, double longitude) {
            if (!NetworkInterface.GetIsNetworkAvailable()) { return; }
            if (Client == null) Client = new DarkSkyService(_APIKey);

            var lang = GetLanguage();
            var unit = Settings.GetUnit();

            Forecast = await Client.GetWeatherDataAsync(latitude, longitude, unit, lang);
        }

        public async Task<Forecast> GetCurrentForecast(double latitude, double longitude) {
            if (!NetworkInterface.GetIsNetworkAvailable()) { return null; }
            if (Client == null) Client = new DarkSkyService(_APIKey);

            var lang = GetLanguage();
            var unit = Settings.GetUnit();
            return await Client.GetWeatherDataAsync(latitude, longitude, unit, lang);
        }

        private Language GetLanguage() {
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
