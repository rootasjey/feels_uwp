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
using Windows.UI.Xaml.Media.Imaging;
using MahApps.Metro.IconPacks;

namespace Feels.Services {
    public class Scene {
        #region variables
        static Rect _bounds { get; set; }
        static double _scaleFactor { get; set; }
        static Size _size { get; set; }

        static List<CompositionLight> _LightsList { get; set; }

        static bool _IsDay { get; set; }
        #endregion variables

        public static Grid CreateNew(CurrentDataPoint current, DayDataPoint day) {
            InitializeVariables(day);

            var scene = new Grid();
            scene = PaintBackground(scene, current, day);
            scene = AnimateBackgroundColor(scene, current.Icon);
            scene = AddAnimations(scene, current.Icon);
            scene = AddWeatherIconCondition(scene, current);

            return scene;
        }

        static void CalculateDeviceSize() {
            _bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            _scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            _size = new Size(_bounds.Width * _scaleFactor, _bounds.Height * _scaleFactor);
        }

        static void InitializeVariables(DayDataPoint day) {
            CalculateDeviceSize();
            _LightsList = new List<CompositionLight>();

            var afterSunrise = DateTime.Now - day.SunriseTime > TimeSpan.FromMinutes(1);
            var beforeSunset = DateTime.Now - day.SunsetTime < TimeSpan.FromMinutes(0);
            _IsDay = afterSunrise && beforeSunset;
        }

        #region lights
        /// <summary>
        /// Add ambiant light to the scene
        /// </summary>
        /// <param name="scene"></param>
        public static void AddAmbiantLight(Grid scene) {
            var sceneVisual = ElementCompositionPreview.GetElementVisual(scene);
            var sceneCompositor = sceneVisual.Compositor;

            var ambiantLight = sceneCompositor.CreateAmbientLight();
            ambiantLight.Color = Colors.White;
            ambiantLight.Targets.Add(sceneVisual);
            _LightsList.Add(ambiantLight);
        }

        /// <summary>
        /// Add point light to the scene at the element coordinates
        /// Use options dictionnary to specify custom properties like light intensity
        /// </summary>
        /// <param name="scene">The Grid to add the light to</param>
        /// <param name="element">Use the element's coordinates to place the light</param>
        /// <param name="options"></param>
        public static void AddPointLight(Grid scene, Grid element, Dictionary<string,object> options = null) {
            var transform = element.TransformToVisual(scene);
            var coordinates = transform.TransformPoint(new Point(0, 0));
            var x = (float)(coordinates.X + element.ActualHeight / 2);
            var y = (float)(coordinates.Y + element.ActualWidth / 2);

            float z = 15;
            Color color = Colors.White;

            if (options != null) {
                z = options.ContainsKey("z") ? (float)options["z"] : z;

                var condition = options.ContainsKey("condition") ? (string)options["condition"] : "day";
                if (condition.Contains("night")) {
                    color = Color.FromArgb(255, 247, 202, 24);
                    z = 30;
                }
            }

            var sceneVisual = ElementCompositionPreview.GetElementVisual(scene);
            var sceneCompositor = sceneVisual.Compositor;

            var light = sceneCompositor.CreatePointLight();
            light.CoordinateSpace = sceneVisual;
            light.Color = color;
            light.Offset = new Vector3(x, y, z);
            light.Targets.Add(sceneVisual);
            _LightsList.Add(light);

            TrackUIElement();

            void TrackUIElement()
            {
                Window.Current.SizeChanged += (s, e) => {
                    var updatedTransform = element.TransformToVisual(scene);
                    var updatedCoordinates = updatedTransform.TransformPoint(new Point(0, 0));
                    var updatedX = (float)(updatedCoordinates.X + element.ActualHeight / 2);
                    var updatedY = (float)(updatedCoordinates.Y + element.ActualWidth / 2);
                    light.Offset = new Vector3(updatedX, updatedY, z);
                };
            }
        }

        public static void AddDistantLight(Grid scene) {
            var sceneVisual = ElementCompositionPreview.GetElementVisual(scene);
            var sceneCompositor = sceneVisual.Compositor;

            var light = sceneCompositor.CreateDistantLight();
            light.CoordinateSpace = sceneVisual;
            light.Color = Colors.White;
            light.Direction = new Vector3(0,0,0) - new Vector3(0, 200f, 100f);
            light.Targets.Add(sceneVisual);
            _LightsList.Add(light);
        }

