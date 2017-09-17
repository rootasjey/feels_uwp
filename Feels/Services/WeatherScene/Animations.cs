using System;
using System.Numerics;
using Windows.UI.Composition;

namespace Feels.Services.WeatherScene {
    public class Animations {
        public static Vector2KeyFrameAnimation CreateScaleAnimation(Compositor compositor, Vector2 ScaleXY, double duration) {
            var animation = compositor.CreateVector2KeyFrameAnimation();
            animation.InsertKeyFrame(0f, new Vector2(1, 1));
            animation.InsertKeyFrame(1f, ScaleXY);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;
            animation.Duration = TimeSpan.FromSeconds(duration);

            return animation;
        }

        public static ScalarKeyFrameAnimation CreateOffsetAnimation(Compositor compositor, float endKeyFrame, double duration) {
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0f, 0);
            animation.InsertKeyFrame(1f, endKeyFrame);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            animation.Direction = AnimationDirection.Alternate;
            animation.Duration = TimeSpan.FromSeconds(duration);

            return animation;
        }

        public static void StartScaleAnimation(SpriteVisual visual, Vector2KeyFrameAnimation animation) {
            visual.StartAnimation("Scale.xy", animation);
        }

        public static void StartOffsetAnimation(Visual visual, ScalarKeyFrameAnimation animation) {
            visual.StartAnimation("Offset.x", animation);
        }

        public static void StartOffsetAnimation(SpriteVisual visual, ScalarKeyFrameAnimation animation, float offset, double seconds) {
            animation.InsertKeyFrame(1f, offset);
            animation.Duration = TimeSpan.FromSeconds(seconds);

            visual.StartAnimation("Offset.x", animation);
        }
    }
}
