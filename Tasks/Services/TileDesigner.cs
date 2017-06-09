using DarkSkyApi.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Notifications;

namespace Tasks.Services {
    public sealed class TileDesigner {
        private static string BingMapsKey = "AEKtGCjDSo2UnEvMVxOh~iS-cB5ZHhjZiIJ9RgGtVgw~AkzS_JYlIhjskoO8ziK63GAJmtcF7U_t4Gni6nBb-MncX6-iw8ldj_NgnmUIzMPY";

        public static void UpdatePrimary(object rawForecast, object city) {
            if (rawForecast == null) return;
            var forecast = (Forecast)rawForecast;
            var location = (string)city;

            var tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
            tileUpdater.EnableNotificationQueue(true);

            tileUpdater.Update(CreateTileCurrent(forecast, location));       // current
            tileUpdater.Update(CreateTileCurrentDetails(forecast));// detailed infos current
            tileUpdater.Update(CreateTileHourly(forecast.Hourly)); // hourly
            tileUpdater.Update(CreateTileDaily(forecast.Daily));   // daily
        }

        // ------------------
        // EXTRACTION METHODS
        // ------------------
        static async Task<string> GetLocation(BasicGeoposition position) {
            //MapService.ServiceToken = "AEKtGCjDSo2UnEvMVxOh~iS-cB5ZHhjZiIJ9RgGtVgw~AkzS_JYlIhjskoO8ziK63GAJmtcF7U_t4Gni6nBb-MncX6-iw8ldj_NgnmUIzMPY";

            //Geopoint pointToReverseGeocode = new Geopoint(position);

            // Reverse geocode the specified geographic location.
            //MapLocationFinderResult result =
            //    await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

            //// If the query returns results, display the name of the town
            //// contained in the address of the first result.
            //if (result.Status == MapLocationFinderStatus.Success && result.Locations.Count != 0) {
            //    return result.Locations[0].Address.Town;
            //}

            //var r = new ReverseGeocodeRequest() {
            //    BingMapsKey = BingMapsKey,
            //    Point = new Coordinate(position.Latitude, position.Longitude),
            //    IncludeNeighborhood = true
            //};

            //var response = await ServiceManager.GetResponseAsync(r);
            

            return "";
        }

        static string GetTemperature(float temperature) {
            return ((int)temperature).ToString() + "°";
        }

        static string GetPrecipProbability(float probability) {
            if (probability < 0.05) return "";
            return (probability * 100).ToString() + "%";
        }

        static string GetIcon(string condition) {
            var path = "";

            switch (condition) {
                case "clear-day":
                    path = "Assets/TileIcons/sun.png";
                    break;
                case "clear-night":
                    path = "Assets/TileIcons/moon.png";
                    break;
                case "partly-cloudy-day":
                    path = "Assets/TileIcons/partycloudy_day.png";
                    break;
                case "partly-cloudy-night":
                    path = "Assets/TileIcons/partycloudy_night.png";
                    break;
                case "cloudy":
                    path = "Assets/TileIcons/cloudy.png";
                    break;
                case "rain":
                    path = "Assets/TileIcons/rain.png";
                    break;
                case "sleet": // neige fondu
                    path = "Assets/TileIcons/sleet.png";
                    break;
                case "snow":
                    path = "Assets/TileIcons/snow.png";
                    break;
                case "wind":
                    path = "Assets/TileIcons/wind.png";
                    break;
                case "fog":
                    path = "Assets/TileIcons/fog.png";
                    break;
                default:
                    path = "Assets/TileIcons/sun.png";
                    break;
            }

            return path;
        }

        static string GetPrecipIcon(string condition) {
            var path = "";

            switch (condition) {
                case "rain":
                    path = "Assets/Icons/rain.png";
                    break;
                case "sleet": // neige fondu
                    path = "Assets/Icons/sleet.png";
                    break;
                case "snow":
                    path = "Assets/Icons/snow.png";
                    break;
                default:
                    path = "";
                    break;
            }

            return path;
        }

        static string GetWindIcon(float speed) {
            var path = "";

            switch (speed) {
                default:
                    path = "Assets/TileIcons/wind0.png";
                    break;
            }

            return path;
        }

