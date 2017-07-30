using Feels.Data;
using Feels.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Feels.Views {
    public sealed partial class HomePage : Page {
        #region variables
        private SourceModel PageDataSource { get; set; }

        CoreDispatcher _UIDispatcher { get; set; }

        private int _AnimationDelayHourForcast { get; set; }

        private int _AnimationDelayDailyForecast { get; set; } 

        static DateTime _LastFetchedTime { get; set; }

        static BasicGeoposition _LastPosition { get; set; }

        public static bool _ForceDataRefresh { get; set; }

        // Composition
        private Compositor _compositor { get; set; }
        private Visual _EarthVisual { get; set; }
        private Visual _PinEarthVisual { get; set; }
        #endregion variables

        public HomePage() {
            InitializeComponent();
            InitializeTitleBar();
            InitializeVariables();
            InitialzeEvents();
            InitializePageData();

            ShowUpdateChangelog();
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

        #region data
        private void InitializeVariables() {
            PageDataSource = App.DataSource;
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        async void InitializePageData() {
            if (PageDataSource.Forecast != null 
                && _ForceDataRefresh == false) {

                HideSplashView();
                HideLoadingView();
                GetCurrentCity(_LastPosition);
                PopulateView();
            }

            if (!await MustRefreshData()) {
                return;
            }

            CleanTheater();
            FetchNewData();

            async void FetchNewData()
            {
                var granted = await GetLocationPermission();
                if (!granted) { ShowNoAccessView(); return; }

                ShowLoadingView();

                var position = await GetPosition();
                GetCurrentCity(position);

                await FetchCurrentLocation(position);

                HideLoadingView();

                PopulateView();
            }
        }

        void InitialzeEvents() {
            Application.Current.Resuming += App_Resuming;
        }

        private void App_Resuming(object sender, object e) {
            var task = _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                InitializePageData();
            });
        }

        void PopulateView() {
            PopulateFirstPage();
            BindHourlyListData();
            BindDailyListData();

            ShowBetaMessageAsync();
        }

        void SafeExit() {
            ShowEmptyView();
        }

        void ShowEmptyView() {
            ShowNoAccessView();
        }

        async Task<bool> MustRefreshData() {
            if (PageDataSource.Forecast == null) return true;

            if (_ForceDataRefresh) {
                _ForceDataRefresh = false;
                return true;
            }

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

            var weatherCurrent = PageDataSource.Forecast.Currently;
            var weatherToday = PageDataSource.Forecast.Daily.Days[0];

            await AnimateSlideIn(WeatherViewContent);
            AnimateTemperature();

            FillInData();
            DrawScene();

            UpdateMainTile();

            async void AnimateTemperature()
            {
                int temperature = (int)weatherCurrent.Temperature;

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
                Status.Text = weatherCurrent.Summary;
                FeelsLike.Text += string.Format(" {0}°{1}", weatherCurrent.ApparentTemperature, Settings.GetTemperatureUnit());
                PrecipProbaValue.Text = string.Format("{0}%", weatherToday.PrecipitationProbability * 100);
                HumidityValue.Text = string.Format("{0}%", weatherCurrent.Humidity * 100);
                SunriseTime.Text = weatherToday.SunriseTime.ToLocalTime().ToString("hh:mm");
                SunsetTime.Text = weatherToday.SunsetTime.ToLocalTime().ToString("hh:mm");

                WindSpeed.Text = string.Format("{0}{1}", weatherCurrent.WindSpeed, GetWindSpeedUnits());
                CloudCover.Text = string.Format("{0}%", weatherCurrent.CloudCover * 100);
                Pressure.Text = string.Format("{0}{1}", weatherCurrent.Pressure, GetPressureUnits());

                LastTimeUpdate.Text = DateTime.Now.ToLocalTime().ToString("dddd HH:mm");

                if (PageDataSource.Forecast.Daily.Days == null ||
                    PageDataSource.Forecast.Daily.Days.Count == 0) return;

                var currentDay = PageDataSource.Forecast.Daily.Days[0];
                var maxTemp = (int)currentDay.MaxTemperature;
                var minTemp = (int)currentDay.MinTemperature;

                MaxTempValue.Text = maxTemp.ToString();
                MinTempValue.Text = minTemp.ToString();
            }
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

        void ResetOpacity(Panel view) {
            view.Opacity = 0;
            var children = view.Children;

            foreach (var child in children) {
                child.Opacity = 0;
            }
        }

        void ShowNoAccessView() {
            AnimateSlideIn(LocationDisabledMessage);
        }
        #endregion data

        #region location
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

        async void GetCurrentCity(BasicGeoposition position) {
            Geopoint pointToReverseGeocode = new Geopoint(position);

            // Reverse geocode the specified geographic location.
            MapLocationFinderResult result =
                await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

            // If the query returns results, display the name of the town
            // contained in the address of the first result.
            if (result.Status == MapLocationFinderStatus.Success) {
                if (result.Locations.Count == 0) return;
                City.Text = result.Locations[0].Address.Town;
            }
        }

        async Task FetchCurrentLocation(BasicGeoposition basicPosition) {
            await PageDataSource.FetchCurrentWeather(basicPosition.Latitude, basicPosition.Longitude);

            if (PageDataSource.Forecast == null) {
                SafeExit();
                return;
            }

            NowPivot.DataContext = PageDataSource.Forecast.Currently;
        }

        async Task<BasicGeoposition> GetPosition() {
            var locator = new Geolocator();
            var positionToReturn = new BasicGeoposition();

            try {
                // NOTE: sometimes GetPositionAsync() is stuck in a loop
                // set a timeout and try a different way (e.g. cached location)
                var position = await locator.GetGeopositionAsync(
                    TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(10));

                if (position == null) {
                    positionToReturn = Settings.GetLastSavedPosition();
                } else {
                    positionToReturn = position.Coordinate.Point.Position;
                    Settings.SavePosition(positionToReturn);
                }

                _LastFetchedTime = DateTime.Now;
                _LastPosition = positionToReturn;

                return positionToReturn;

            } catch {
                return positionToReturn;
            }
        }

        #endregion location

        #region units
        string GetWindSpeedUnits() {
            var unit = Settings.GetUnit();

            switch (unit) {
                case DarkSkyApi.Unit.US:
                    return "miles/h";
                case DarkSkyApi.Unit.SI:
                    return "m/s";
                case DarkSkyApi.Unit.CA:
                    return "km/h";
                case DarkSkyApi.Unit.UK:
                    return "miles/h";
                case DarkSkyApi.Unit.UK2:
                    return "miles/h";
                case DarkSkyApi.Unit.Auto:
                    return "m/s";
                default:
                    return "miles/h";
            }
        }

        string GetPressureUnits() {
            var unit = Settings.GetUnit();

            switch (unit) {
                case DarkSkyApi.Unit.US:
                case DarkSkyApi.Unit.CA:
                case DarkSkyApi.Unit.UK:
                case DarkSkyApi.Unit.UK2:
                case DarkSkyApi.Unit.Auto:
                    return "millibars";
                case DarkSkyApi.Unit.SI:
                    return "hPa";
                default:
                    return "millibars";
            }
        }
        #endregion units

        #region loading view
        void ShowLoadingView() {
            SplashView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Collapsed;
            LocationDisabledMessage.Visibility = Visibility.Collapsed;

            StartEarthRotation();
            StartPinEarthOffsetAnimation();

            AnimateSlideIn(LoadingView);
        }

        void HideLoadingView() {
            LoadingView.Visibility = Visibility.Collapsed;
            ResetOpacity(LoadingView);

            StopEarthRotation();
            StopPinEarthAnimation();
        }

        void StartEarthRotation() {
            var visual = ElementCompositionPreview.GetElementVisual(EarthIcon);
            var compositor = visual.Compositor;

            var animationRotate = compositor.CreateScalarKeyFrameAnimation();
            animationRotate.InsertKeyFrame(0f, 0f);
            animationRotate.InsertKeyFrame(1f, 20f);
            animationRotate.Duration = TimeSpan.FromSeconds(10);
            animationRotate.IterationBehavior = AnimationIterationBehavior.Forever;
            animationRotate.Direction = AnimationDirection.Alternate;

            visual.RotationAxis = new Vector3(0, 0, 1);
            visual.CenterPoint = new Vector3((float)EarthIcon.Height / 2, (float)EarthIcon.Width / 2, 0);

            visual.StartAnimation("RotationAngle", animationRotate);

            _EarthVisual = visual;
            _compositor = compositor;
        }

        void StopEarthRotation() {
            _EarthVisual?.StopAnimation("RotationAngle");
        }

        void StartPinEarthOffsetAnimation() {
            var visual = ElementCompositionPreview.GetElementVisual(PinEarthIcon);
            ElementCompositionPreview.SetIsTranslationEnabled(PinEarthIcon, true);

            var animationOffset = _compositor.CreateScalarKeyFrameAnimation();
            animationOffset.InsertKeyFrame(0f, 0);
            animationOffset.InsertKeyFrame(1f, 20);
            animationOffset.Duration = TimeSpan.FromSeconds(3);
            animationOffset.Direction = AnimationDirection.Alternate;
            animationOffset.IterationBehavior = AnimationIterationBehavior.Forever;

            visual.StartAnimation("Translation.y", animationOffset);

            _PinEarthVisual = visual;
        }

        void StopPinEarthAnimation() {
            _PinEarthVisual?.StopAnimation("Translation.y");
        }

        #endregion loading view


        #region scene theater
        async void DrawScene() {
            var scene = Scene.CreateNew(
                PageDataSource.Forecast.Currently, 
                PageDataSource.Forecast.Daily.Days[0]);

            Theater.Children.Add(scene);

            await scene.Fade(0, 0).Offset(0, 200, 0).StartAsync();
            Theater.Fade(1, 1000).Start();
            scene.Fade(1, 1000).Offset(0, 0, 1000).Start();
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

        void CleanTheater() {
            Theater.Children.Clear();
            Theater.Fade(0).Start();
        }
        #endregion scene theater

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

        #region events
        private async void HourForecast_Loaded(object sender, RoutedEventArgs e) {
            var panel = (Grid)sender;

            _AnimationDelayHourForcast += 100;

            await panel.Offset(0, 50,0)
                        .Then()
                        .Fade(1, 500, _AnimationDelayHourForcast)
                        .Offset(0, 0, 500, _AnimationDelayHourForcast)
                        .StartAsync();
        }

        private async void DailyForecast_Loaded(object sender, RoutedEventArgs e) {
            var panel = (Grid)sender;

            _AnimationDelayDailyForecast += 100;

            await panel.Offset(0, 50, 0)
                        .Then()
                        .Fade(1, 500, _AnimationDelayDailyForecast)
                        .Offset(0, 0, 500, _AnimationDelayDailyForecast)
                        .StartAsync();
        }
        #endregion events

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
            _ForceDataRefresh = true;
            CleanTheater();
            InitializePageData();
        }
        #endregion appbar

        #region others
        void UpdateMainTile() {
            TileDesigner.UpdatePrimary(_LastPosition);
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
        #endregion others

        #region update changelog
        /// <summary>
        /// Show update change log if the app has been updated
        /// </summary>
        private void ShowUpdateChangelog() {
            if (Settings.IsNewUpdatedLaunch()) {
                UpdateVersion.Text += string.Format(" {0}", Settings.GetAppVersion());
                ShowLastUpdateChangelog();
            }
        }

        private async void ShowLastUpdateChangelog() {
            PagePivot.IsEnabled = false;
            await UpdateChangeLogFlyout.Scale(.9f, .9f, 0, 0, 0).Fade(0).StartAsync();
            UpdateChangeLogFlyout.Visibility = Visibility.Visible;

            var x = (float)UpdateChangeLogFlyout.ActualWidth / 2;
            var y = (float)UpdateChangeLogFlyout.ActualHeight / 2;

            await UpdateChangeLogFlyout.Scale(1f, 1f, x, y).Fade(1).StartAsync();
            PagePivot.Blur(10, 500, 500).Start();
        }

        private async void ChangelogDismissButton_Tapped(object sender, TappedRoutedEventArgs e) {
            var x = (float)UpdateChangeLogFlyout.ActualWidth / 2;
            var y = (float)UpdateChangeLogFlyout.ActualHeight / 2;

            await UpdateChangeLogFlyout.Scale(.9f, .9f, x, y).Fade(0).StartAsync();
            UpdateChangeLogFlyout.Visibility = Visibility.Collapsed;
            PagePivot.Blur(0).Start();
            PagePivot.IsEnabled = true;
        }

        #endregion update changelog
    }
}