        public static void AddSpotLight(Grid scene, Grid element) {
            var sceneVisual = ElementCompositionPreview.GetElementVisual(scene);
            var sceneCompositor = sceneVisual.Compositor;

            var transform = element.TransformToVisual(scene);
            var coordinates = transform.TransformPoint(new Point(0, 0));
            var x = (float)(coordinates.X + element.ActualHeight / 2);
            var y = (float)(coordinates.Y + element.ActualWidth / 2);
            float z = 30;

            var light = sceneCompositor.CreateSpotLight();
            light.CoordinateSpace = sceneVisual;
            light.InnerConeAngleInDegrees = 20;
            light.InnerConeColor = Colors.White;
            light.OuterConeAngleInDegrees = 45;
            light.OuterConeColor = Colors.Yellow;
            //light.Direction = new Vector3(0, 0, -100);
            light.Offset = new Vector3(x, y, z);

            light.Targets.Add(sceneVisual);
            _LightsList.Add(light);
        }
        #endregion lights


        #region background
        static Grid PaintBackground(Grid scene, CurrentDataPoint current, DayDataPoint day) {
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
                    color2 = Color.FromArgb(255, 135, 211, 124);
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

        #endregion background


        static Grid AddWeatherIconCondition(Grid scene, CurrentDataPoint current) {
            var topScene = new Grid() {
                Name = "PrimaryConditionScene",
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 200, 0, 0)
            };


            var condition = current.Icon;
            Canvas weatherCondition = null;

            switch (condition) {
                case "clear-day":
                    weatherCondition = CreateClearDayIcon();
                    break;
                case "clear-night":
                    weatherCondition = CreateClearNightIcon();
                    break;
                case "partly-cloudy-day":
                    weatherCondition = CreatePartlyCloudyDayIcon();
                    break;
                case "partly-cloudy-night":
                    weatherCondition = CreatePartlyCloudyNightIcon();
                    break;
                case "cloudy":
                    weatherCondition = _IsDay == true ?
                        CreateCloudyDayIcon() :
                        CreateCloudyNightIcon();

                    break;
                case "rain":
                    weatherCondition = CreateRainIcon();
                    break;
                case "sleet": // neige fondu
                    weatherCondition = CreateSnowIcon();
                    break;
                case "snow":
                    weatherCondition = CreateSnowIcon();
                    break;
                case "wind":
                    weatherCondition = CreateWindIcon();
                    break;
                case "fog":
                    weatherCondition = CreateFogIcon();
                    break;
                default:
                    break;
            }

            //weatherCondition = GetWeatherIcon();

            topScene.Children.Add(weatherCondition);
            scene.Children.Add(topScene);
            return scene;
        }

        // For tests purpose
        static Canvas GetWeatherIcon() {
            //return CreateClearDayIcon();
            //return CreateClearNightIcon();
            //return CreatePartlyCloudyDayIcon();
            //return CreatePartlyCloudyNightIcon();
            //return CreateCloudyDayIcon();
            //return CreateCloudyNightIcon();

            return CreateRainIcon();
            //return CreateSnowIcon();
            //return CreateWindIcon();
            //return CreateFogIcon();
        }

