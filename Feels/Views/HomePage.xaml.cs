﻿using Feels.Data;
using Feels.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Feels.Views {
    public sealed partial class HomePage : Page {
        private SourceModel PageDataSource { get; set; }

        CoreDispatcher _UIDispatcher { get; set; }

        private int _HourForcastAnimDelay { get; set; }

        static DateTime _LastFetchedTime { get; set; }

        static BasicGeoposition _LastPosition { get; set; }

        public HomePage() {
            InitializeComponent();
            InitializeTitleBar();

            PageDataSource = App.DataSource;
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            InitializePageData();            
        }

        #region titlebar
        async void InitializeTitleBar() {
            App.DeviceType = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
            if (App.DeviceType == "Windows.Mobile") {
                var statusBar = StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
                return;
            }

            Window.Current.Activated += Current_Activated;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            TitleBar.Height = coreTitleBar.Height;
            Window.Current.SetTitleBar(MainTitleBar);
            
            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
        }

        void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar titleBar, object args) {
            TitleBar.Visibility = titleBar.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) {
            TitleBar.Height = sender.Height;
            RightMask.Width = sender.SystemOverlayRightInset;
        }

        private void Current_Activated(object sender, WindowActivatedEventArgs e) {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated) {
                //BackButtonGrid.Visibility = Visibility.Visible;
                MainTitleBar.Opacity = 1;
            } else {
                //BackButtonGrid.Visibility = Visibility.Collapsed;
                MainTitleBar.Opacity = 0.5;
            }
        }

        #endregion titlebar

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
            if (!await MustRefreshData()) {
                HideSplashView();
                HideLoadingView();
                PopulateView();
                return;
            }

            var granted = await GetLocationPermission();
            if (!granted) { ShowNoAccessView(); return; }

            ShowLoadingView();
            await FetchCurrentLocation();
            HideLoadingView();

            PopulateView();            
        }

        void PopulateView() {
            PopulateFirstPage();
            BindHourlyListData();
            BindDailyListData();

            ShowBetaMessageAsync();
        }

        async Task ShowBetaMessageAsync() {
            if (!Settings.IsFirstLaunch()) return;

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            var message = resourceLoader.GetString("BetaMessage");

            //DataTransfer.ShowLocalToast(message);
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
            Settings.SaveFirstLaunchPassed();
        }

        async Task FetchCurrentLocation() {
            var position = await GetPosition();
            if (position == null) { SafeExit(); return; }

            var coord = position.Coordinate.Point.Position;
            await PageDataSource.FetchCurrentWeather(coord.Latitude, coord.Longitude);

            if (PageDataSource.Forecast == null) {
                SafeExit();
                return;
            }

            NowPivot.DataContext = PageDataSource.Forecast.Currently;

            _LastFetchedTime = DateTime.Now;
            _LastPosition = coord;
        }

        async Task<Geoposition> GetPosition() {
            var geo = new Geolocator();

            try {
                var position = await geo.GetGeopositionAsync();
                return position;
            } catch {
                return null;
            }
        }

        void SafeExit() {
            ShowEmptyView();
        }

        void ShowEmptyView() {
            //SplashView.Visibility = Visibility.Visible;
            ShowNoAccessView();
        }

        async Task<bool> MustRefreshData() {
            if (PageDataSource.Forecast == null) return true;

            if (DateTime.Now.Hour - _LastFetchedTime.Hour > 0) {
                return true;
            }

            var geo = new Geolocator();
            var position = await geo.GetGeopositionAsync();
            var coord = position.Coordinate.Point.Position;

            var currLat = (int)coord.Latitude;
            var currLon = (int)coord.Longitude;

            var prevLat = (int)_LastPosition.Latitude;
            var prevLon = (int)_LastPosition.Longitude;

            if (currLat != prevLat || currLon != prevLon) {
                return true;
            }

            return false;
        }
        
        async void PopulateFirstPage() {
            if (PageDataSource.Forecast == null) return;

            WeatherView.Visibility = Visibility.Visible;

            var currentWeather = PageDataSource.Forecast.Currently;

            await AnimateSlideIn(WeatherViewContent);
            AnimateTemperature();
            FillInData();
            DrawScene();

            UpdateMainTile();

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
                SetCity();
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

            void SetCity()
            {
                var timeZone = PageDataSource.Forecast.TimeZone;
                var index = timeZone.IndexOf("/");
                City.Text = timeZone.Substring(index + 1);
            }
        }

        async void DrawScene() {
            var scene = Scene.CreateNew(PageDataSource.Forecast.Currently, PageDataSource.Forecast.Daily.Days[0]);
            Theater.Children.Add(scene);

            await scene.Fade(0, 0).Offset(0, 200, 0).StartAsync();
            Theater.Fade(1, 1000).Start();
            scene.Fade(1, 1000).Offset(0, 0, 1000).Start();
        }
        
        void ShowLoadingView() {
            SplashView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Collapsed;
            LocationDisabledMessage.Visibility = Visibility.Collapsed;

            AnimateSlideIn(LoadingView);
        }

        void HideLoadingView() {
            LoadingView.Visibility = Visibility.Collapsed;
            ResetOpacity(LoadingView);
        }

        void HideSplashView() {
            SplashView.Visibility = Visibility.Collapsed;
        }

        void BindHourlyListData() {
            if (PageDataSource.Forecast == null) return;
            HourlyList.ItemsSource = PageDataSource.Forecast.Hourly.Hours;
            HourlySummary.Text = PageDataSource.Forecast.Hourly.Summary;
        }

        void BindDailyListData() {
            if (PageDataSource.Forecast == null) return;
            DailyList.ItemsSource = PageDataSource.Forecast.Daily.Days;
            DailySummary.Text = PageDataSource.Forecast.Daily.Summary;
        }

        async Task AnimateSlideIn(Panel view) {
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

        void ResetOpacity(Panel view) {
            view.Opacity = 0;
            var children = view.Children;

            foreach (var child in children) {
                child.Opacity = 0;
            }
        }

        async void ShowNoAccessView() {
            //SplashView.Visibility = Visibility.Collapsed;
            //WeatherView.Visibility = Visibility.Collapsed;

            AnimateSlideIn(LocationDisabledMessage);
        }

        #region buttons
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
        private void HourlySummary_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            HourlyList.ScrollToIndex(0);
        }

        private void DailySummary_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            DailyList.ScrollToIndex(0);
        }
        #endregion buttons

        private async void HourForecast_Loaded(object sender, RoutedEventArgs e) {
            var HourForecast = (Grid)sender;

            _HourForcastAnimDelay += 100;
            await HourForecast.Offset(0, 50,0)
                                .Then()
                                .Fade(1, 500, _HourForcastAnimDelay)
                                .Offset(0, 0, 500, _HourForcastAnimDelay)
                                .StartAsync();
        }
        
        #region appbar
        private void AppBar_Closed(object sender, object e) {
            AppBar.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void AppBar_Opening(object sender, object e) {
            AppBar.Background = new SolidColorBrush(Colors.Black);
        }

        private void GoToSettings_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(SettingsPage_Mobile));
        }

        private void CmdRefresh_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Theater.Children.Clear();
            InitializePageData();
        }
        #endregion appbar

        void UpdateMainTile() {
            TileDesigner.UpdatePrimary();
        }
    }
}
