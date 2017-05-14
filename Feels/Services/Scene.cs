using System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using DarkSkyApi.Models;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using Windows.UI.Composition;
using System.Collections.Generic;
using Feels.Composition;
using System.Numerics;

namespace Feels.Services {
    public class Scene {
        static Rect _bounds;
        static double _scaleFactor;
        static Size _size;

        public static Grid CreateNew(CurrentDataPoint current, DayDataPoint day) {
            CalculateDeviceSize();

            var scene = new Grid();
            scene = PaintBackground(scene, current, day);
            scene = AnimateBackgroundColor(scene, current.Icon);
            scene = CreateSunOrMoon(scene, current);
            scene = AddAnimations(scene, current.Icon);
            return scene;
        }

        static void CalculateDeviceSize() {
            _bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            _scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            _size = new Size(_bounds.Width * _scaleFactor, _bounds.Height * _scaleFactor);
        }

        static Grid PaintBackground(Grid scene, CurrentDataPoint current, DayDataPoint day) {
            //var timeNow = DateTime.Now;
            //var sunrise = day.SunriseTime;
            //var sunset = day.SunsetTime;

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

        static Grid AnimateBackgroundColor(Grid scene, string condition) {
            var animatedBackground = new Grid();

            var compositor = ElementCompositionPreview.GetElementVisual(animatedBackground).Compositor;
            var container = compositor.CreateContainerVisual();
            container.Opacity = .5f;

            var visual = compositor.CreateSpriteVisual();
            
            var colors = GetColors(condition);
            var color1 = colors[0];
            var color2 = colors[1];

            var height = (float)_size.Height + 500;
            var width = (float)_size.Width + 500;

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

            //var light = compositor.CreateAmbientLight();
            //light.Color = Colors.White;
            ////light.CoordinateSpace = container;
            //light.Targets.Add(visual);

            ElementCompositionPreview.SetElementChildVisual(animatedBackground, container);
            scene.Children.Add(animatedBackground);
            return scene;
        }

        static List<Color> GetColors(string condition) {
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
                    color2 = Color.FromArgb(255, 249, 191, 59);
                    break;
                case "fog":
                    color1 = Color.FromArgb(255, 108, 122, 137);
                    color2 = Color.FromArgb(255, 218, 223, 225);
                    break;
                default:
                    color1 = Color.FromArgb(255, 255,255,255);
                    color2 = Color.FromArgb(0, 0, 0, 0);
                    break;
            }

            colors.Add(color1);
            colors.Add(color2);
            return colors;
        }

        static Grid CreateSunOrMoon(Grid scene, CurrentDataPoint current) {
            var topScene = new Grid() {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0)
            };

            var condition = current.Icon;

