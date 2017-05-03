using Feels.Data;
using Feels.Models;
using Feels.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Feels.Views {
    public sealed partial class HomePage : Page {
        private SourceModel PageDataSource;

        CoreDispatcher _UIDispatcher { get; set; }

        public HomePage() {
            InitializeComponent();
            PageDataSource = App.DataSource;

            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            InitializePageData();
        }

        async Task<bool> GetLocationPermission() {
            var accessStatus = await Geolocator.RequestAccessAsync();

            switch (accessStatus) {
                case GeolocationAccessStatus.Unspecified:
                    return false;
                case GeolocationAccessStatus.Allowed:
                    return true;
                case GeolocationAccessStatus.Denied:
                    return false;
                default:
                    return false;
            }
        }

        async void InitializePageData() {
            var granted = await GetLocationPermission();
            if (!granted) { ShowNoAccessView(); return; }

            ShowLoadingView();
            await FetchCurrentLocation();
            HideLoadingView();
            PopulateFirstPage();
        }

        async Task FetchCurrentLocation() {
            var geo = new Geolocator();
            var position = await geo.GetGeopositionAsync();
            var coord = position.Coordinate.Point.Position;
            await PageDataSource.FetchCurrentWether(coord.Latitude, coord.Longitude);
            MainCityPivot.DataContext = PageDataSource.Cities[0];
        }
        
        async void PopulateFirstPage() {
            var mainCity = PageDataSource.Cities[0];
            var currentWeather = mainCity.Current;

            await AnimateSlideUP(WeatherViewContent);
            AnimateTemperature();
            FillInData();
            DrawScene(mainCity);

            async void AnimateTemperature()
            {
                var temperature = currentWeather.Temperature;
                var index = temperature.IndexOf(".");
                
                if (index != -1) temperature = currentWeather.Temperature.Substring(0, index);

                var max = int.Parse(temperature);
                var curr = 0;
                var step = max > 0 ? 1 : -1;
                var div = Math.Abs(max - curr);
                var delay = 1000 / div;

                while (max != curr) {
                    await Task.Delay(delay).ContinueWith(async _ => {
                        curr += step;
                        div = Math.Abs(max - curr) == 0 ? 1 : Math.Abs(max - curr);
                        delay = 1000 / div;

                        await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                            Temperature.Text = curr.ToString();
                        });
                    });
                }
            }

            void FillInData()
            {
                Status.Text = currentWeather.Description;
                City.Text = mainCity.Location.City;
                SunriseBlock.Text = currentWeather.Sunrise;
                SunsetBlock.Text = currentWeather.Sunset;
            }
        }

        void DrawScene(Weather weather) {
            var scene = Scene.CreateNew(weather);
            Theater.Children.Add(scene);

            Theater.Fade(1, 1000).Start();
        }

        async void ShowLoadingView() {
            WeatherView.Visibility = Visibility.Collapsed;
            LocationDisabledMessage.Visibility = Visibility.Collapsed;

            AnimateSlideUP(LoadingView);
        }

        async Task AnimateSlideUP(Panel view) {
            view.Opacity = 0;
            view.Visibility = Visibility.Visible;

            List<double> opacities = new List<double>();

            var children = view.Children;
            foreach (var child in children) {
                opacities.Add(child.Opacity);
                child.Opacity = 0;
                await child.Offset(0, 20, 0).StartAsync();
            }

            view.Opacity = 1;

            AnimateView();

            void AnimateView()
            {
                int index = 0;
                var delay = 0;
                foreach (var child in children) {
                    delay += 200;
                    child.Fade((float)opacities[index], 1000, delay).Offset(0, 0, 1000, delay).Start();
                    index++;
                }
            }
        }

        void HideLoadingView() {
            LoadingView.Visibility = Visibility.Collapsed;
            LocationDisabledMessage.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Visible;
        }

        async void ShowNoAccessView() {
            WeatherView.Visibility = Visibility.Collapsed;

            AnimateSlideUP(LocationDisabledMessage);
        }

        private async void LocationSettingsButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            bool result = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private void TryAgainButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            InitializePageData();
        }
    }
}
