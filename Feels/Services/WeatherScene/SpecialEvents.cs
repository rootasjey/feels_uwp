using DarkSkyApi.Models;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Feels.Services.WeatherScene {
    public class SpecialEvents {
        private static Compositor s_compositor { get; set; }

        public static Grid AddHalloween(Grid scene, Forecast forecast) {
            if (scene == null) return scene;

            s_compositor = ElementCompositionPreview.GetElementVisual(scene).Compositor;

            scene = TransformMoon(scene, forecast.Daily.Days[0]);
            return scene;
        }

        private static Grid TransformMoon(Grid scene, DayDataPoint dayDataPoint) {
            var moonContainer = (Grid)scene.FindName("MoonContainer");
            if (moonContainer == null) return scene;

            moonContainer = Icons.CreateMoon(dayDataPoint.MoonPhase, s_compositor, Color.FromArgb(255, 192, 57, 43));

            return scene;
        }
    }
}
