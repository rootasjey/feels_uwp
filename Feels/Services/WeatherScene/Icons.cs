using DarkSkyApi.Models;
using Feels.Composition;
using MahApps.Metro.IconPacks;
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Feels.Services.WeatherScene {
    public class Icons {
        public static Grid AddWeatherIconCondition(Grid scene, CurrentDataPoint current, DayDataPoint day, bool isDay = true) {
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
                    weatherCondition = CreateClearNightIcon(day);
                    break;
                case "partly-cloudy-day":
                    weatherCondition = CreatePartlyCloudyDayIcon();
                    break;
                case "partly-cloudy-night":
                    weatherCondition = CreatePartlyCloudyNightIcon(day);
                    break;
                case "cloudy":
                    weatherCondition = isDay == true ?
                        CreateCloudyDayIcon() :
                        CreateCloudyNightIcon(day);

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

            //weatherCondition = GetWeatherIcon(); // test prupose
            topScene.Children.Add(weatherCondition);
            scene.Children.Add(topScene);
            return scene;
        }

        // For tests purpose
        public static Canvas GetWeatherIcon() {
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

        public static Grid CreateWeatherIcon(string condition) {
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

        public static Grid CreateStar(int duration, Vector2 coord, int radius) {
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

        public static Grid CreateCloud(int duration, int endX, int marginTop) {
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

        public static Line CreateLine(int x, int duration, int delay, Size size) {
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
            _animation.InsertKeyFrame(1f, new Vector2(0, (float)(size.Height + 300)));
            _animation.Duration = TimeSpan.FromSeconds(duration);
            _animation.DelayTime = TimeSpan.FromSeconds(delay);
            _animation.IterationBehavior = AnimationIterationBehavior.Forever;

            _visual.StartAnimation("Offset.xy", _animation);

            return line;
        }

        public static Canvas CreateSnowBall(int x, double size, double duration, double delay, Size screenSize) {
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
            animationOffset.InsertKeyFrame(1f, new Vector2(x, (float)screenSize.Height));
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

        public static PackIconModern CreateLeafIcon(double size) {
            return new PackIconModern() {
                Height = size,
                Width = size,
                Kind = PackIconModernKind.TreeLeaf
            };
        }

        public static PackIconModern CreateFogIcon(double size = 150) {
            return new PackIconModern() {
                Height = size,
                Width = size,
                Kind = PackIconModernKind.Cloud
            };
        }

        private static Canvas CreateClearDayIcon() {
            var sunRadius = 100;
            var sunExtRadius = 140;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-110, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(sunRadius, sunRadius);

            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            var sunVisual = Visuals.CreateSunVisual(compositor, sunRadius);
            var sunBeamVisual = BuildSunBeamVisual();

            var scaleAnimation = Animations.CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);

            AnimateSun(sunVisual, scaleAnimation);
            AnimateSunBeam(sunBeamVisual, scaleAnimation);

            containerVisual.Children.InsertAtTop(sunVisual);
            containerVisual.Children.InsertAtBottom(sunBeamVisual);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);

            return container;

            SpriteVisual BuildSunBeamVisual() {
                // SUNBEAM
                var sunbeamSurface = ImageLoader.Instance.LoadCircle(sunExtRadius, Color.FromArgb(255, 245, 215, 110));
                var visual = compositor.CreateSpriteVisual();

                visual.Brush = compositor.CreateSurfaceBrush(sunbeamSurface.Surface);
                visual.Size = new Vector2(sunExtRadius, sunExtRadius);
                visual.CenterPoint = new Vector3(sunExtRadius / 2, sunExtRadius / 2, 0);
                visual.Offset = new Vector3((sunRadius - sunExtRadius) / 2, (sunRadius - sunExtRadius) / 2, 0);

                return visual;
            }

            void AnimateSun(SpriteVisual visual, Vector2KeyFrameAnimation animation) {
                Animations.StartScaleAnimation(sunVisual, scaleAnimation);
            }

            void AnimateSunBeam(SpriteVisual visual, Vector2KeyFrameAnimation animation) {
                animation.InsertKeyFrame(0f, new Vector2(0.8f, 0.8f));
                animation.Duration = TimeSpan.FromSeconds(3);
                visual.StartAnimation("Scale.xy", animation);
            }
        }

        private static Canvas CreatePartlyCloudyDayIcon() {
            float sunRadius = 60;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-50, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(sunRadius, sunRadius);

            var sunVisual = Visuals.CreateSunVisual(compositor, sunRadius);

            var cloudImage = Visuals.CreateCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);

            container.Children.Add(cloudImage);

            containerVisual.Children.InsertAtBottom(sunVisual);
            containerVisual.Children.InsertAtTop(cloudVisual);

            Visuals.AddShadow(cloudImage, compositor, cloudVisual, containerVisual);

            // ANIMATIONS
            // ----------
            var scaleAnimation = Animations.CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);
            Animations.StartScaleAnimation(sunVisual, scaleAnimation);

            var offsetAnimation = Animations.CreateOffsetAnimation(compositor, -30, 6);
            cloudVisual.StartAnimation("Offset.x", offsetAnimation);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        private static Canvas CreateCloudyDayIcon() {
            float sunRadius = 60;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-50, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(sunRadius, sunRadius);

            var sunVisual = Visuals.CreateSunVisual(compositor, sunRadius);

            var cloudImage = Visuals.CreateCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);

            var cloudImage2 = Visuals.CreateCloudImage(70);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);
            cloudVisual2.Offset = new Vector3(-30, -30, 0);

            var cloudImage3 = Visuals.CreateCloudImage(60);
            var cloudVisual3 = ElementCompositionPreview.GetElementVisual(cloudImage3);
            cloudVisual3.Offset = new Vector3(-70, -50, 0);

            container.Children.Add(cloudImage);
            container.Children.Add(cloudImage2);
            container.Children.Add(cloudImage3);

            containerVisual.Children.InsertAtBottom(sunVisual);
            containerVisual.Children.InsertAtTop(cloudVisual);
            containerVisual.Children.InsertAtTop(cloudVisual2);
            containerVisual.Children.InsertAtBottom(cloudVisual3);

            Visuals.AddShadow(cloudImage, compositor, cloudVisual, containerVisual);
            Visuals.AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);

            // ----------
            // ANIMATIONS
            // ----------
            var scaleAnimation = Animations.CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);
            Animations.StartScaleAnimation(sunVisual, scaleAnimation);

            var offsetAnimation = Animations.CreateOffsetAnimation(compositor, -30, 6);
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

        private static Canvas CreateRainIcon() {
            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-60, 0, 0, 0)
            };
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();

            // ------
            // CLOUDS
            // ------
            var cloudImage1 = Visuals.CreateCloudImage(90);
            var cloudVisual1 = ElementCompositionPreview.GetElementVisual(cloudImage1);

            var cloudImage2 = Visuals.CreateCloudImage(70);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);

            var cloudImage3 = Visuals.CreateCloudImage(50);
            var cloudVisual3 = ElementCompositionPreview.GetElementVisual(cloudImage3);

            container.Children.Add(cloudImage1);
            container.Children.Add(cloudImage2);
            container.Children.Add(cloudImage3);

            containerVisual.Children.InsertAtTop(cloudVisual1);
            containerVisual.Children.InsertAtTop(cloudVisual2);
            containerVisual.Children.InsertAtTop(cloudVisual3);

            Visuals.AddShadow(cloudImage1, compositor, cloudVisual1, containerVisual);
            Visuals.AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);
            Visuals.AddShadow(cloudImage3, compositor, cloudVisual3, containerVisual);

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

            var offsetAnimation = Animations.CreateOffsetAnimation(compositor, endOffset, duration);

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

        private static Canvas CreateSnowIcon() {
            var container = new Canvas() { Margin = new Thickness(-35, 0, 0, 0) };
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

            var animationScale = Animations.CreateScaleAnimation(compositor, new Vector2(1.3f, 1.3f), 5);
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

        private static Grid CreateSleetIcon() {
            var container = new Grid();
            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var containerVisual = compositor.CreateContainerVisual();
            var canvas = new Canvas();

            ElementCompositionPreview.SetElementChildVisual(canvas, containerVisual);
            container.Children.Add(canvas);
            return container;
        }

        private static Canvas CreateWindIcon() {
            var container = new Canvas() { Margin = new Thickness(-40, 0, 0, 0) };
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

        private static Canvas CreateFogIcon() {
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

        private static Canvas CreateClearNightIcon(DayDataPoint day) {
            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-130, -50, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
            var moon = CreateMoon(day.MoonPhase, compositor, Color.FromArgb(255, 236, 240, 241));
            container.Children.Add(moon);

            // black orbs shadow
            //var shadowBallVisual1 = CreateEllipseVisual(compositor, 20, Color.FromArgb(255, 189, 195, 199));
            //shadowBallVisual1.Offset = new Vector3(moonRadius/2, moonRadius/2, 0);
            //containerVisual.Children.InsertAtTop(shadowBallVisual1);

            return container;
        }

        private static Grid CreateMoon(float moonPhase, Compositor compositor, Color color, int radius = 140) {
            var moonExtRadius = 150;
            var container = new Grid();
            var containerVisual = compositor.CreateContainerVisual();

            containerVisual.Size = new Vector2(radius, radius);

            var iconMoon = new PackIconModern() {
                Height = 120,
                Width = 120,
                Foreground = new SolidColorBrush(color)
            };

            if (moonPhase == 0) {
                if (ImageLoader.Instance == null) {
                    ImageLoader.Initialize(compositor);
                }

                var newMoonVisual = Visuals.CreateMoonVisual(compositor, radius, color);
                var newMoonBeamVisual = CreateMoonBeamVisual();
                var scaleAnimation = Animations.CreateScaleAnimation(compositor, new Vector2(1.2f, 1.2f), 5);

                AnimateMoonBeam(newMoonBeamVisual, scaleAnimation);

                containerVisual.Children.InsertAtTop(newMoonVisual);
                containerVisual.Children.InsertAtBottom(newMoonBeamVisual);

            } else if (moonPhase > 0 && moonPhase < .25) {
                iconMoon.Kind = PackIconModernKind.MoonWaxingCrescent;

            } else if (moonPhase == .25) {
                iconMoon.Kind = PackIconModernKind.MoonFirstQuarter;

            } else if (moonPhase > .25 && moonPhase < .5) {
                iconMoon.Kind = PackIconModernKind.MoonWaxingGibbous;

            } else if (moonPhase == .5) {
                if (ImageLoader.Instance == null) {
                    ImageLoader.Initialize(compositor);
                }

                var fullMoonVisual = Visuals.CreateMoonVisual(compositor, radius, color);
                containerVisual.Children.InsertAtTop(fullMoonVisual);

                iconMoon.Visibility = Visibility.Collapsed;

            } else if (moonPhase > .5 && moonPhase < .75) {
                iconMoon.Kind = PackIconModernKind.MoonWaningGibbous;

            } else if (moonPhase == .75) {
                iconMoon.Kind = PackIconModernKind.MoonThirdQuarter;

            } else { // moonPhase > .75
                iconMoon.Kind = PackIconModernKind.MoonWaxingCrescent;
            }

            AnimateMoon(iconMoon);
            container.Children.Add(iconMoon);
            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;

            SpriteVisual CreateMoonBeamVisual() {
                var visual = Visuals.CreateEllipseVisual(compositor, moonExtRadius, Color.FromArgb(155, 255, 255, 255));
                visual.Offset = new Vector3((radius - moonExtRadius) / 2, (radius - moonExtRadius) / 2, 0);
                return visual;
            }

            void AnimateMoonBeam(SpriteVisual visual, Vector2KeyFrameAnimation animation) {
                animation.InsertKeyFrame(0f, new Vector2(0.8f, 0.8f));
                animation.Duration = TimeSpan.FromSeconds(9);
                visual.StartAnimation("Scale.xy", animation);
            }

            void AnimateMoon(UIElement element) {
                var moonVisual = ElementCompositionPreview.GetElementVisual(element);
                var animationOpacity = compositor.CreateScalarKeyFrameAnimation();
                animationOpacity.InsertKeyFrame(0f, 1f);
                animationOpacity.InsertKeyFrame(1f, .7f);
                animationOpacity.IterationBehavior = AnimationIterationBehavior.Forever;
                animationOpacity.Direction = AnimationDirection.Alternate;
                animationOpacity.Duration = TimeSpan.FromSeconds(10);

                moonVisual.StartAnimation("Opacity", animationOpacity);
                containerVisual.Children.InsertAtTop(moonVisual);
            }
        }

        private static Canvas CreatePartlyCloudyNightIcon(DayDataPoint day) {
            var moonRadius = 140;

            var container = new Canvas() {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(-120, -50, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(moonRadius * 4, moonRadius * 4);

            var moon = CreateMoon(day.MoonPhase, compositor, Color.FromArgb(255, 245, 215, 110));

            var cloudImage = Visuals.CreateDarkCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);

            var cloudImage2 = Visuals.CreateDarkCloudImage(100);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);
            cloudVisual2.Offset = new Vector3(80, 80, 0);

            var offsetAnimation = Animations.CreateOffsetAnimation(compositor, -30, 6);
            cloudVisual.StartAnimation("Offset.x", offsetAnimation);

            offsetAnimation.InsertKeyFrame(1f, 60);
            offsetAnimation.Duration = TimeSpan.FromSeconds(8);
            cloudVisual2.StartAnimation("Offset.x", offsetAnimation);

            container.Children.Add(cloudImage);
            container.Children.Add(cloudImage2);

            container.Children.Add(moon);
            containerVisual.Children.InsertAtTop(cloudVisual);
            containerVisual.Children.InsertAtTop(cloudVisual2);


            Visuals.AddShadow(cloudImage, compositor, cloudVisual, containerVisual);
            Visuals.AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);
            return container;
        }

        private static Canvas CreateCloudyNightIcon(DayDataPoint day) {
            var moonRadius = 120;

            var container = new Canvas() {
                Margin = new Thickness(-60, 0, 0, 0)
            };

            var compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;

            var containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = new Vector2(moonRadius * 2, moonRadius * 2);

            var moon = CreateMoon(day.MoonPhase, compositor, Color.FromArgb(255, 245, 215, 110), 120);

            // ------
            // CLOUDS
            // ------
            var cloudImage = Visuals.CreateDarkCloudImage(100);
            var cloudVisual = ElementCompositionPreview.GetElementVisual(cloudImage);
            cloudVisual.Offset = new Vector3(0, 50, 0);

            var cloudImage2 = Visuals.CreateDarkCloudImage(70);
            var cloudVisual2 = ElementCompositionPreview.GetElementVisual(cloudImage2);
            cloudVisual2.Offset = new Vector3(-30, -20, 0);

            var cloudImage3 = Visuals.CreateDarkCloudImage(70);
            var cloudVisual3 = ElementCompositionPreview.GetElementVisual(cloudImage3);
            cloudVisual3.Offset = new Vector3(-90, -40, 0);

            container.Children.Add(moon);
            container.Children.Add(cloudImage);
            container.Children.Add(cloudImage2);
            container.Children.Add(cloudImage3);

            containerVisual.Children.InsertAtTop(cloudVisual);
            containerVisual.Children.InsertAtTop(cloudVisual2);
            containerVisual.Children.InsertAtBottom(cloudVisual3);

            Visuals.AddShadow(cloudImage, compositor, cloudVisual, containerVisual);
            Visuals.AddShadow(cloudImage2, compositor, cloudVisual2, containerVisual);
            Visuals.AddShadow(cloudImage3, compositor, cloudVisual3, containerVisual);

            // ----------
            // ANIMATIONS
            // ----------
            var animationOffset = Animations.CreateOffsetAnimation(compositor, -30, 6);
            cloudVisual.StartAnimation("Offset.x", animationOffset);

            animationOffset.Duration = TimeSpan.FromSeconds(5);
            animationOffset.InsertKeyFrame(1f, 70);

            cloudVisual2.StartAnimation("Offset.x", animationOffset);

            animationOffset.Duration = TimeSpan.FromSeconds(7);
            cloudVisual3.StartAnimation("Offset.x", animationOffset);

            ElementCompositionPreview.SetElementChildVisual(container, containerVisual);

            return container;
        }
    }
}