        // ---------------
        // TILES CREATIONS
        // ---------------
        static TileNotification CreateTileCurrent(Forecast forecast, string location) {
            var currentWeather = forecast.Currently;
            var condition = currentWeather.Icon;
            var currentTemperature = GetTemperature(forecast.Currently.ApparentTemperature);
            var maxTemperature = GetTemperature(forecast.Daily.Days[0].ApparentMaxTemperature);
            var minTemperature = GetTemperature(forecast.Daily.Days[0].ApparentMinTemperature);

            var time = DateTime.Now.ToLocalTime().ToString("h tt", CultureInfo.InvariantCulture);

            // Generate Visual
            var content = new TileContent() {
                Visual = new TileVisual() {
                    LockDetailedStatus1 = GetDetailedStatus(),
                    TileMedium = GetMediumVisual(),
                    TileSmall = GetSmallVisual(),
                    TileWide = GetWideVisual(),
                    TileLarge = GetLargeVisual()
                }
            };

            string GetDetailedStatus()
            {
                string formatedText = string.Format("{0} {1} {2} ({3}/{4})",
                    location, currentTemperature, forecast.Currently.Summary, maxTemperature, minTemperature);
                return formatedText;
            }

            TileBinding GetSmallVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveImage() {
                                Source = GetIcon(forecast.Currently.Icon)
                            }
                        }
                    }
                };
            }

            TileBinding GetMediumVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveText() {
                                Text = location.ToUpper(),
                                HintAlign = AdaptiveTextAlign.Center,
                                HintStyle = AdaptiveTextStyle.Body
                            },
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = GetIcon(condition),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() { HintWeight = 1 }
                                }
                            },
                            new AdaptiveGroup() {
                                Children = {
                                    // Temperature
                                    new AdaptiveSubgroup() {
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = currentTemperature,
                                                HintAlign = AdaptiveTextAlign.Center,
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            },
                                        }
                                    },
                                    // Time
                                    new AdaptiveSubgroup() {
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = time.ToString(),
                                                HintAlign = AdaptiveTextAlign.Left,
                                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                                           }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetWideVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        TextStacking = TileTextStacking.Center,
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() {HintWeight = 5},
                                    new AdaptiveSubgroup() {
                                        HintWeight = 25,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = GetIcon(condition),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 30,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = currentTemperature,
                                                HintStyle = AdaptiveTextStyle.Title
                                            },
                                            new AdaptiveText() {
                                                Text = time,
                                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 40,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = forecast.Currently.Summary,
                                                HintStyle = AdaptiveTextStyle.Body
                                            },
                                            new AdaptiveText() {
                                                Text = location,
                                                HintStyle = AdaptiveTextStyle.BaseSubtle
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetLargeVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        TextStacking = TileTextStacking.Center,
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() {HintWeight = 5},
                                    new AdaptiveSubgroup() {
                                        HintWeight = 25,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = GetIcon(condition),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 30,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = currentTemperature,
                                                HintStyle = AdaptiveTextStyle.Title
                                            },
                                            new AdaptiveText() {
                                                Text = time,
                                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 40,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = condition,
                                                HintStyle = AdaptiveTextStyle.Body
                                            },
                                            new AdaptiveText() {
                                                Text = location,
                                                HintStyle = AdaptiveTextStyle.BaseSubtle
                                            }
                                        }
                                    }
                                }
                            },

                            new AdaptiveText(), // for spacing

                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() {HintWeight = 10}, // spacing
                                    // Max - min temperatures
                                    new AdaptiveSubgroup() {
                                        HintWeight = 30,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = maxTemperature,
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            },
                                            new AdaptiveText() {
                                                Text = minTemperature,
                                                HintStyle = AdaptiveTextStyle.SubtitleSubtle
                                            }
                                        }
                                    },

                                    // Wind & Precip icons
                                    new AdaptiveSubgroup() {
                                        HintWeight = 15,
                                        Children = {
                                            //new AdaptiveText(),
                                            new AdaptiveImage() {
                                                Source = GetWindIcon(forecast.Currently.WindSpeed),
                                                HintRemoveMargin = true
                                            },

                                            new AdaptiveText(), // spacing

                                            new AdaptiveImage() {
                                                Source = GetPrecipIcon(forecast.Currently.PrecipitationType),
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },

                                    new AdaptiveSubgroup() {HintWeight = 3}, // spacing

                                    // Wind & Precip values
                                    new AdaptiveSubgroup() {
                                        HintWeight = 40,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = forecast.Currently.WindSpeed.ToString(),
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            },

                                            new AdaptiveText(),
                                            new AdaptiveText() {
                                                Text = GetPrecipProbability(forecast.Currently.PrecipitationProbability),
                                                HintStyle = AdaptiveTextStyle.Subtitle
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            return new TileNotification(content.GetXml());
        }

        static TileNotification CreateTileCurrentDetails(Forecast forecast) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = GetMediumVisual()
                    //TileWide = GetWideVisual()
                }
            };
            
            TileBinding GetMediumVisual()
            {
                //var precipIconPath = GetIcon(forecast.Currently.Icon);
                //var precipProba = (forecast.Currently.PrecipitationProbability * 100).ToString();
                var maxTemp = ((int)forecast.Daily.Days[0].MaxTemperature).ToString() + "°";
                var minTemp = ((int)forecast.Daily.Days[0].MinTemperature).ToString() + "°";

                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            // Max temp
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = maxTemp,
                                                HintStyle = AdaptiveTextStyle.Title
                                            }
                                        }
                                    }
                                }
                            },

                            // Min temp
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = minTemp,
                                                HintStyle = AdaptiveTextStyle.TitleSubtle
                                            }
                                        }
                                    }
                                }
                            },

                            // Wind speed ?
                            new AdaptiveGroup() {
                                Children = {
                                    new AdaptiveSubgroup() { HintWeight = 1 },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 1,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveImage() {
                                                Source = "Assets/Icons/wind0.png",
                                                HintRemoveMargin = true
                                            }
                                        }
                                    },
                                    new AdaptiveSubgroup() {
                                        HintWeight = 2,
                                        HintTextStacking = AdaptiveSubgroupTextStacking.Bottom,
                                        Children = {
                                            new AdaptiveText() {
                                                Text = forecast.Currently.WindSpeed.ToString(),
                                                HintStyle = AdaptiveTextStyle.Caption
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            // TODO: Complete
            //TileBinding GetWideVisual()
            //{
            //    return new TileBinding() {

            //    };
            //}

            return new TileNotification(content.GetXml());
        }

        static TileNotification CreateTileHourly(HourlyForecast hourlyForecast) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = GetMediumVisual(),
                    TileWide = GetWideVisual(),
                    TileLarge = GetLargeVisual()
                }
            };

            TileBinding GetMediumVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[1]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[2])
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetWideVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[1]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[2]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[3]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[4]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[5]),
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetLargeVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        TextStacking = TileTextStacking.Center,
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[1]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[2]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[3]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[4]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[5]),
                                }
                            },
                            new AdaptiveText(), // for spacing
                            new AdaptiveGroup() {
                                Children = {
                                    CreateHourlySubGroup(hourlyForecast.Hours[6]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[7]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[8]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[9]),
                                    CreateHourlySubGroup(hourlyForecast.Hours[10]),
                                }
                            }
                        }
                    }
                };
            }

            return new TileNotification(content.GetXml());
        }

        static TileNotification CreateTileDaily(DailyForecast dailyForecast) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = GetMediumVisual(),
                    TileWide = GetWideVisual(),
                    TileLarge = GetLargeVisual()
                }
            };

            TileBinding GetMediumVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[1]),
                                    CreateDailySubGroup(dailyForecast.Days[2])
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetWideVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[1]),
                                    CreateDailySubGroup(dailyForecast.Days[2]),
                                    CreateDailySubGroup(dailyForecast.Days[3]),
                                    CreateDailySubGroup(dailyForecast.Days[4]),
                                    CreateDailySubGroup(dailyForecast.Days[5])
                                }
                            }
                        }
                    }
                };
            }

            TileBinding GetLargeVisual()
            {
                return new TileBinding() {
                    Content = new TileBindingContentAdaptive() {
                        Children = {
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[1]),
                                    CreateDailySubGroup(dailyForecast.Days[2]),
                                    CreateDailySubGroup(dailyForecast.Days[3])
                                }
                            },
                            //new AdaptiveText(), // for spacing
                            new AdaptiveGroup() {
                                Children = {
                                    CreateDailySubGroup(dailyForecast.Days[4]),
                                    CreateDailySubGroup(dailyForecast.Days[5]),
                                    CreateDailySubGroup(dailyForecast.Days[6]),
                                    CreateDailySubGroup(dailyForecast.Days[7])
                                }
                            }
                        }
                    }
                };
            }

            return new TileNotification(content.GetXml());
        }

        static AdaptiveSubgroup CreateHourlySubGroup(HourDataPoint hour) {
            return new AdaptiveSubgroup() {
                HintWeight = 1,
                Children = {
                    new AdaptiveText() {
                        Text = hour.Time.ToLocalTime().ToString("htt", CultureInfo.InvariantCulture),
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                    },
                    new AdaptiveImage() {
                        Source = GetIcon(hour.Icon),
                        HintRemoveMargin = true
                    },
                    new AdaptiveText() {
                        Text = ((int)hour.ApparentTemperature).ToString() + "°",
                        HintStyle = AdaptiveTextStyle.Base,
                        HintAlign = AdaptiveTextAlign.Center
                    }
                }
            };
        }

        static AdaptiveSubgroup CreateDailySubGroup(DayDataPoint day) {
            return new AdaptiveSubgroup() {
                HintWeight = 1,
                Children = {
                    new AdaptiveText() {
                        Text = day.Time.ToString("ddd"),
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.Caption
                    },
                    new AdaptiveImage() {
                        Source = GetIcon(day.Icon),
                        HintRemoveMargin = true
                    },
                    new AdaptiveText() {
                        Text = ((int)day.ApparentMaxTemperature).ToString() + "°",
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.Caption
                    },
                    new AdaptiveText() {
                        Text = ((int)day.ApparentMinTemperature).ToString() + "°",
                        HintAlign = AdaptiveTextAlign.Center,
                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                    }
                }
            };
        }

    }
}
