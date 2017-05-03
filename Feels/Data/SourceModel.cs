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
        

        public async Task FetchCurrentWether(double lat, double lon) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return;
            }

            if (Cities == null) Cities = new ObservableCollection<Weather>();

            var http = new HttpClient();
            string responseBodyAsText;
            var url = _BaseURL + _Endpoints["current"];

            var latitude = lat.ToString().Replace(",", ".");
            var longitude = lon.ToString().Replace(",", ".");

            url += "?lat=" + latitude + "&lon=" + longitude;
            url += "&key=" + _APIKey;

            try {
                HttpResponseMessage message = await http.GetAsync(url);
                responseBodyAsText = await message.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(responseBodyAsText)) return;

                var parsedResponse = JObject.Parse(responseBodyAsText);
                var data = (JArray)parsedResponse["data"];
                var raw = (JObject)data[0];

                var currentLocation = new Weather() {
                    Location = new Location() {
                        City = (string)raw["city_name"],
                        Latitude = latitude,
                        Longitude = longitude,
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

            } catch {
                return;
            }
        }
        
    }
}