        static Grid AddAnimations(Grid scene, string condition) {
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


        #region animated scenes

        static Canvas CreateRainyScene() {
            var container = new Canvas();
            var rand = new Random();

            for (int i = 0; i < 25; i++) {
                var x = rand.Next(0, (int)_size.Width);
                var duration = rand.Next(2, 10);
                var delay = rand.Next(0, 10);

                var line = CreateLine(x, duration, delay);
                container.Children.Add(line);
            }

            return container;
        }
        
        static Canvas CreateStarsScene(int starsNumber = 35) {
            var container = new Canvas();
            var rand = new Random();

            var x = (int)_size.Width;

            for (int i = 0; i < starsNumber; i++) {
                var coord = new Vector2(rand.Next(-x, x), rand.Next(0, 400));
                var duration = rand.Next(10, 20);
                var radius = rand.Next(1, 7);

                var star = CreateStar(duration, coord, radius);
                container.Children.Add(star);
            }

            return container;
        }

        static Canvas CreateSnowScene() {
            var container = new Canvas();
            var rand = new Random();

            for (int i = 0; i < 20; i++) {
                var x = rand.Next(0, (int)_size.Width);
                var size = rand.Next(4, 20);
                var duration = rand.Next(10, 20);
                var delay = rand.Next(0, 10);

                var snow = CreateSnowBall(x, size, duration, delay);
                container.Children.Add(snow);
            }

            return container;
        }

        static Canvas CreateSleetScene() {
            var container = new Canvas();
            var random = new Random();

            for (int i = 0; i < 10; i++) {
                var x = random.Next(0, (int)_size.Width);
                var size = random.Next(4, 20);
                var duration = random.Next(10, 20);
                var delay = random.Next(0, 10);

                var snow = CreateSnowBall(x, size, duration, delay);
                container.Children.Add(snow);
            }

            for (int i = 0; i < 10; i++) {
                var x = random.Next(0, (int)_size.Width);
                var duration = random.Next(2, 10);
                var delay = random.Next(0, 10);

                var line = CreateLine(x, duration, delay);
                container.Children.Add(line);
            }

            return container;
        }

        static Canvas CreateFogScene() {
            var container = new Canvas();
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            var fog1 = CreateFogIcon();
            var fogVisual1 = CreateFogVisual(fog1, compositor, 170, -100);

            var fog2 = CreateFogIcon(130);
            var fogVisual2 = CreateFogVisual(fog2, compositor, 90, (float)_size.Width, 0, 20);

            var fog3 = CreateFogIcon(120);
            var fogVisual3 = CreateFogVisual(fog3, compositor, 120, (float)_size.Width / 2, -90, 15);

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

            var endX = (float)_size.Width;
            var endY = (float)_size.Height;

            var random = new Random();

            for (int i = 0; i < 6; i++) {
                var startX = random.Next(100);
                var startY = random.Next(100);

                var leaf = CreateLeafIcon(40);
                var leafVisual = CreateLeafVisual(
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

        //-------------------------
        // WEATHER ICONS CONDITIONS
        //-------------------------
        #region animated icons
        static Grid CreateWeatherIcon(string condition) {
            var grid = new Grid();

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

            return grid;
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

            ElementCompositionPreview.SetElementChildVisual(grid, container);

            // ------------
            // ANIMATION //
            // ------------
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, 1f);
            animation.InsertKeyFrame(1f, 0f);
            animation.Duration = TimeSpan.FromSeconds(duration);
            animation.DelayTime = TimeSpan.FromSeconds(duration / 2);
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

        static Line CreateLine(int x, int duration, int delay) {
            var y = -70;
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
                StrokeThickness = 2
            };

            // ANIMATION
            var _visual = ElementCompositionPreview.GetElementVisual(line);
            var _compositor = _visual.Compositor;

            var _animation = _compositor.CreateVector2KeyFrameAnimation();
            //_animation.InsertKeyFrame(1f, new System.Numerics.Vector2(-coord, coord));
            _animation.InsertKeyFrame(1f, new Vector2(0, (float)_size.Height));
            _animation.Duration = TimeSpan.FromSeconds(duration);
            _animation.DelayTime = TimeSpan.FromSeconds(delay);
            _animation.IterationBehavior = AnimationIterationBehavior.Forever;

            _visual.StartAnimation("Offset.xy", _animation);

            return line;
        }

        static Canvas CreateSnowBall(int x, double size, double duration, double delay) {
            var container = new Canvas();
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            var snow = new Ellipse() {
                Height = size,
                Width = size,
                Fill = new SolidColorBrush(Colors.White)
            };

            var snowVisual = ElementCompositionPreview.GetElementVisual(snow);

            snowVisual.Offset = new Vector3(x, 0, 0);

            var animationOffset = compositor.CreateVector2KeyFrameAnimation();
            animationOffset.InsertKeyFrame(0f, new Vector2(x, 0f));
            animationOffset.InsertKeyFrame(1f, new Vector2(x, (float)_size.Height));
            animationOffset.Duration = TimeSpan.FromSeconds(duration);
            animationOffset.DelayTime = TimeSpan.FromSeconds(delay);
            animationOffset.IterationBehavior = AnimationIterationBehavior.Forever;

            var animationFade = compositor.CreateScalarKeyFrameAnimation();
            animationFade.InsertKeyFrame(0f, 1);
            animationFade.InsertKeyFrame(1f, 0);
            animationFade.Duration = TimeSpan.FromSeconds(duration);
            animationFade.DelayTime = TimeSpan.FromSeconds(delay);
            animationFade.IterationBehavior = AnimationIterationBehavior.Forever;

            snowVisual.StartAnimation("Offset.xy", animationOffset);
            snowVisual.StartAnimation("Opacity", animationFade);

            container.Children.Add(snow);
            containerVisual.Children.InsertAtTop(snowVisual);
            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);

            return container;
        }

        static PackIconModern CreateLeafIcon(double size) {
            return new PackIconModern() {
                Height = size,
                Width = size,
                Kind = PackIconModernKind.TreeLeaf
            };
        }

        static PackIconModern CreateFogIcon(double size = 150) {
            return new PackIconModern() {
                Height = size,
                Width = size,
                Kind = PackIconModernKind.Cloud
            };
        }

        static Canvas CreateClearDayIcon() {
            var sunRadius = 100;
            var sunExtRadius = 140;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-110,0,0,0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(sunRadius, sunRadius);

            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            var sunVisual = CreateSunVisual(compositor, sunRadius);
            var sunBeamVisual = BuildSunBeamVisual();

            var scaleAnimation = CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);

            AnimateSun(sunVisual, scaleAnimation);
            AnimateSunBeam(sunBeamVisual, scaleAnimation);

            containerVisual.Children.InsertAtTop(sunVisual);
            containerVisual.Children.InsertAtBottom(sunBeamVisual);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);

            return container;

            SpriteVisual BuildSunBeamVisual()
            {
                // SUNBEAM
                var sunbeamSurface = ImageLoader.Instance.LoadCircle(sunExtRadius, Color.FromArgb(255, 245, 215, 110));
                var visual = compositor.CreateSpriteVisual();

                visual.Brush = compositor.CreateSurfaceBrush(sunbeamSurface.Surface);
                visual.Size = new Vector2(sunExtRadius, sunExtRadius);
                visual.CenterPoint = new Vector3(sunExtRadius / 2, sunExtRadius / 2, 0);
                visual.Offset = new Vector3((sunRadius - sunExtRadius) / 2, (sunRadius - sunExtRadius) / 2, 0);

                return visual;
            }

            void AnimateSun(SpriteVisual visual, Vector2KeyFrameAnimation animation)
            {
                StartScaleAnimation(sunVisual, scaleAnimation);
            }

            void AnimateSunBeam(SpriteVisual visual, Vector2KeyFrameAnimation animation)
            {
                animation.InsertKeyFrame(0f, new Vector2(0.8f, 0.8f));
                animation.Duration = TimeSpan.FromSeconds(3);
                visual.StartAnimation("Scale.xy", animation);
            }
        }
        