            switch (condition) {
                case "clear-day":
                    MakeItShine("high");
                    break;
                case "clear-night":
                    MakeTheMoon();
                    break;
                case "partly-cloudy-day":
                    MakeItShine("normal");
                    break;
                case "partly-cloudy-night":
                    MakeTheMoon();
                    break;
                case "cloudy":
                    MakeItShine("low");
                    break;
                case "rain":
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

            void MakeItShine(string _condition)
            {
                var sun = CreateSun(_condition);
                topScene.Children.Add(sun);
            }

            void MakeTheMoon(string _condition = "full")
            {
                var moon = CreateMoon(_condition);
                topScene.Children.Add(moon);
            }

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
        
        static Grid CreateSun(string condition) {
            int duration = 30;
            float angle = 45;

            if (condition == "low") {
                duration *= 3;
            } else if (condition == "normal") {
                duration *= 2;
            }

            var height = 70;
            var width = height;

            var grid = new Grid() {
                Height = height,
                Width = width,
                Margin = new Thickness(0, 20, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            var compositor = ElementCompositionPreview.GetElementVisual(grid).Compositor;
            var container = compositor.CreateContainerVisual();
            container.Size = new System.Numerics.Vector2(height, width);

            if (ImageLoader.Instance == null)
                ImageLoader.Initialize(compositor);

            ManagedSurface surface = ImageLoader.Instance.LoadFromUri(new Uri("ms-appx:///Assets/Icons/sun.png"));

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new System.Numerics.Vector2(70, 70);
            container.Children.InsertAtTop(visual);
            container.Opacity = 1f;

            ElementCompositionPreview.SetElementChildVisual(grid, container);

            // -----------
            // ANIMATION |
            // -----------
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, 0f);
            animation.InsertKeyFrame(1f, angle);
            animation.Duration = TimeSpan.FromSeconds(duration);
            animation.DelayTime = TimeSpan.FromSeconds(1);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;

            visual.RotationAxis = new System.Numerics.Vector3(0, 0, 1);
            visual.CenterPoint = new System.Numerics.Vector3(height / 2, width / 2, 0);
            visual.StartAnimation("RotationAngle", animation);

            return grid;
        }

        static Grid CreateMoon(string condition) {
            var height = 70;
            var width = height;

            var grid = new Grid() {
                Height = height,
                Width = width,
                Margin = new Thickness(0, 20, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            var compositor = ElementCompositionPreview.GetElementVisual(grid).Compositor;
            var container = compositor.CreateContainerVisual();
            container.Size = new System.Numerics.Vector2(height, width);

            if (ImageLoader.Instance == null)
                ImageLoader.Initialize(compositor);

            ManagedSurface surface = ImageLoader.Instance.LoadFromUri(new Uri("ms-appx:///Assets/Icons/moon.png"));

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new System.Numerics.Vector2(70, 70);
            container.Children.InsertAtTop(visual);
            container.Opacity = 1f;

            ElementCompositionPreview.SetElementChildVisual(grid, container);

            // -----------
            // ANIMATION |
            // -----------
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, 0f);
            animation.InsertKeyFrame(1f, -3f);
            animation.Duration = TimeSpan.FromSeconds(20);
            animation.DelayTime = TimeSpan.FromSeconds(5);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;

            visual.RotationAxis = new System.Numerics.Vector3(0, 0, 1);
            visual.CenterPoint = new System.Numerics.Vector3(height / 2, width / 2, 0);
            visual.StartAnimation("RotationAngle", animation);

            return grid;
        }
        
        static Grid AddAnimations(Grid scene, string condition) {
            var animationsScene = new Grid();
            var rand = new Random();

            switch (condition) {
                case "clear-day":
                    break;
                case "clear-night":
                    MakeStars();
                    break;
                case "partly-cloudy-day":
                    MakeClouds();
                    break;
                case "partly-cloudy-night":
                    MakeStars();
                    MakeClouds();
                    break;
                case "cloudy":
                    MakeLotOfCloudds();
                    break;
                case "rain":
                    MakeItRain();
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

            //MakeItRain();

            void MakeItRain()
            {
                for (int i = 0; i < 20; i++) {
                    var line = CreateRandomLine();
                    animationsScene.Children.Add(line);
                    CreateRandomLineAnimation(line);
                }
            }

            void MakeStars(int starsNumber = 15)
            {
                var x = (int)_size.Width;
                for (int i = 0; i < starsNumber; i++) {
                    var coord = new Vector2(rand.Next(-x, x), rand.Next(0, 400));
                    var duration = rand.Next(10, 20);
                    var radius = rand.Next(5, 10);

                    var star = CreateStar(duration, coord, radius);
                    animationsScene.Children.Add(star);
                }
            }

            void MakeClouds(int cloudsNumber = 3)
            {
                var x = (int)_size.Width / 2;

                for (int i = 0; i < cloudsNumber; i++) {
                    var duration = rand.Next(10, 20);
                    var endX = rand.Next(-x, x);
                    var marginTop = rand.Next(50, 300);

                    var cloud = CreateCloud(duration, endX, marginTop);
                    animationsScene.Children.Add(cloud);
                }
            }

            void MakeLotOfCloudds()
            {
                MakeClouds(6);             
            }

            scene.Children.Add(animationsScene);
            return scene;
        }

        static Grid CreateStar(int duration, Vector2 coord, int radius) {
            var x = coord.X;
            var y = coord.Y;

            var height = radius;
            var width = height;

            var grid = new Grid() {
                Height = height,
                Width = width,
                Margin = new Thickness(x, y, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            var compositor = ElementCompositionPreview.GetElementVisual(grid).Compositor;
            var container = compositor.CreateContainerVisual();
            container.Size = new Vector2(height, width);

            if (ImageLoader.Instance == null)
                ImageLoader.Initialize(compositor);

            ManagedSurface surface = ImageLoader.Instance.LoadCircle(radius, Colors.White);

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new Vector2(radius, radius);
            container.Children.InsertAtTop(visual);
            //container.Opacity = .5f;

            ElementCompositionPreview.SetElementChildVisual(grid, container);

            // ------------
            // ANIMATION //
            // ------------
            //var light = compositor.CreateAmbientLight();
            //light.Color = Colors.White;
            ////light.CoordinateSpace = container;
            //light.Targets.Add(container);

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, .5f);
            animation.InsertKeyFrame(1f, 0f);
            animation.Duration = TimeSpan.FromSeconds(duration);
            animation.DelayTime = TimeSpan.FromSeconds(duration/2);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;
            visual.StartAnimation("Opacity", animation);

            return grid;
        }

        static Grid CreateCloud(int duration, int endX, int marginTop) {
            var height = 70;
            var width = height;

            var grid = new Grid() {
                Height = height,
                Width = width,
                Margin = new Thickness(0, marginTop, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            var compositor = ElementCompositionPreview.GetElementVisual(grid).Compositor;
            var container = compositor.CreateContainerVisual();
            container.Size = new Vector2(height, width);

            if (ImageLoader.Instance == null)
                ImageLoader.Initialize(compositor);

            ManagedSurface surface = ImageLoader.Instance.LoadFromUri(new Uri("ms-appx:///Assets/Icons/cloudy.png"));

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new Vector2(70, 70);
            container.Children.InsertAtTop(visual);
            container.Opacity = .5f;

            ElementCompositionPreview.SetElementChildVisual(grid, container);

            // ------------
            // ANIMATION //
            // ------------
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, 0f);
            animation.InsertKeyFrame(1f, endX);
            animation.Duration = TimeSpan.FromSeconds(duration);
            animation.DelayTime = TimeSpan.FromSeconds(duration / 2);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;
            visual.StartAnimation("Offset.x", animation);

            return grid;
        }

        static void AnimateStar(Ellipse star) {
            var visual = ElementCompositionPreview.GetElementVisual(star);
            var compositor = visual.Compositor;
            var containerVisual = compositor.CreateContainerVisual();
            var spriteVisual = compositor.CreateSpriteVisual();
            var animation = compositor.CreateColorKeyFrameAnimation();

            // Create the KeyFrames using Windows.UI.Color objects
            animation.InsertKeyFrame(0.5f, Colors.Purple);
            animation.InsertKeyFrame(1.0f, Colors.Cyan);

            // Set the interpolation to go through the HSL space
            animation.InterpolationColorSpace = CompositionColorSpace.Hsl;
            animation.Duration = TimeSpan.FromSeconds(3);

            // Apply the cubic-bezier to a KeyFrame
            var brush = compositor.CreateColorBrush(Colors.White);
            spriteVisual.Brush = brush;
            spriteVisual.Brush.StartAnimation("Color", animation);

            containerVisual.Children.InsertAtTop(spriteVisual);
            
        }

        static Line CreateRandomLine() {
            var rand = new Random();
            var x = rand.Next(0, (int)_size.Width);
            var y = -70;
            //var y = rand.Next(0, 400);
            //var line = new Line() {
            //    X1 = (x + 90),
            //    Y1 = y,
            //    X2 = x,
            //    Y2 = (y + 70),
            //    Opacity = .5,
            //    Stroke = new SolidColorBrush(Colors.White),
            //    StrokeThickness = 5
            //};
            var line = new Line() {
                X1 = x,
                Y1 = y,
                X2 = x,
                Y2 = (y + 70),
                Opacity = .5,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 5
            };

            return line;
        }

        static void CreateRandomLineAnimation(Line line) {
            var rand = new Random();
            //var coord = rand.Next((int)size.Height, 900);

            var _visual = ElementCompositionPreview.GetElementVisual(line);
            var _compositor = _visual.Compositor;
            var _animation = _compositor.CreateVector2KeyFrameAnimation();

            //_animation.InsertKeyFrame(1f, new System.Numerics.Vector2(-coord, coord));
            _animation.InsertKeyFrame(1f, new System.Numerics.Vector2(0, (float)_size.Height));
            _animation.Duration = TimeSpan.FromSeconds(rand.Next(2,4));
            _animation.DelayTime = TimeSpan.FromSeconds(rand.Next(0, 10));
            _animation.IterationBehavior = AnimationIterationBehavior.Forever;

            _visual.StartAnimation("Offset.xy", _animation);
        }
        
    }
}
