using Feels.Models;
using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;

namespace Feels.Services {
    public class Scene {
        public static Grid CreateNew(Weather weather) {
            var scene = new Grid();
            scene = PaintBackground(scene, weather);
            scene = AddWeatherIcons(scene, weather);
            return scene;
        }

        static Grid PaintBackground(Grid scene, Weather weather) {
            var current = DateTime.Now;
            var sunrise = DateTime.Parse(weather.Current.Sunrise);
            var sunset = DateTime.Parse(weather.Current.Sunset);

            if (IsNight(weather)) {
                scene.Background = new SolidColorBrush(Color.FromArgb(0, 34, 49, 63));
            }
            else {
                scene.Background = PaintFromWeatherCondition(weather.Current.Description);
            }

            return scene;
        }

        static SolidColorBrush PaintFromWeatherCondition(string condition) {
            switch (condition) {
                case "Light rain":
                    return new SolidColorBrush(Color.FromArgb(255, 52, 73, 94));
                case "Sunny":
                    return new SolidColorBrush(Color.FromArgb(255, 245, 171, 53));
                case "Clear sky":
                    return new SolidColorBrush(Color.FromArgb(255, 68, 108, 179));
                default:
                    return new SolidColorBrush(Color.FromArgb(255, 68, 108, 179));
            }
        }

        static Grid AddWeatherIcons(Grid scene, Weather weather) {
            var topScene = new Grid() {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 30, 0)
                
            };

            if (IsNight(weather)) {
                topScene = AddMoon(topScene);
                topScene = AddNightOtherIcons(topScene, weather.Current.Description);
                topScene = AddStars(topScene);
            } else {
                topScene = AddCurrentWeatherIcon(topScene, weather.Current.Description);
            }

            scene.Children.Add(topScene);
            return scene;
        }

        static bool IsNight(Weather weather) {
            var current = DateTime.Now;
            var sunrise = DateTime.Parse(weather.Current.Sunrise);
            var sunset = DateTime.Parse(weather.Current.Sunset);

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

            secondaryScene.Children.Add(star);
            secondaryScene.Children.Add(star2);
            secondaryScene.Children.Add(star3);

            return topScene;
        }

        static Grid AddCurrentWeatherIcon(Grid topScene, string condition) {
            var icon = new BitmapIcon();
            var sun = new FontIcon() {
                Glyph = "\uE706"
            };

            switch (condition) {
                case "Light rain":
                    icon.UriSource = new Uri("ms-appx:///Assets/Icons/cloudrain.png");
                    topScene.Children.Add(icon);
                    break;
                case "Sunny":
                    topScene.Children.Add(sun);
                    break;
                case "Clear sky":
                    icon.UriSource = new Uri("");
                    break;
                default:
                    topScene.Children.Add(sun);
                    break;
            }

            return topScene;
        }
    }
}
