using DarkSkyApi.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

namespace Feels.Services.WeatherScene {
    public class Backgrounds {
        public static Grid PaintBackground(Grid scene, CurrentDataPoint current, DayDataPoint day) {
            var condition = current.Icon;

            var gradient = new LinearGradientBrush();
            var firstStop = new GradientStop() { Offset = 0.0 };
            var lastStop = new GradientStop() { Offset = 0.5 };

            switch (condition) {
                case "clear-day":
                    firstStop.Color = Color.FromArgb(255, 249, 191, 59);
                    lastStop.Color = Color.FromArgb(255, 249, 105, 14);
                    break;
                case "clear-night":
                    firstStop.Color = Color.FromArgb(255, 51, 110, 123);
                    lastStop.Color = Color.FromArgb(255, 31, 58, 147);
                    break;
                case "partly-cloudy-day":
                    firstStop.Color = Color.FromArgb(255, 58, 83, 155);
                    lastStop.Color = Color.FromArgb(255, 34, 49, 63);
                    break;
                case "partly-cloudy-night":
                    firstStop.Color = Color.FromArgb(255, 58, 83, 155);
                    lastStop.Color = Color.FromArgb(255, 34, 49, 63);
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

            scene.Background = gradient;

            return scene;
        }

        public static Grid AnimateBackgroundColor(Grid scene, string condition, Size screenSize) {
            var animatedBackground = new Grid();

            var compositor = ElementCompositionPreview.GetElementVisual(animatedBackground).Compositor;
            var container = compositor.CreateContainerVisual();
            container.Opacity = .5f;

            var visual = compositor.CreateSpriteVisual();

            var colors = GetColors(condition);
            var color1 = colors[0];
            var color2 = colors[1];

            var height = (float)screenSize.Height + 500;
            var width = (float)screenSize.Width + 1000;

            visual.Brush = compositor.CreateColorBrush(color1);
            visual.Size = new Vector2(height, width);
            container.Children.InsertAtTop(visual);

            var colorAnimation = compositor.CreateColorKeyFrameAnimation();

            colorAnimation.InsertKeyFrame(0.0f, color1);
            colorAnimation.InsertKeyFrame(1.0f, color2);

            colorAnimation.InterpolationColorSpace = CompositionColorSpace.Hsl;
            colorAnimation.Direction = AnimationDirection.Alternate;
            colorAnimation.Duration = TimeSpan.FromSeconds(10);
            colorAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            visual.Brush.StartAnimation("Color", colorAnimation);

            ElementCompositionPreview.SetElementChildVisual(animatedBackground, container);
            scene.Children.Add(animatedBackground);
            return scene;
        }

        private static List<Color> GetColors(string condition) {
            var colors = new List<Color>();

            Color color1;
            Color color2;

            switch (condition) {
                case "clear-day":
                    color1 = Color.FromArgb(255, 233, 212, 96);
                    color2 = Color.FromArgb(255, 107, 185, 240);
                    break;
                case "clear-night":
                    color1 = Color.FromArgb(255, 44, 62, 80);
                    color2 = Color.FromArgb(0, 0, 0, 0);
                    break;
                case "partly-cloudy-day":
                    color1 = Color.FromArgb(255, 228, 241, 254);
                    color2 = Color.FromArgb(255, 37, 116, 169);
                    break;
                case "partly-cloudy-night":
                    color1 = Color.FromArgb(255, 103, 128, 159);
                    color2 = Color.FromArgb(0, 0, 0, 0);
                    break;
                case "cloudy":
                    color1 = Color.FromArgb(255, 103, 128, 159);
                    color2 = Color.FromArgb(255, 189, 195, 199);
                    break;
                case "rain":
                    color1 = Color.FromArgb(255, 103, 128, 159);
                    color2 = Color.FromArgb(255, 37, 116, 169);
                    break;
                case "sleet": // neige fondu
                    color1 = Color.FromArgb(255, 236, 240, 241);
                    color2 = Color.FromArgb(255, 191, 191, 191);
                    break;
                case "snow":
                    color1 = Color.FromArgb(255, 236, 240, 241);
                    color2 = Colors.White;
                    break;
                case "wind":
                    color1 = Color.FromArgb(255, 236, 236, 236);
                    color2 = Color.FromArgb(255, 135, 211, 124);
                    break;
                case "fog":
                    color1 = Color.FromArgb(255, 108, 122, 137);
                    color2 = Color.FromArgb(255, 218, 223, 225);
                    break;
                default:
                    color1 = Color.FromArgb(255, 255, 255, 255);
                    color2 = Color.FromArgb(0, 0, 0, 0);
                    break;
            }

            colors.Add(color1);
            colors.Add(color2);
            return colors;
        }

    }
}
