using DarkSkyApi.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace Feels.Services {
    public class TileDesigner {
        public static void UpdatePrimary() {
            var data = App.DataSource;
            if (data == null) return;

            var tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
            tileUpdater.EnableNotificationQueue(true);

            tileUpdater.Update(CreateNotification(data.Forecast));
            //tileUpdater.Update(); // current forecast
            //tileUpdater.Update(); // others infos current forecast
            //tileUpdater.Update(); // hourly forecast
            //tileUpdater.Update(); // daily forecast
        }

        static TileNotification CreateNotification(Forecast forecast) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = CreateCurrentForecast(forecast)
                }
            };

            return new TileNotification(content.GetXml());
        }

        static TileBinding CreateCurrentForecast(Forecast forecast) {
            var currentWeather = forecast.Currently;

            var condition = currentWeather.Icon;
            var temperature = ((int)currentWeather.ApparentTemperature).ToString();

            var timeZone = forecast.TimeZone;
            var index = timeZone.IndexOf("/");
            var location = timeZone.Substring(index + 1);

            return new TileBinding() {
                Content = new TileBindingContentAdaptive() {
                    Children = {
                        new AdaptiveText() {
                            Text = location.ToUpper(),
                            HintStyle = AdaptiveTextStyle.Base
                        },
                        new AdaptiveGroup() {
                            Children = {
                                new AdaptiveSubgroup() { HintWeight = 1 },
                                new AdaptiveSubgroup() {
                                    //HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                    HintWeight = 2,
                                    Children = {
                                        new AdaptiveImage() {
                                            Source = GetIcon(condition),
                                            HintRemoveMargin = true
                                             //HintAlign = AdaptiveImageAlign.Center
                                        }
                                    }
                                },
                                new AdaptiveSubgroup() { HintWeight = 1 }
                            }
                        },
                        new AdaptiveGroup() {
                            Children = {
                                new AdaptiveSubgroup() {
                                    Children = {
                                        new AdaptiveText() {
                                            Text = temperature,
                                            HintStyle = AdaptiveTextStyle.Title
                                        },
                                    }
                                },
                                new AdaptiveSubgroup() {
                                    Children = {

                                        new AdaptiveText() {
                                            Text = currentWeather.Summary,
                                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                                       }
                                    }
                                }
                            }
                        }
                        //new AdaptiveText() {
                        //    Text = temperature,
                        //    HintStyle = AdaptiveTextStyle.Title
                        //},
                        //new AdaptiveText() {
                        //    Text = currentWeather.Summary,
                        //    HintStyle = AdaptiveTextStyle.CaptionSubtle
                       //}

                        //new AdaptiveGroup() {
                        //    Children = {
                                
                        //    }
                        //}
                    }
                }
            };
        }

        static TileBinding CreateHourlyForecast() {
            return new TileBinding() {

            };
        }

        static TileBinding CreateDailyForecast() {
            return new TileBinding() {

            };
        }

        static string GetIcon(string condition) {
            var path = "";

            switch (condition) {
                case "clear-day":
                    path = "Assets/Icons/sun.png";
                    break;
                case "clear-night":
                    path = "Assets/TileIcons/moon.png";
                    break;
                case "partly-cloudy-day":
                    path = "Assets/Icons/partycloudy_day.png";
                    break;
                case "partly-cloudy-night":
                    path = "Assets/Icons/partycloudy_night.png";
                    break;
                case "cloudy":
                    path = "Assets/Icons/cloudy.png";
                    break;
                case "rain":
                    path = "Assets/Icons/rain.png";
                    break;
                case "sleet": // neige fondu
                    path = "Assets/Icons/sleet.png";
                    break;
                case "snow":
                    path = "Assets/Icons/snow.png";
                    break;
                case "wind":
                    path = "Assets/Icons/wind.png";
                    break;
                case "fog":
                    path = "Assets/Icons/fog.png";
                    break;
                default:
                    path = "Assets/Icons/sun.png";
                    break;
            }

            return path;
        }
    }
}
