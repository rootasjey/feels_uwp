using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Feels.Services.WeatherScene {
    public class Lights {
        private static List<CompositionLight> _LightsList { get; set; }

        public static void InitializeVariables() {
            _LightsList = new List<CompositionLight>();
        }

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
        public static void AddPointLight(Grid scene, Grid element, Dictionary<string, object> options = null) {
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

            void TrackUIElement() {
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
            light.Direction = new Vector3(0, 0, 0) - new Vector3(0, 200f, 100f);
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
    }
}
