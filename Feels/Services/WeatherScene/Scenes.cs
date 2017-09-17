using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using DarkSkyApi.Models;
using Windows.Foundation;
using Windows.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using Windows.UI.Composition;
using System.Collections.Generic;
using System.Numerics;

namespace Feels.Services.WeatherScene {
    public class Scenes {
        #region variables
        static Rect _screenBounds { get; set; }
        static double _screenScaleFactor { get; set; }
        static Size _screenSize { get; set; }
        
        static bool _IsDay { get; set; }
        #endregion variables

        public static Grid CreateNew(CurrentDataPoint current, DayDataPoint day) {
            InitializeVariables(day);

            var scene = new Grid();
            scene = Backgrounds.PaintBackground(scene, current, day);
            scene = Backgrounds.AnimateBackgroundColor(scene, current.Icon, _screenSize);
            scene = AddAnimationsOn(scene, current.Icon);
            scene = Icons.AddWeatherIconCondition(scene, current, day, _IsDay);

            return scene;
        }

        #region initialization

        static void CalculateDeviceSize() {
            _screenBounds = ApplicationView.GetForCurrentView().VisibleBounds;
            _screenScaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            _screenSize = new Size(_screenBounds.Width * _screenScaleFactor, _screenBounds.Height * _screenScaleFactor);
        }

        static void InitializeVariables(DayDataPoint day) {
            CalculateDeviceSize();

            var afterSunrise = DateTime.Now - day.SunriseTime > TimeSpan.FromMinutes(1);
            var beforeSunset = DateTime.Now - day.SunsetTime < TimeSpan.FromMinutes(0);
            _IsDay = afterSunrise && beforeSunset;
        }

        #endregion initialization

        #region animated scenes

        static Grid AddAnimationsOn(Grid scene, string condition) {
            var animationsScene = new Grid();

            switch (condition) {
                case "clear-day":
                    break;
                case "clear-night":
                    animationsScene.Children.Add(CreateStarsScene());
                    break;
                case "partly-cloudy-day":
                    break;
                case "partly-cloudy-night":
                    animationsScene.Children.Add(CreateStarsScene());
                    break;
                case "cloudy":
                    break;
                case "rain":
                    animationsScene.Children.Add(CreateRainyScene());
                    break;
                case "sleet": // neige fondu
                    animationsScene.Children.Add(CreateSleetScene());
                    break;
                case "snow":
                    animationsScene.Children.Add(CreateSnowScene());
                    break;
                case "wind":
                    animationsScene.Children.Add(CreateWindScene());
                    break;
                case "fog":
                    animationsScene.Children.Add(CreateFogScene());
                    break;
                default:
                    break;
            }

            scene.Children.Add(animationsScene);
            return scene;
        }

        static Canvas CreateRainyScene() {
            var container = new Canvas();
            var rand = new Random();

            for (int i = 0; i < 35; i++) {
                var x = rand.Next(0, (int)_screenSize.Width);
                var duration = rand.Next(3, 8);
                var delay = rand.Next(0, 10);

                var line = Icons.CreateLine(x, duration, delay, _screenSize);
                container.Children.Add(line);
            }

            return container;
        }
        
        static Canvas CreateStarsScene(int starsNumber = 35) {
            var container = new Canvas();
            var rand = new Random();

            var x = (int)_screenSize.Width;

            for (int i = 0; i < starsNumber; i++) {
                var coord = new Vector2(rand.Next(-x, x), rand.Next(0, 400));
                var duration = rand.Next(10, 20);
                var radius = rand.Next(1, 7);

                var star = Icons.CreateStar(duration, coord, radius);
                container.Children.Add(star);
            }

            return container;
        }

        static Canvas CreateSnowScene() {
            var container = new Canvas();
            var rand = new Random();

            for (int i = 0; i < 20; i++) {
                var x = rand.Next(0, (int)_screenSize.Width);
                var size = rand.Next(4, 20);
                var duration = rand.Next(10, 20);
                var delay = rand.Next(0, 10);

                var snow = Icons.CreateSnowBall(x, size, duration, delay, _screenSize);
                container.Children.Add(snow);
            }

            return container;
        }

        static Canvas CreateSleetScene() {
            var container = new Canvas();
            var random = new Random();

            for (int i = 0; i < 10; i++) {
                var x = random.Next(0, (int)_screenSize.Width);
                var size = random.Next(4, 20);
                var duration = random.Next(10, 20);
                var delay = random.Next(0, 10);

                var snow = Icons.CreateSnowBall(x, size, duration, delay, _screenSize);
                container.Children.Add(snow);
            }

            for (int i = 0; i < 10; i++) {
                var x = random.Next(0, (int)_screenSize.Width);
                var duration = random.Next(2, 10);
                var delay = random.Next(0, 10);

                var line = Icons.CreateLine(x, duration, delay, _screenSize);
                container.Children.Add(line);
            }

            return container;
        }

        static Canvas CreateFogScene() {
            var container = new Canvas();
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            var fog1 = Icons.CreateFogIcon();
            var fogVisual1 = Visuals.CreateFogVisual(fog1, compositor, 170, -100);

            var fog2 = Icons.CreateFogIcon(130);
            var fogVisual2 = Visuals.CreateFogVisual(fog2, compositor, 90, (float)_screenSize.Width, 0, 20);

            var fog3 = Icons.CreateFogIcon(120);
            var fogVisual3 = Visuals.CreateFogVisual(fog3, compositor, 120, (float)_screenSize.Width / 2, -90, 15);

            container.Children.Add(fog1);
            container.Children.Add(fog2);
            container.Children.Add(fog3);

            containerVisual.Children.InsertAtTop(fogVisual1);
            containerVisual.Children.InsertAtTop(fogVisual2);
            containerVisual.Children.InsertAtTop(fogVisual3);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        static Canvas CreateWindScene() {
            var container = new Canvas();
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            var endX = (float)_screenSize.Width;
            var endY = (float)_screenSize.Height;

            var random = new Random();

            for (int i = 0; i < 6; i++) {
                var startX = random.Next(100);
                var startY = random.Next(100);

                var leaf = Icons.CreateLeafIcon(40);
                var leafVisual = Visuals.CreateLeafVisual(
                    leaf, compositor, 
                    random.Next(5,12), random.Next(5), 
                    startX, startY, 
                    endX - startX, endY - startY);

                containerVisual.Children.InsertAtTop(leafVisual);
                container.Children.Add(leaf);
            }

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        #endregion animated scenes
    }
}
