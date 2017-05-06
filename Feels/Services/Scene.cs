using Feels.Models;
using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Uwp.UI.Animations;
using DarkSkyApi.Models;

namespace Feels.Services {
    public class Scene {
        public static Grid CreateNew(CurrentDataPoint current, DayDataPoint day) {
            var scene = new Grid();
            scene = PaintBackground(scene, current, day);
            scene = AddWeatherIcons(scene, current);
            return scene;
        }

        static Grid PaintBackground(Grid scene, CurrentDataPoint current, DayDataPoint day) {
            //var timeNow = DateTime.Now;
            //var sunrise = day.SunriseTime;
            //var sunset = day.SunsetTime;
            //if (IsNight(timeNow, sunrise, sunset)) {
            //    scene.Background = new SolidColorBrush(Color.FromArgb(0, 34, 49, 63));
            //} else {
            //    scene.Background = PaintFromWeatherCondition(current.Icon);
            //}

            scene.Background = PaintFromWeatherCondition(current.Icon);

            return scene;
        }

        static GradientBrush PaintFromWeatherCondition(string condition) {
            var gradient = new LinearGradientBrush();
            var firstStop = new GradientStop() { Offset = 0.0 };
            var lastStop = new GradientStop() { Offset = 0.5 };

            switch (condition) {
                case "clear-day":
                    firstStop.Color = Color.FromArgb(255, 249, 191, 59);
                    lastStop.Color = Color.FromArgb(255, 249, 105, 14);
                    break;
                case "clear-night":
                    firstStop.Color = Color.FromArgb(255, 249, 191, 59);
                    lastStop.Color = Color.FromArgb(255, 249, 105, 14);
                    break;
                case "partly-cloudy-day":
                    firstStop.Color = Color.FromArgb(255, 249, 191, 59);
                    lastStop.Color = Color.FromArgb(255, 189, 195, 199);
                    break;
                case "partly-cloudy-night":
                    firstStop.Color = Color.FromArgb(255, 103, 128, 159);
                    lastStop.Color = Color.FromArgb(100, 34, 49, 63);
                    break;
                case "cloudy":
                    firstStop.Color = Color.FromArgb(255, 34, 49, 63);
                    lastStop.Color = Color.FromArgb(255, 103, 128, 159);
                    break;
                case "rain":
                    firstStop.Color = Color.FromArgb(255, 31, 58, 147);
                    lastStop.Color = Color.FromArgb(255, 75, 119, 190);
                    break;
                case "sleet": // neige fondu
                    firstStop.Color = Color.FromArgb(255, 236, 240, 241);
                    lastStop.Color = Color.FromArgb(255, 191, 191, 191);
                    break;
                case "snow":
                    firstStop.Color = Color.FromArgb(255, 236, 236, 236);
                    lastStop.Color = Color.FromArgb(255, 255, 255, 255);
                    break;
                case "wind":
                    firstStop.Color = Color.FromArgb(255, 236, 236, 236);
                    lastStop.Color = Color.FromArgb(255, 137, 196, 244);
                    break;
                case "fog":
                    firstStop.Color = Color.FromArgb(255, 107, 185, 240);
                    lastStop.Color = Color.FromArgb(255, 174, 168, 211);
                    break;
                default:
                    firstStop.Color = Color.FromArgb(255, 249, 191, 59);
                    lastStop.Color = Color.FromArgb(255, 211, 84, 0);
                    break;
            }

            gradient.GradientStops.Add(firstStop);
            gradient.GradientStops.Add(lastStop);
            return gradient;
        }

        static Grid AddWeatherIcons(Grid scene, CurrentDataPoint current) {
            var topScene = new Grid() {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0)
                
            };

            //if (IsNight(weather)) {
            //    topScene = AddMoon(topScene);
            //    topScene = AddNightOtherIcons(topScene, weather.Current.Description);
            //    topScene = AddStars(topScene);
            //} else {
            //    topScene = AddCurrentWeatherIcon(topScene, weather.Current.Description);
            //}

            topScene = AddCurrentWeatherIcon(topScene, current.Icon);
            scene.Children.Add(topScene);
            return scene;
        }

        static bool IsNight(DateTime current, DateTimeOffset sunrise, DateTimeOffset sunset) {
            if (current > sunrise && current < sunset) {
                return false;
            }

            if (current > sunset) {
                return true;
            }

            return false;
        }

        static Grid AddSun(Grid sceneIcons) {
            return sceneIcons;
        }

        static Grid AddMoon(Grid sceneIcons) {
            var moon = new FontIcon() {
                Glyph = "\uF0CE",
                Margin = new Thickness(0, 0, 5, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontSize = 100,
                Name = "Moon"
            };

            sceneIcons.Children.Add(moon);
            return sceneIcons;
        }

        static Grid AddNightOtherIcons(Grid topScene, string condition) {
            switch (condition) {
                case "Overcast clouds":
                    var cloud = new BitmapIcon() {
                        UriSource = new Uri("ms-appx:///Assets/Icons/cloud.png"),
                        Height = 40,
                        Width = 40,
                        Opacity = 0.6,
                        Margin = new Thickness(0,0,40,0),
                    };
                    topScene.Children.Add(cloud);
                    break;
                default:
                    break;
            }

            return topScene;
        }

        static Grid AddCloud(Grid sceneIcons) {
            var cloud = new BitmapIcon() {
                UriSource = new Uri("ms-appx:///Assets/Icons/cloud.png")
            };

            sceneIcons.Children.Add(cloud);
            return sceneIcons;
        }

        static Grid AddStars(Grid topScene) {
            var secondaryScene = new Grid() {
                Name = "Stars"
            };
            topScene.Children.Add(secondaryScene);

            var star = new FontIcon() {
                Glyph = "\uE735",
                FontSize = 15,
                Opacity = 1,
                Margin = new Thickness(0,-30,50,0),
                Foreground = new SolidColorBrush(Colors.White)
            };

            var star2 = new FontIcon() {
                Glyph = "\uE735",
                FontSize = 20,
                Opacity = 1,
                Margin = new Thickness(0, 90, 90, 0),
                Foreground = new SolidColorBrush(Colors.White)
            };

            var star3 = new FontIcon() {
                Glyph = "\uE735",
                Margin = new Thickness(0, 120, 0, 0),
                FontSize = 10,
                Opacity = 1,
                Foreground = new SolidColorBrush(Colors.White)
            };

            star.Light(30, 5000, 2000).Start();

            secondaryScene.Children.Add(star);
            secondaryScene.Children.Add(star2);
            secondaryScene.Children.Add(star3);

            return topScene;
        }

        static Grid AddCurrentWeatherIcon(Grid topScene, string condition) {
            var icon = new BitmapIcon() {
                Height = 60,
                Width = 60
            };
            var fontIcon = new FontIcon() {
                Glyph = "\uE706",
                FontSize = 90
            };

            switch (condition) {
                case "clear-day":
                    break;
                case "clear-night":
                    break;
                case "partly-cloudy-day":
                    break;
                case "partly-cloudy-night":
                    break;
                case "cloudy":
                    break;
                case "rain":
                    icon.UriSource = new Uri("ms-appx:///Assets/Icons/cloudrain.png");
                    topScene.Children.Add(icon);
                    break;
                case "sleet": // neige fondu
                    break;
                case "snow":
                    break;
                case "wind":
                    break;
                case "fog":
                    break;
                default:
                    break;
            }


            return topScene;
        }
    }
}
