using Feels.Composition;
using System;
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Feels.Services.WeatherScene {
    public class Visuals {
        public static SpriteVisual CreateSunVisual(Compositor compositor, float sunRadius) {
            return CreateEllipseVisual(compositor, sunRadius, Color.FromArgb(255, 249, 179, 47)); ;
        }

        public static SpriteVisual CreateMoonVisual(Compositor compositor, float moonRadius, Color color) {
            return CreateEllipseVisual(compositor, moonRadius, color);
        }

        public static Visual CreateFogVisual(FrameworkElement fog, Compositor compositor,
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

        public static Visual CreateLeafVisual(
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

        public static SpriteVisual CreateEllipseVisual(Compositor compositor, float radius, Color color) {
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

        public static SpriteVisual CreateCloudVisual(Compositor compositor, float size) {
            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            ManagedSurface surface = ImageLoader.Instance.LoadFromUri(new Uri("ms-appx:///Assets/Icons/cloudy.png"));

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new Vector2(size, size);

            return visual;
        }

        public static SpriteVisual CreateDarkCloudVisual(Compositor compositor, float size) {
            if (ImageLoader.Instance == null) {
                ImageLoader.Initialize(compositor);
            }

            ManagedSurface surface = ImageLoader.Instance.LoadFromUri(new Uri("ms-appx:///Assets/Icons/dark_cloud_png"));

            var visual = compositor.CreateSpriteVisual();
            visual.Brush = compositor.CreateSurfaceBrush(surface.Surface);
            visual.Size = new Vector2(size, size);

            return visual;
        }

        public static Image CreateCloudImage(double size) {
            var cloudBitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/Icons/cloudy.png"));
            var cloudImageControl = new Image() {
                Source = cloudBitmapImage,
                Height = size,
                Width = size,
            };

            return cloudImageControl;
        }

        public static Image CreateDarkCloudImage(double size) {
            var cloudImage = CreateCloudImage(size);
            cloudImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Icons/dark_cloud.png"));
            return cloudImage;
        }

        public static BitmapIcon CreateCloudIcon(double size, Brush brush) {
            var cloudIcon = new BitmapIcon() {
                UriSource = new Uri("ms-appx:///Assets/Icons/cloudy.png"),
                Height = size,
                Width = size,
                Foreground = brush
            };

            return cloudIcon;
        }

        public static void AddShadow(Image image, Compositor compositor, Visual shadowTarget, ContainerVisual shadowHost) {
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
    }
}
