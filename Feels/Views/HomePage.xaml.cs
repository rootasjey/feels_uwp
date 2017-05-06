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
        private SourceModel PageDataSource { get; set; }

        CoreDispatcher _UIDispatcher { get; set; }

        private int _HourForcastAnimDelay { get; set; }

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

            BindHourlyListData();
            BindDailyListData();
        }

        async Task FetchCurrentLocation() {
            var geo = new Geolocator();
            var position = await geo.GetGeopositionAsync();
            var coord = position.Coordinate.Point.Position;
            await PageDataSource.FetchCurrentWeather(coord.Latitude, coord.Longitude);
            NowPivot.DataContext = PageDataSource.Forecast.Currently;
        }
        
        async void PopulateFirstPage() {
            var currentWeather = PageDataSource.Forecast.Currently;

            await AnimateSlideUP(WeatherViewContent);
            AnimateTemperature();
            FillInData();
            DrawScene();

            async void AnimateTemperature()
            {
                int temperature = (int)currentWeather.Temperature;

                var max = temperature;
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
                            Temperature.Text = string.Format("{0}°", curr);
                        });
                    });
                }
            }

            void FillInData()
            {
                Status.Text = currentWeather.Summary;
                City.Text = PageDataSource.Forecast.TimeZone;
                FeelsLike.Text = string.Format("{0} {1}", "Feels more like ", currentWeather.ApparentTemperature);
                PrecipValue.Text = string.Format("{0}%", currentWeather.PrecipitationProbability * 100);
                //SunriseBlock.Text = currentWeather.Sunrise;
                //SunsetBlock.Text = currentWeather.Sunset;

                if (PageDataSource.Forecast.Daily.Days == null ||
                    PageDataSource.Forecast.Daily.Days.Count == 0) return;

                var currentDay = PageDataSource.Forecast.Daily.Days[0];
                var maxTemp = (int)currentDay.MaxTemperature;
                var minTemp = (int)currentDay.MinTemperature;

                MaxTempValue.Text = maxTemp.ToString();
                MinTempValue.Text = minTemp.ToString();
            }
        }

        async void DrawScene() {
            var scene = Scene.CreateNew(PageDataSource.Forecast.Currently, PageDataSource.Forecast.Daily.Days[0]);
            Theater.Children.Add(scene);

            await scene.Fade(0, 0).Offset(0, 200, 0).StartAsync();

            Theater.Fade(1, 1000).Start();

            scene.Fade(1, 1000).Offset(0, 0, 1000).Start();
        }

        async void ShowLoadingView() {
            SplashView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Collapsed;
            LocationDisabledMessage.Visibility = Visibility.Collapsed;

            AnimateSlideUP(LoadingView);
        }

        void BindHourlyListData() {
            HourlyList.ItemsSource = PageDataSource.Forecast.Hourly.Hours;
            HourlySummary.Text = PageDataSource.Forecast.Hourly.Summary;
        }

        void BindDailyListData() {
            DailyList.ItemsSource = PageDataSource.Forecast.Daily.Days;
            DailySummary.Text = PageDataSource.Forecast.Daily.Summary;
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
                    child.Fade((float)opacities[index], 1000, delay)
                         .Offset(0, 0, 1000, delay)
                         .Start();
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
            SplashView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Collapsed;

            AnimateSlideUP(LocationDisabledMessage);
        }

        private async void LocationSettingsButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            bool result = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private void TryAgainButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            InitializePageData();
        }

        private void CurrentToHourlyButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            PagePivot.SelectedIndex = 1;
        }

        private void CurrentToDailyButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            PagePivot.SelectedIndex = 2;
        }

        private void HourlyToCurrentButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            PagePivot.SelectedIndex = 0;
        }

        private async void HourForecast_Loaded(object sender, RoutedEventArgs e) {
            var HourForecast = (Grid)sender;

            _HourForcastAnimDelay += 100;
            await HourForecast.Offset(0, 50,0)
                                .Then()
                                .Fade(1, 500, _HourForcastAnimDelay)
                                .Offset(0, 0, 500, _HourForcastAnimDelay)
                                .StartAsync();
        }
        
        private void HourlySummary_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            HourlyList.ScrollToIndex(0);
        }

        private void DailySummary_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            DailyList.ScrollToIndex(0);
        }

        private void AppBar_Closed(object sender, object e) {
            AppBar.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void AppBar_Opening(object sender, object e) {
            AppBar.Background = new SolidColorBrush(Colors.Black);
        }
    }
}