        static Canvas CreatePartlyCloudyDayIcon() {
            float sunRadius = 60;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-50, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(sunRadius, sunRadius);
            
            var sunVisual = CreateSunVisual(compositor, sunRadius);

            var cloudImage = CreateCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);

            container.Children.Add(cloudImage);

            containerVisual.Children.InsertAtBottom(sunVisual);
            containerVisual.Children.InsertAtTop(cloudVisual);

            AddShadow(cloudImage, compositor, cloudVisual, containerVisual);

            // ANIMATIONS
            // ----------
            var scaleAnimation = CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);
            StartScaleAnimation(sunVisual, scaleAnimation);

            var offsetAnimation = CreateOffsetAnimation(compositor, -30, 6);
            cloudVisual.StartAnimation("Offset.x", offsetAnimation);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        static Canvas CreateCloudyDayIcon() {
            float sunRadius = 60;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-50, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(sunRadius, sunRadius);

            var sunVisual = CreateSunVisual(compositor, sunRadius);

            var cloudImage = CreateCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);

            var cloudImage2 = CreateCloudImage(70);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);
            cloudVisual2.Offset = new Vector3(-30, -30, 0);

            var cloudImage3 = CreateCloudImage(60);
            var cloudVisual3 = ElementCompositionPreview.GetElementVisual(cloudImage3);
            cloudVisual3.Offset = new Vector3(-70, -50, 0);

            container.Children.Add(cloudImage);
            container.Children.Add(cloudImage2);
            container.Children.Add(cloudImage3);

            containerVisual.Children.InsertAtBottom(sunVisual);
            containerVisual.Children.InsertAtTop(cloudVisual);
            containerVisual.Children.InsertAtTop(cloudVisual2);
            containerVisual.Children.InsertAtBottom(cloudVisual3);

            AddShadow(cloudImage, compositor, cloudVisual, containerVisual);
            AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);

            // ----------
            // ANIMATIONS
            // ----------
            var scaleAnimation = CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);
            StartScaleAnimation(sunVisual, scaleAnimation);

            var offsetAnimation = CreateOffsetAnimation(compositor, -30, 6);
            cloudVisual.StartAnimation("Offset.x", offsetAnimation);

            offsetAnimation.Duration = TimeSpan.FromSeconds(5);
            offsetAnimation.InsertKeyFrame(1f, 40);
            cloudVisual2.StartAnimation("Offset.x", offsetAnimation);

            //offsetAnimation
            offsetAnimation.Duration = TimeSpan.FromSeconds(7);
            cloudVisual3.StartAnimation("Offset.x", offsetAnimation);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);

            return container;
        }

        static Canvas CreateRainIcon() {
            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-60, 0, 0, 0)
            };
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            // ------
            // CLOUDS
            // ------
            var cloudImage1 = CreateCloudImage(90);
            var cloudVisual1 = ElementCompositionPreview.GetElementVisual(cloudImage1);

            var cloudImage2 = CreateCloudImage(70);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);

            var cloudImage3 = CreateCloudImage(50);
            var cloudVisual3 = ElementCompositionPreview.GetElementVisual(cloudImage3);

            container.Children.Add(cloudImage1);
            container.Children.Add(cloudImage2);
            container.Children.Add(cloudImage3);

            containerVisual.Children.InsertAtTop(cloudVisual1);
            containerVisual.Children.InsertAtTop(cloudVisual2);
            containerVisual.Children.InsertAtTop(cloudVisual3);

            AddShadow(cloudImage1, compositor, cloudVisual1, containerVisual);
            AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);
            AddShadow(cloudImage3, compositor, cloudVisual3, containerVisual);

            // ----------
            // ANIMATIONS
            // ----------
            var random = new Random();
            var minOffset = -70;
            var maxOffset = 70;
            var minDuration = 5;
            var maxDuration = 12;

            var endOffset = random.Next(minOffset, maxOffset);
            var duration = random.Next(minDuration, maxDuration);

            var offsetAnimation = CreateOffsetAnimation(compositor, endOffset, duration);

            // cloud 1
            cloudVisual1.StartAnimation("Offset.x", offsetAnimation);

            // cloud 2
            offsetAnimation.InsertKeyFrame(1f, endOffset + 30);
            offsetAnimation.Duration = TimeSpan.FromSeconds(duration - 3);

            cloudVisual2.StartAnimation("Offset.x", offsetAnimation);

            // cloud 3
            offsetAnimation.InsertKeyFrame(1f, endOffset - 30);

            cloudVisual3.StartAnimation("Offset.x", offsetAnimation);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            
            return container;
        }

        static Canvas CreateSnowIcon() {
            var container = new Canvas() { Margin = new Thickness(-35,0,0,0) };
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            var iconSnow = new PackIconModern() {
                Height = 70,
                Width = 70,
                Kind = PackIconModernKind.Snowflake
            };

            var iconSnowVisual = ElementCompositionPreview.GetElementVisual(iconSnow);
            

            // ANIMATIONS
            var animationRotate = compositor.CreateScalarKeyFrameAnimation();
            animationRotate.InsertKeyFrame(0f, 0f);
            animationRotate.InsertKeyFrame(1f, -5f);
            animationRotate.Duration = TimeSpan.FromSeconds(5);
            animationRotate.IterationBehavior = AnimationIterationBehavior.Forever;
            animationRotate.Direction = AnimationDirection.Alternate;

            var animationFade = compositor.CreateScalarKeyFrameAnimation();
            animationFade.InsertKeyFrame(0f, 1f);
            animationFade.InsertKeyFrame(1f, 0.2f);
            animationFade.Duration = TimeSpan.FromSeconds(3);
            animationFade.IterationBehavior = AnimationIterationBehavior.Forever;
            animationFade.Direction = AnimationDirection.Alternate;

            var animationScale = CreateScaleAnimation(compositor, new Vector2(1.3f, 1.3f), 5);
            iconSnowVisual.RotationAxis = new Vector3(0, 0, 1);
            iconSnowVisual.CenterPoint = new Vector3((float)iconSnow.Height / 2, (float)iconSnow.Width / 2, 0);

            iconSnowVisual.StartAnimation("RotationAngle", animationRotate);
            iconSnowVisual.StartAnimation("Opacity", animationFade);
            iconSnowVisual.StartAnimation("Scale.xy", animationScale);

            container.Children.Add(iconSnow);
            containerVisual.Children.InsertAtTop(iconSnowVisual);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        static Grid CreateSleetIcon() {
            var container = new Grid();
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();
            var canvas = new Canvas();

            ElementCompositionPreview.SetElementChildVisual(canvas, containerVisual);
            container.Children.Add(canvas);
            return container;
        }

        static Canvas CreateWindIcon() {
            var container = new Canvas() { Margin = new Thickness(-40,0,0,0) };
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            var windBitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/Icons/wind.png"));
            var windImageControl = new Image() {
                Source = windBitmapImage,
                Height = 90,
                Width = 90,
            };

            container.Children.Add(windImageControl);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        static Canvas CreateFogIcon() {
            var container = new Canvas() { Margin = new Thickness(-40, 0, 0, 0) };
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            var windBitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/Icons/fog.png"));
            var windImageControl = new Image() {
                Source = windBitmapImage,
                Height = 90,
                Width = 90,
            };

            container.Children.Add(windImageControl);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        static Canvas CreateClearNightIcon() {
            var moonRadius = 140;
            var moonExtRadius = 150;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-130, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(moonRadius, moonRadius);

            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            var moonVisual = CreateMoonVisual(compositor, moonRadius, Color.FromArgb(255, 236, 240, 241));
            var moonBeamVisual = CreateMoonBeamVisual();

            var scaleAnimation = CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);

            AnimateMoonBeam(moonBeamVisual, scaleAnimation);

            containerVisual.Children.InsertAtTop(moonVisual);
            containerVisual.Children.InsertAtBottom(moonBeamVisual);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);

            // black orbs shadow
            //var shadowBallVisual1 = CreateEllipseVisual(compositor, 20, Color.FromArgb(255, 189, 195, 199));
            //shadowBallVisual1.Offset = new Vector3(moonRadius/2, moonRadius/2, 0);
            //containerVisual.Children.InsertAtTop(shadowBallVisual1);

            return container;

            SpriteVisual CreateMoonBeamVisual()
            {
                var visual = CreateEllipseVisual(compositor, moonExtRadius, Color.FromArgb(155, 255, 255, 255));
                visual.Offset = new Vector3((moonRadius - moonExtRadius) / 2, (moonRadius - moonExtRadius) / 2, 0);
                return visual;
            }
            
            void AnimateMoonBeam(SpriteVisual visual, Vector2KeyFrameAnimation animation)
            {
                animation.InsertKeyFrame(0f, new Vector2(0.8f, 0.8f));
                animation.Duration = TimeSpan.FromSeconds(9);
                visual.StartAnimation("Scale.xy", animation);
            }
        }

        static Canvas CreatePartlyCloudyNightIcon() {
            var moonRadius = 140;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-120, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(moonRadius*4, moonRadius*4);

            var moonVisual = CreateMoonVisual(compositor, moonRadius, Color.FromArgb(255, 245, 215, 110));

            var cloudImage = CreateDarkCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);

            var cloudImage2 = CreateDarkCloudImage(100);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);
            cloudVisual2.Offset = new Vector3(80, 80, 0);

            var offsetAnimation = CreateOffsetAnimation(compositor, -30, 6);
            cloudVisual.StartAnimation("Offset.x", offsetAnimation);

            offsetAnimation.InsertKeyFrame(1f, 60);
            offsetAnimation.Duration = TimeSpan.FromSeconds(8);
            cloudVisual2.StartAnimation("Offset.x", offsetAnimation);

            container.Children.Add(cloudImage);
            container.Children.Add(cloudImage2);

            containerVisual.Children.InsertAtBottom(moonVisual);
            containerVisual.Children.InsertAtTop(cloudVisual);
            containerVisual.Children.InsertAtTop(cloudVisual2);


            AddShadow(cloudImage, compositor, cloudVisual, containerVisual);
            AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
            
        }

        static Canvas CreateCloudyNightIcon() {
            var moonRadius = 120;

            var container = new Canvas() {
                Margin = new Thickness(-60, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(moonRadius *2, moonRadius *2);

            var moonVisual = CreateMoonVisual(compositor, moonRadius, Color.FromArgb(255, 245, 215, 110));

            // ------
            // CLOUDS
            // ------
            var cloudImage = CreateDarkCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);
            cloudVisual.Offset = new Vector3(0, 50, 0);

            var cloudImage2 = CreateDarkCloudImage(70);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);
            cloudVisual2.Offset = new Vector3(-30, -20, 0);

            var cloudImage3 = CreateDarkCloudImage(70);
            var cloudVisual3 = ElementCompositionPreview.GetElementVisual(cloudImage3);
            cloudVisual3.Offset = new Vector3(-90, -40, 0);

            container.Children.Add(cloudImage);
            container.Children.Add(cloudImage2);
            container.Children.Add(cloudImage3);

            containerVisual.Children.InsertAtBottom(moonVisual);
            containerVisual.Children.InsertAtTop(cloudVisual);
            containerVisual.Children.InsertAtTop(cloudVisual2);
            containerVisual.Children.InsertAtBottom(cloudVisual3);

            AddShadow(cloudImage, compositor, cloudVisual, containerVisual);
            AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);
            AddShadow(cloudImage3, compositor, cloudVisual3, containerVisual);

            // ----------
            // ANIMATIONS
            // ----------
            var animationOffset = CreateOffsetAnimation(compositor, -30, 6);
            cloudVisual.StartAnimation("Offset.x", animationOffset);

            animationOffset.Duration = TimeSpan.FromSeconds(5);
            animationOffset.InsertKeyFrame(1f, 70);

            cloudVisual2.StartAnimation("Offset.x", animationOffset);

            animationOffset.Duration = TimeSpan.FromSeconds(7);
            cloudVisual3.StartAnimation("Offset.x", animationOffset);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);

            return container;
        }

        #endregion animated icons

        #region visuals
        private static SpriteVisual CreateSunVisual(Compositor compositor, float sunRadius) {
            return CreateEllipseVisual(compositor, sunRadius, Color.FromArgb(255, 249, 179, 47)); ;
        }

        private static SpriteVisual CreateMoonVisual(Compositor compositor, float moonRadius, Color color) {
            return CreateEllipseVisual(compositor, moonRadius, color);
        }

        static Visual CreateFogVisual(FrameworkElement fog, Compositor compositor,
            float y = 50, float startX = 0, float endX = 200,
            double duration = 10) {

            var fogVisual = ElementCompositionPreview.GetElementVisual(fog);

            var animationOffset = compositor.CreateVector2KeyFrameAnimation();
            animationOffset.InsertKeyFrame(0, new Vector2(startX, y));
            animationOffset.InsertKeyFrame(1f, new Vector2(endX, y));
            animationOffset.Duration = TimeSpan.FromSeconds(duration);
            animationOffset.IterationBehavior = AnimationIterationBehavior.Forever;
            animationOffset.Direction = AnimationDirection.Alternate;

            var animationFade = compositor.CreateScalarKeyFrameAnimation();
            animationFade.InsertKeyFrame(0, .6f);
            animationFade.InsertKeyFrame(1, .3f);
            animationFade.Duration = TimeSpan.FromSeconds(duration);
            animationFade.Direction = AnimationDirection.Alternate;
            animationFade.IterationBehavior = AnimationIterationBehavior.Forever;

            fogVisual.StartAnimation("Offset.xy", animationOffset);
            fogVisual.StartAnimation("Opacity", animationFade);

            return fogVisual;
        }

        static Visual CreateLeafVisual(
            FrameworkElement leaf, Compositor compositor,
            double duration = 3, double delay = 0,
            float startX = 0, float startY = 0,
            float endX = 250, float endY = 350) {

            var leafVisual = ElementCompositionPreview.GetElementVisual(leaf);

            var animationOffset = compositor.CreateVector2KeyFrameAnimation();
            animationOffset.InsertKeyFrame(0f, new Vector2(startX, startY));
            animationOffset.InsertKeyFrame(1f, new Vector2(endX, endY));
            animationOffset.Duration = TimeSpan.FromSeconds(duration);
            animationOffset.DelayTime = TimeSpan.FromSeconds(delay);
            animationOffset.IterationBehavior = AnimationIterationBehavior.Forever;

            var animationFade = compositor.CreateScalarKeyFrameAnimation();
            animationFade.InsertKeyFrame(0f, 1);
            animationFade.InsertKeyFrame(1f, 0);
            animationFade.Duration = TimeSpan.FromSeconds(duration);
            animationFade.DelayTime = TimeSpan.FromSeconds(delay);
            animationFade.IterationBehavior = AnimationIterationBehavior.Forever;

            var animationRotate = compositor.CreateScalarKeyFrameAnimation();
            animationRotate.InsertKeyFrame(0f, 0f);
            animationRotate.InsertKeyFrame(1f, -5f);
            animationRotate.Duration = TimeSpan.FromSeconds(5);
            animationRotate.DelayTime = TimeSpan.FromSeconds(delay);
            animationRotate.IterationBehavior = AnimationIterationBehavior.Forever;
            animationRotate.Direction = AnimationDirection.Alternate;

            leafVisual.RotationAxis = new Vector3(0, 0, 1);
            leafVisual.CenterPoint = new Vector3((float)leaf.Height / 2, (float)leaf.Width / 2, 0);

            leafVisual.StartAnimation("Offset.xy", animationOffset);
            leafVisual.StartAnimation("Opacity", animationFade);
            leafVisual.StartAnimation("RotationAngle", animationRotate);

            return leafVisual;
        }


        static SpriteVisual CreateEllipseVisual(Compositor compositor, float radius, Color color) {
            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            var surface = ImageLoader.Instance.LoadCircle(radius, color);
            var visual = compositor.CreateSpriteVisual();

            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new Vector2(radius, radius);
            visual.CenterPoint = new Vector3(radius / 2, radius / 2, 0);
            visual.RotationAxis = new Vector3(0, 0, 1);

            return visual;
        }

        private static SpriteVisual CreateCloudVisual(Compositor compositor, float size) {
            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            ManagedSurface surface = ImageLoader.Instance.LoadFromUri(new Uri("ms-appx:///Assets/Icons/cloudy.png"));

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new Vector2(size, size);

            return visual;
        }

        private static SpriteVisual CreateDarkCloudVisual(Compositor compositor, float size) {
            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            ManagedSurface surface = ImageLoader.Instance.LoadFromUri(new Uri("ms-appx:///Assets/Icons/dark_cloud_png"));

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new Vector2(size, size);

            return visual;
        }

        static Image CreateCloudImage(double size) {
            var cloudBitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/Icons/cloudy.png"));
            var cloudImageControl = new Image() {
                Source = cloudBitmapImage,
                Height = size,
                Width = size,
            };

            return cloudImageControl;
        }

        static Image CreateDarkCloudImage(double size) {
            var cloudImage = CreateCloudImage(size);
            cloudImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Icons/dark_cloud.png"));
            return cloudImage;
        }

        static BitmapIcon CreateCloudIcon(double size, Brush brush) {
            var cloudIcon = new BitmapIcon() {
                UriSource = new Uri("ms-appx:///Assets/Icons/cloudy.png"),
                Height = size,
                Width = size,
                Foreground = brush
            };

            return cloudIcon;
        }

        static void AddShadow(Image image, Compositor compositor, Visual shadowTarget, ContainerVisual shadowHost) {
            var shadow = compositor.CreateDropShadow();
            shadow.Offset = new Vector3(5, 5, 0);
            shadow.Mask = image.GetAlphaMask();
            shadow.BlurRadius = 10;
            shadow.Opacity = .3f;

            var shadowVisual = compositor.CreateSpriteVisual();
            shadowVisual.Size = new Vector2((float)image.ActualHeight, (float)image.ActualWidth);
            shadowVisual.Shadow = shadow;

            shadowHost.Children.InsertBelow(shadowVisual, shadowTarget);

            var shadowHostSyncAnimation = compositor.CreateExpressionAnimation("reference.Offset");
            shadowHostSyncAnimation.SetReferenceParameter("reference", shadowTarget);
            shadowVisual.StartAnimation("Offset", shadowHostSyncAnimation);
        }
        
        #endregion visuals

        #region animations functions
        static Vector2KeyFrameAnimation CreateScaleAnimation(Compositor compositor, Vector2 ScaleXY, double duration) {
            // ANIMATIONS
            var animation = compositor.CreateVector2KeyFrameAnimation();
            animation.InsertKeyFrame(0f, new Vector2(1, 1));
            animation.InsertKeyFrame(1f, ScaleXY);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;
            animation.Duration = TimeSpan.FromSeconds(duration);

            return animation;
        }

        static ScalarKeyFrameAnimation CreateOffsetAnimation(Compositor compositor, float endKeyFrame, double duration) {
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, 0);
            animation.InsertKeyFrame(1f, endKeyFrame);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;
            animation.Duration = TimeSpan.FromSeconds(duration);

            return animation;
        }

        static void StartScaleAnimation(SpriteVisual visual, Vector2KeyFrameAnimation animation) {
            visual.StartAnimation("Scale.xy", animation);
        }

        static void StartOffsetAnimation(Visual visual, ScalarKeyFrameAnimation animation) {
            visual.StartAnimation("Offset.x", animation);
        }

        static void StartOffsetAnimation(SpriteVisual visual, ScalarKeyFrameAnimation animation, float offset, double seconds) {
            animation.InsertKeyFrame(1f, offset);
            animation.Duration = TimeSpan.FromSeconds(seconds);

            visual.StartAnimation("Offset.x", animation);
        }

        #endregion animations functions
    }
}
