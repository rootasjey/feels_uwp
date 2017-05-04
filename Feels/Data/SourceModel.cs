using Feels.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Feels.Data {
    public class SourceModel {
        public ObservableCollection<Weather> Cities { get; set; }

        private const string _APIKey = "9b9a5a81665d4e7a949cbe7aef92fae4";
        private string _BaseURL {
            get {
                return "http://api.weatherbit.io/v1.0/";
            }
        }

        private Dictionary<string, string> _Endpoints = new Dictionary<string, string>() {
            {"current","current" }
        };


        public async Task FetchCurrentWeather(double lat, double lon) {
            
            if (Cities == null) Cities = new ObservableCollection<Weather>();

            var coord = ConvertCoord(lat, lon);
            var url = BuildURL(coord["Latitude"], coord["Longitude"]);
            var response = await Fetch(url);

            if (string.IsNullOrEmpty(response)) return;

            var parsedResponse = JObject.Parse(response);
            var data = (JArray)parsedResponse["data"];
            var raw = (JObject)data[0];

            var currentLocation = new Weather() {
                Location = new Location() {
                    City = (string)raw["city_name"],
                    Latitude = coord["Latitude"],
                    Longitude = coord["Longitude"],
                    CountryCode = (string)raw["country_code"],
                    TimeZone = (string)raw["timezone"],
                    StateCode = (string)raw["state_code"]
                },

                Current = new Observation() {
                    ApparentTemperature = (string)raw["app_temp"],
                    Clouds = (string)raw["clouds"],
                    DateTime = (string)raw["datetime"],
                    Description = (string)raw["weather"]["description"],
                    Precipitations = (string)raw["precip"],
                    Pressure = (string)raw["pres"],
                    RelativeHumidity = (string)raw["rh"],
                    SeaLevelPressure = (string)raw["slp"],
                    StationID = (string)raw["station"],
                    Sunrise = (string)raw["sunrise"],
                    Sunset = (string)raw["sunset"],
                    Temperature = (string)raw["temp"],
                    UV = (string)raw["uv"],
                    Visibility = (string)raw["visibility"],
                    WindSpeed = (string)raw["wind_spd"],
                    WindDirection = (string)raw["wind_dir"],
                    WindCardinalDirection = (string)raw["wind_cdir"]
                }
            };

            Cities.Add(currentLocation);

            string BuildURL(string _latitude, string _longitude)
            {
                var _url = _BaseURL + _Endpoints["current"];

                _url += "?lat=" + _latitude + "&lon=" + _longitude;
                _url += "&key=" + _APIKey;
                return _url;
            }

            Dictionary<string, string> ConvertCoord(double _latitude, double _longitude)
            {
                var _coord = new Dictionary<string, string>();
                var _latStr = _latitude.ToString().Replace(",", ".");
                var _lonStr = _longitude.ToString().Replace(",", ".");

                _coord.Add("Latitude", _latStr);
                _coord.Add("Longitude", _lonStr);
                return _coord;
            }
        }

        public async Task FetchHourly(Weather weather) {

        }

        public async Task FetchDaily(Weather weather) {

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
