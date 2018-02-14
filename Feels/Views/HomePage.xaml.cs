using DarkSkyApi.Models;
using Feels.Data;
using Feels.Models;
using Feels.Services;
using Feels.Services.WeatherScene;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Threading;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Feels.Views {
    public sealed partial class HomePage : Page {
        #region variables

        private SourceModel _pageDataSource { get; set; }

        CoreDispatcher _UIDispatcher { get; set; }

        private int _animationDelayHourForcast { get; set; }

        private int _animationDelayDailyForecast { get; set; }

        private int _delayDetailItem { get; set; }

        static DateTime _lastFetchedTime { get; set; }

        static BasicGeoposition _lastPosition { get; set; }

        public static bool _forceDataRefresh { get; set; }

        private string _currentTown { get; set; }

        /// <summary>
        /// True if the user has the suitable addon to see special scenes.
        /// </summary>
        private bool _isSpecialSceneChecked { get; set; }

        // -----------
        // Composition
        // -----------

        private Compositor _compositor { get; set; }

        private Visual _earthVisual { get; set; }

        private Visual _pinEarthVisual { get; set; }

        private Visual _lineVisual { get; set; }

        private AnimationSet _leftArrowAnimationOut { get; set; }

        private AnimationSet _rightArrowAnimationOut { get; set; }

        private List<Tuple<Visual, string>> _animatedVisuals { get; set; }

        #endregion variables

        public HomePage() {
            InitializeComponent();
            InitializeTitleBar();
            InitializeVariables();
            InitialzeEvents();

            ApplyCommandBarBarFrostedGlass();
            ShowUpdateChangelogIfUpdated();

            BackgroundTasks.CheckAllTasks();
            InAppPurchases.CheckAndUpdatePremiumUser();
        }

        #region titlebar
        private void InitializeTitleBar() {
            App.DeviceType = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;

            if (App.DeviceType == "Windows.Mobile") {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.HideAsync();
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

        #region navigation

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (e.Parameter != null && (string)e.Parameter != "App" &&
                !string.IsNullOrEmpty((string)e.Parameter)) {

                var locationId = (string)e.Parameter;
                FetchNewDataFromLocation(locationId);
                return;
            }

            InitializePageData();
        }

        private void App_Resuming(object sender, object e) {
            var task = _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                InitializePageData();
            });
        }

        private void GoToAchievements_Tapped(object sender, TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(AchievementsPage));
        }

        #endregion navigation

        #region initialization

        private void InitialzeEvents() {
            Application.Current.Resuming += App_Resuming;
        }

        private void InitializeVariables() {
            _pageDataSource = App.DataSource;
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            _animatedVisuals = new List<Tuple<Visual, string>>();

            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        }

        /// <summary>
        /// 1.Load recent data fetched.
        /// 2.If there's no connectivity:
        ///   2.1.a.Do nothing if there's already data available
        ///   2.1.b.Load cached data if there's no data available yet
        ///   2.2.return
        /// 3.Check if data must be refreshed (based on time & location)
        /// 4.Refresh data if the previous condition is true
        /// </summary>
        private async void InitializePageData() {
            LoadLastFetchedData();
            
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                if (_pageDataSource.Forecast != null) return;

                LoadCachedData();
                return;
            }

            if (await MustRefreshData()) {
                CleanTheater();
                FetchNewData();
            }
        }

        #endregion initialization

        #region data

        /// <summary>
        /// Load old data before checking if refresh must be made
        /// so the app seems fast.
        /// </summary>
        private void LoadLastFetchedData() {
            if (_pageDataSource.Forecast == null || _forceDataRefresh) {
                return;
            }

            HideSplashView();
            HideLoadingView();
            PopulateView();
            PopulateCurrentLocationAndTime();
        }

        /// <summary>
        /// This test may take some seconds (~10sec) 
        /// because it has to retrieve GPS location, then fetch data.
        /// </summary>
        /// <returns>True if data must be refreshed.</returns>
        private async Task<bool> MustRefreshData() {
            if (_pageDataSource.Forecast == null) return true;

            if (_forceDataRefresh) {
                _forceDataRefresh = false;
                return true;
            }

            if (DateTime.Now.Hour - _lastFetchedTime.Hour > 0) {
                return true;
            }

            if (await Settings.GetFavoriteLocation() != null) {
                return false;
            }

            var geo = new Geolocator();
            var position = await geo.GetGeopositionAsync();
            var coord = position.Coordinate.Point.Position;

            var currLat = (int)coord.Latitude;
            var currLon = (int)coord.Longitude;

            var prevLat = (int)_lastPosition.Latitude;
            var prevLon = (int)_lastPosition.Longitude;

            if (currLat != prevLat || currLon != prevLon) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find the LocationItem from storage and get new data for the specific location.
        /// If the item isn't found, abort and get new data for the current lcoation.
        /// (Handle no connectivity)
        /// </summary>
        /// <param name="locationId"></param>
        private async void FetchNewDataFromLocation(string locationId) {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                LoadCachedData();
                return;
            }

            var locationsList = await Settings.GetSavedLocationAsync();
            var locationName = locationId.Replace(".", ", ");
            LocationItem locationToFind = null;

            foreach (var location in locationsList) {
                if (location.Name == locationName) {
                    locationToFind = location;
                    break;
                }
            }

            if (locationToFind == null) {
                InitializePageData();
                return;
            }

            ShowLoadingView();
            await FetchCurrentForecast(locationToFind);

            HideLoadingView();
            PopulateView();
            PopulateCurrentLocationAndTime();

            TownTextBlock.Text = locationToFind.Town;

            TileDesigner.UpdatePrimary(_lastPosition);
            Settings.CacheForecastData(_pageDataSource.Forecast);
            Settings.CacheLocationAndTime(
                _currentTown, 
                DateTime.Now.ToLocalTime().ToString("dddd HH:mm"));
        }

        /// <summary>
        /// Get & show cached data.
        /// Tell the user the data may be outdated.
        /// </summary>
        private async void LoadCachedData() {
            HideSplashView();
            ShowLoadingView();

            var cachedForecast = await Settings.GetCachedForecastData();

            if (cachedForecast == null) {
                NoConnectivityView.AnimateSlideIn();
                return;
            }

            _pageDataSource.Forecast = cachedForecast;

            HideLoadingView();
            PopulatCachedLocationAndTime();
            PopulateView();

            OutdatedDataIcon.Visibility = Visibility.Visible;
            TimePanel.Margin = new Thickness(0, 12, 0, 0);
            LastTimeUpdate.Foreground = new SolidColorBrush(Colors.Salmon);
        }

        private async void FetchNewData() {
            var location = await Settings.GetFavoriteLocation();

            if (location == null || string.IsNullOrEmpty(location.Id)) {
                var granted = await GetLocationPermission();

                if (!granted) {
                    ShowNoAccessView();
                    return;
                }

                ShowLoadingView();

                var position = await GetPosition();

                SetCurrentCity();

                location = new LocationItem() {
                    Latitude = position.Latitude,
                    Longitude = position.Longitude
                };

            } else {
                ShowLoadingView();
                SetCurrentCity();
            }

            await FetchCurrentForecast(location);

            ShowLocalizationSuccess();

            TileDesigner.UpdatePrimary(_lastPosition);
            Settings.CacheForecastData(_pageDataSource.Forecast);
            Settings.CacheLocationAndTime(
                _currentTown,
                DateTime.Now.ToLocalTime().ToString("dddd HH:mm"));

            Timer deffered = null;

            deffered = new Timer(async (object state) => {
                await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    if (LoadingView.Visibility == Visibility.Collapsed) {
                        deffered?.Dispose();
                        return;
                    }

                    HideLoadingView();
                    PopulateView();
                    PopulateCurrentLocationAndTime();
                });
            }, new AutoResetEvent(true), 3000, 2000);
        }

        private void PopulateView() {
            PopulateFirstPage();
            BindHourlyListData();
            BindDailyListData();
        }

        private void SafeExit() {
            ShowEmptyView();
        }

        private void ShowEmptyView() {
            ShowNoAccessView();
        }

        private void PopulateFirstPage() {
            if (_pageDataSource.Forecast == null) return;

            WeatherView.Visibility = Visibility.Visible;

            var currentWeather = _pageDataSource.Forecast.Currently;
            var weatherToday = _pageDataSource.Forecast.Daily.Days[0];

            WeatherViewContent.AnimateSlideIn();

            UI.AnimateNumericValue((int)currentWeather.Temperature, Temperature, _UIDispatcher, "°");

            PopulateForecastView(weatherToday, currentWeather);

            AnimateDetailsItems();
            SetMoonPhase(weatherToday);
            AnimateWindDirectionIcons(currentWeather);

            DrawScene();
        }

        private void PopulateForecastView(DayDataPoint todayWeather, CurrentDataPoint currentWeather) {
            Status.Text = currentWeather.Summary;

            //FeelsLike.Text = string.Format("{0} {1}°{2}", App.ResourceLoader.GetString("FeelsLike"), 
            //    currentWeather.ApparentTemperature, Settings.GetTemperatureUnit());

            PrecipProbaValue.Text   = $"{todayWeather.PrecipitationProbability * 100}%";
            HumidityValue.Text      = $"{currentWeather.Humidity * 100}%";
            UVIndexValue.Text       = $"{currentWeather.UVIndex}";
            
            VisibilityValue.Text    = $"{currentWeather.Visibility}";

            SunriseTime.Text        = todayWeather.SunriseTime.ToLocalTime().ToString("HH:mm");
            SunsetTime.Text         = todayWeather.SunsetTime.ToLocalTime().ToString("HH:mm");
            MoonriseTime.Text       = todayWeather.SunsetTime.ToLocalTime().ToString("HH:mm");
            MoonsetTime.Text        = todayWeather.SunriseTime.ToLocalTime().ToString("HH:mm");

            WindSpeed.Text          = $"{currentWeather.WindSpeed}{GetWindSpeedUnits()}";

            WindDirection.Text      = $"{currentWeather.WindBearing}°";

            CloudCover.Text         = $"{currentWeather.CloudCover * 100}%";
            
            SetPressureValue(currentWeather, true);

            if (_pageDataSource.Forecast.Daily.Days == null ||
                _pageDataSource.Forecast.Daily.Days.Count == 0) { return; }

            var currentDay = _pageDataSource.Forecast.Daily.Days[0];

            MaxTempValue.Text = $"{(int)currentDay.MaxTemperature}°";
            MinTempValue.Text = $"{(int)currentDay.MinTemperature}°";

            // TODO: Make a pull request to avoid UNIX time conversion (https://github.com/jcheng31/DarkSkyApi)
            DateTimeOffset baseUnixTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan());
            var converted = baseUnixTime.AddSeconds(currentDay.UVIndexTime);
            UVIndexTimeValue.Text = $"{converted.ToLocalTime().ToString("HH:mm")}";
        }

        private void PopulateCurrentLocationAndTime() {
            LastTimeUpdate.Text = DateTime.Now.ToLocalTime().ToString("dddd HH:mm");
            SetCurrentCity();
        }

        private void PopulatCachedLocationAndTime() {
            var cachedData = Settings.GetCachedLocationAndTown();
            if (cachedData == null) return;

            _currentTown = cachedData.Item1;
            LastTimeUpdate.Text = cachedData.Item2;
            TownTextBlock.Text = _currentTown;
        }

        private void AnimateWindDirectionIcons(CurrentDataPoint currentWeather) {
            WindDirectionIcon.Rotate(currentWeather.WindBearing + 180, 15, 15).Start();

            var baseAngle = 0;
            var visual = ElementCompositionPreview.GetElementVisual(WindDirectionIcon);
            var animationRotate = UI.CreateScalarAnimation(_compositor, baseAngle, baseAngle + 20);

            visual.RotationAxis = new Vector3(0, 0, 1);
            visual.CenterPoint = new Vector3(15, 15, 0);
            visual.StartAnimation("RotationAngleInDegrees", animationRotate);
        }

        private void SetPressureValue(CurrentDataPoint currentWeather, bool skipAnimation = false) {
            var unit = Settings.GetPressureUnit();
            var pressure = currentWeather.Pressure;

            if (unit == null) { unit = GetPressureUnits(); }
            else { pressure *= 0.75006168f; }

            if (skipAnimation) {
                Pressure.Text = string.Format("{0}{1}", pressure, unit);
                return;
            }

            UI.AnimateNumericValue(pressure, Pressure, _UIDispatcher, unit, 100);
        }

        private void SetMoonPhase(DayDataPoint weatherToday) {
            var moonPhase = weatherToday.MoonPhase;
            
            if (moonPhase == 0) {
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseNewMoon");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_new.png");

            } else if (moonPhase > 0 && moonPhase < .25) {
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseWaxingCrescent");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_waxing_crescent.png");

            } else if (moonPhase == .25) {
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseFirstQuarter");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_first_quarter.png");

            } else if (moonPhase > .25 && moonPhase < .5) {
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseWaxingGibbous");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_waxing_gibbous.png");

            } else if (moonPhase == .5) {
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseFullMoon");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_full.png");

            } else if (moonPhase > .5 && moonPhase < .75) {
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseWaningGibbous");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_waning_gibbous.png");

            } else if (moonPhase == .75) {
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseThirdQuarter");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_third_quarter.png");

            } else { // moonPhase > .75
                MoonPhaseValue.Text = App.ResourceLoader.GetString("MoonPhaseWaningCrescent");
                MoonPhaseIcon.UriSource = new Uri("ms-appx:///Assets/Icons/moon_waning_crescent.png");
            }
        }

        private void HideSplashView() {
            SplashView.Visibility = Visibility.Collapsed;
        }

        private void BindHourlyListData() {
            if (_pageDataSource.Forecast == null) return;
            HourlyList.ItemsSource = _pageDataSource.Forecast.Hourly.Hours;
            HourlySummary.Text = _pageDataSource.Forecast.Hourly.Summary;
        }

        private void BindDailyListData() {
            if (_pageDataSource.Forecast == null) return;
            DailyList.ItemsSource = _pageDataSource.Forecast.Daily.Days;
            DailySummary.Text = _pageDataSource.Forecast.Daily.Summary;
        }

        private void ResetOpacity(Panel view) {
            view.Opacity = 0;
            var children = view.Children;

            foreach (var child in children) {
                child.Opacity = 0;
            }
        }

        #endregion data

        #region location

        private async Task<bool> GetLocationPermission() {
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

        private async void SetCurrentCity() {
            if (string.IsNullOrEmpty(_currentTown)) {
                _currentTown = await GetCurrentCity();
            }

            TownTextBlock.Text = _currentTown;
        }

        private async Task<string> GetCurrentCity() {
            var location = await Settings.GetFavoriteLocation();

            if (UseGPSLocation(location)) {
                Geopoint pointToReverseGeocode = new Geopoint(_lastPosition);

                // Reverse geocode the specified geographic location.
                MapLocationFinderResult result =
                    await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

                // If the query returns results, display the name of the town
                // contained in the address of the first result.
                if (result.Status != MapLocationFinderStatus.Success || result.Locations.Count == 0) {
                    return null;
                }

                return result.Locations[0].Address.Town;
            }

            return location.Town;
            //TownTextBlock.Text = location.Town;
        }

        private bool UseGPSLocation(LocationItem location) {
            if (location == null) return true;
            return string.IsNullOrEmpty(location.Id);
        }

        private async Task FetchCurrentForecast(LocationItem location) {
            await _pageDataSource.FetchCurrentForecast(location.Latitude, location.Longitude);

            _lastFetchedTime = DateTime.Now;

            if (_pageDataSource.Forecast == null) {
                SafeExit();
                return;
            }

            NowPivot.DataContext = _pageDataSource.Forecast.Currently;
        }

        private async Task<BasicGeoposition> GetPosition() {
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

                //_LastFetchedTime = DateTime.Now;
                _lastPosition = positionToReturn;

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

        #region views

        private void ShowLoadingView() {
            SplashView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Collapsed;

            HideNoAccessView();

            StartEarthRotation();
            StartSpaceModuleAnimation();

            UI.AnimateSlideIn(LoadingView);
        }

        private void HideLoadingView() {
            LoadingView.Visibility = Visibility.Collapsed;
            ResetOpacity(LoadingView);

            StopEarthRotation();
            StopAllAnimations();
        }
        
        private void StartEarthRotation() {
            var visual = ElementCompositionPreview.GetElementVisual(EarthIcon);

            var animationRotate = _compositor.CreateScalarKeyFrameAnimation();
            animationRotate.InsertKeyFrame(0f, 0f);
            animationRotate.InsertKeyFrame(1f, 20f);
            animationRotate.Duration = TimeSpan.FromSeconds(10);
            animationRotate.IterationBehavior = AnimationIterationBehavior.Forever;
            animationRotate.Direction = Windows.UI.Composition.AnimationDirection.Alternate;

            visual.RotationAxis = new Vector3(0, 0, 1);
            visual.CenterPoint = new Vector3((float)EarthIcon.Height / 2, (float)EarthIcon.Width / 2, 0);

            visual.StartAnimation("RotationAngle", animationRotate);

            _earthVisual = visual;
        }

        private void StopEarthRotation() {
            _earthVisual?.StopAnimation("RotationAngle");
        }

        private void StartSpaceModuleAnimation() {
            var offsetAnimation = UI.CreateScalarAnimation(_compositor, 0, 20);
            var opacityAnimation = UI.CreateScalarAnimation(_compositor, 1, 0);

            UI.StartAnimatingElement(SpaceModulePanel, "Translation.y", offsetAnimation, -1, _animatedVisuals);
            UI.StartAnimatingElement(SpaceModuleLine, "Opacity", opacityAnimation, -1, _animatedVisuals);

            UI.StartAnimatingElement(LoadingViewStars1, "Opacity", opacityAnimation, 7, _animatedVisuals);
            UI.StartAnimatingElement(LoadingViewStars2, "Opacity", opacityAnimation, 6, _animatedVisuals);
            UI.StartAnimatingElement(LoadingViewStars3, "Opacity", opacityAnimation, 5, _animatedVisuals);
            UI.StartAnimatingElement(LoadingViewStars4, "Opacity", opacityAnimation, -1, _animatedVisuals);
        }

        private void StopAllAnimations() {
            foreach (var tuple in _animatedVisuals) {
                tuple.Item1?.StopAnimation(tuple.Item2);
            }
        }

        private async void ShowLocalizationSuccess() {
            LocalizationSearchingMessage.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(_currentTown)) {
                _currentTown = await GetCurrentCity();
            }

            SpaceModuleLine.Stroke = new SolidColorBrush(Colors.LimeGreen);

            SuccessMessageLocationName.Text = $"{App.ResourceLoader.GetString("SuccessMessageLocationName")}: {_currentTown}";
            await LocalizationSuccessMessage.AnimateSlideIn();
        }

        private async void LocationSettingsButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            bool result = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private void TryAgainButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            HideNoAccessView();

            _forceDataRefresh = true;
            CleanTheater();
            InitializePageData();
        }

        private void ShowNoAccessView() {
            Theater.Visibility = Visibility.Collapsed;
            SplashView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Collapsed;

            LocationDisabledMessage.Visibility = Visibility.Visible;
            LocationDisabledMessageContent.AnimateSlideIn();

            var offsetAnimation = UI.CreateScalarAnimation(_compositor, 0, 20);
            UI.StartAnimatingElement(AstronauteIcon, "Translation.y", offsetAnimation, 5, _animatedVisuals);
        }

        private void HideNoAccessView() {
            LocationDisabledMessage.Visibility = Visibility.Collapsed;
            LocationDisabledMessageContent.Opacity = 0;

            StopAllAnimations();
        }

        #endregion views

        #region scene theater

        private void DrawScene() {
            var isPremium = Settings.IsPremiumUser();
            var isColorAnimationOff = Settings.IsSceneColorAnimationDeactivated();

            var scene = Scenes.CreateNew(
                _pageDataSource.Forecast,
                isPremium,
                isColorAnimationOff);

            Theater.Children.Add(scene);

            scene.Opacity = 0;

            Theater.Fade(1, 1000, 500).Start();

            scene.Offset(0, 200, 0)
                .Then()
                .Fade(1, 1000)
                .Offset(0, 0, 1000)
                .SetDelay(500)
                .Start();

            

            //Timer deffered = null;

            //deffered = new Timer(async (object state) => {
            //    await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
            //        if (_isSpecialSceneChecked) { deffered?.Dispose(); }

            //        _isSpecialSceneChecked = true;
            //        AddSpecialScene();
            //    });
            //}, new AutoResetEvent(true), 3000, 2000);
        }

        private void CleanTheater() {
            Theater.Children.Clear();
            Theater.Fade(0).Start();
        }
        
        #endregion scene theater

        #region buttons

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

            _animationDelayHourForcast += 100;

            await panel.Offset(0, 50,0)
                        .Then()
                        .Fade(1, 500, _animationDelayHourForcast)
                        .Offset(0, 0, 500, _animationDelayHourForcast)
                        .StartAsync();
        }

        private async void DailyForecast_Loaded(object sender, RoutedEventArgs e) {
            var panel = (Grid)sender;

            _animationDelayDailyForecast += 100;

            await panel.Offset(0, 50, 0)
                        .Then()
                        .Fade(1, 500, _animationDelayDailyForecast)
                        .Offset(0, 0, 500, _animationDelayDailyForecast)
                        .StartAsync();
        }

        private void PageArrow_PointerEntered(object sender, PointerRoutedEventArgs e) {
            var arrow = (FontIcon)sender;
            arrow.Fade(1).Start();
        }

        private void PageArrow_PointerExited(object sender, PointerRoutedEventArgs e) {
            var arrow = (FontIcon)sender;
            arrow.Fade(.5f).Start();
        }

        private void Page_PointerEntered(object sender, PointerRoutedEventArgs ev) {
            if (PageArrowLeft.Visibility == Visibility.Collapsed && 
                UpdateChangeLogFlyout.Visibility == Visibility.Collapsed && 
                App.DeviceType != "Windows.Mobile") {

                _leftArrowAnimationOut?.Stop();
                _leftArrowAnimationOut?.Dispose();
                _rightArrowAnimationOut?.Stop();
                _rightArrowAnimationOut?.Dispose();

                _leftArrowAnimationOut = null;
                _rightArrowAnimationOut = null;

                PageArrowLeft.Visibility = Visibility.Visible;
                PageArrowRight.Visibility = Visibility.Visible;

                PageArrowLeft.Fade(.5f).Offset(0).Start();
                PageArrowRight.Fade(.5f).Offset(0).Start();
            }
        }

        private void Page_PointerExited(object sender, PointerRoutedEventArgs ev) {
            if (PageArrowLeft.Visibility == Visibility.Visible) { /*App.DeviceType != "Windows.Mobile" &&*/
                _leftArrowAnimationOut = PageArrowLeft.Fade(0).Offset(-30);
                _rightArrowAnimationOut = PageArrowRight.Fade(0).Offset(30);

                _leftArrowAnimationOut.Completed += (s, e) => {
                    PageArrowLeft.Visibility = Visibility.Collapsed;
                };

                _rightArrowAnimationOut.Completed += (s, e) => {
                    PageArrowRight.Visibility = Visibility.Collapsed;
                };

                _leftArrowAnimationOut.Start();
                _rightArrowAnimationOut.Start();
            }
        }

        private void PageArrowLeft_Tapped(object sender, TappedRoutedEventArgs e) {
            if (PagePivot.SelectedIndex == 0) {
                PagePivot.SelectedIndex = PagePivot.Items.Count - 1;
                return;
            }

            PagePivot.SelectedIndex -= 1;
        }

        private void PageArrowRight_Tapped(object sender, TappedRoutedEventArgs e) {
            if (PagePivot.SelectedIndex == PagePivot.Items.Count - 1) {
                PagePivot.SelectedIndex = 0;
                return;
            }

            PagePivot.SelectedIndex += 1;
        }

        private void PanelPressure_Tapped(object sender, TappedRoutedEventArgs ev) {
            var savedUnit = Settings.GetPressureUnit();
            var baseUnit = GetPressureUnits();
            var millibars = "millibars";

            var description = new TextBlock() {
                Text = App.ResourceLoader.GetString("DescriptionPressure"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                MaxWidth = 170,
                TextWrapping = TextWrapping.Wrap
            };

            var unitsChooser = new ComboBox() {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var baseUnitItem = new ComboBoxItem() {
                Content = "Hectopascal",
                Tag = "hpa"
            };

            var mercuryUnit = new ComboBoxItem() {
                Content = "Millimeter of mercury",
                Tag = "mmHg"
            };

            if (baseUnit == millibars) {
                baseUnitItem.Content = millibars;
                Tag = millibars;
            }

            unitsChooser.Items.Add(baseUnitItem);
            unitsChooser.Items.Add(mercuryUnit);

            unitsChooser.SelectedIndex = 0;

            if (savedUnit != null) {
                unitsChooser.SelectedIndex = 1;
            }

            unitsChooser.SelectionChanged += (s, e) => {
                var unit = (string)((ComboBoxItem)unitsChooser.SelectedItem).Tag;

                if (unit == "mmHg") { Settings.SavePressureUnit(unit); } else { Settings.SavePressureUnit(null); }

                SetPressureValue(_pageDataSource.Forecast.Currently);
            };

            var panel = new StackPanel();
            panel.Children.Add(description);
            panel.Children.Add(unitsChooser);

            SetFlyoutWeatherDetailContent(panel);
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void AddLocationManually_Tapped(object sender, TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(LocationsPage));
        }

        private void SummaryIcon_Loaded(object sender, RoutedEventArgs e) {
            var icon = (BitmapIcon)sender;

            if (Settings.IsApplicationThemeLight()) {
                icon.Foreground = new SolidColorBrush(Colors.Black);
                return;
            }

            icon.Foreground = new SolidColorBrush(Colors.White);
        }

        private void OutdatedDataIcon_Tapped(object sender, TappedRoutedEventArgs e) {
            if (NetworkInterface.GetIsNetworkAvailable()) return;
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        #endregion events

        #region commandbar

        void ApplyCommandBarBarFrostedGlass() {
            var glassHost = AppBarFrozenHost;
            var visual = ElementCompositionPreview.GetElementVisual(glassHost);
            var compositor = visual.Compositor;

            // Create a glass effect, requires Win2D NuGet package
            var glassEffect = new GaussianBlurEffect {
                BlurAmount = 10.0f,
                BorderMode = EffectBorderMode.Hard,
                Source = new ArithmeticCompositeEffect {
                    MultiplyAmount = 0,
                    Source1Amount = 0.5f,
                    Source2Amount = 0.5f,
                    Source1 = new CompositionEffectSourceParameter("backdropBrush"),
                    Source2 = new ColorSourceEffect {
                        Color = Color.FromArgb(255, 245, 245, 245)
                    }
                }
            };

            //  Create an instance of the effect and set its source to a CompositionBackdropBrush
            var effectFactory = compositor.CreateEffectFactory(glassEffect);
            var backdropBrush = compositor.CreateBackdropBrush();
            var effectBrush = effectFactory.CreateBrush();

            effectBrush.SetSourceParameter("backdropBrush", backdropBrush);

            // Create a Visual to contain the frosted glass effect
            var glassVisual = compositor.CreateSpriteVisual();
            glassVisual.Brush = effectBrush;

            // Add the blur as a child of the host in the visual tree
            ElementCompositionPreview.SetElementChildVisual(glassHost, glassVisual);

            // Make sure size of glass host and glass visual always stay in sync
            var bindSizeAnimation = compositor.CreateExpressionAnimation("hostVisual.Size");
            bindSizeAnimation.SetReferenceParameter("hostVisual", visual);

            glassVisual.StartAnimation("Size", bindSizeAnimation);

            glassHost.Offset(0, 50).Start();

            // EVENTS
            // ------
            AppBar.Opening += (s, e) => {
                glassHost.Offset(0, 18).Start();
            };

            AppBar.Closing += (s, e) => {
                if (AppBar.ClosedDisplayMode == AppBarClosedDisplayMode.Compact) {
                    glassHost.Offset(0, 27).Start();

                } else if (AppBar.ClosedDisplayMode == AppBarClosedDisplayMode.Minimal) {
                    glassHost.Offset(0, 50).Start();
                }
            };
        }

        private void GoToSettings_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(SettingsPage_Mobile));
        }

        private void CmdRefresh_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            _forceDataRefresh = true;
            CleanTheater();
            InitializePageData();
        }

        private void CmdLocations_Tapped(object sender, TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(LocationsPage));
        }

        #endregion commandbar

        #region others

        private async Task ShowBetaMessageAsync() {
            if (!Settings.IsFirstLaunch()) return;

            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            var message = resourceLoader.GetString("BetaMessage");

            //DataTransfer.ShowLocalToast(message);
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
            Settings.SaveFirstLaunchPassed();
        }

        private void SetFlyoutWeatherDetailContent(Panel panel) {
            FlyoutWeatherDetailContent.Children.Clear();
            FlyoutWeatherDetailContent.Children.Add(panel);
        }

        #endregion others

        #region update changelog
        
        private void ShowUpdateChangelogIfUpdated() {
            if (Settings.IsNewUpdatedLaunch()) {
                UpdateVersion.Text += string.Format(" {0}", Settings.GetAppVersion());
                ShowLastUpdateChangelog();
            }
        }

        private async void ShowLastUpdateChangelog() {
            PagePivot.IsEnabled = false;
            AppBar.IsEnabled = false;

            await UpdateChangeLogFlyout.Scale(.9f, .9f, 0, 0, 0).Fade(0).StartAsync();
            UpdateChangeLogFlyout.Visibility = Visibility.Visible;

            var x = (float)UpdateChangeLogFlyout.ActualWidth / 2;
            var y = (float)UpdateChangeLogFlyout.ActualHeight / 2;

            await UpdateChangeLogFlyout.Scale(1f, 1f, x, y).Fade(1).StartAsync();
            PagePivot.Blur(10, 500, 500).Start();
        }

        private async void HideUpdateChangelog() {
            var x = (float)UpdateChangeLogFlyout.ActualWidth / 2;
            var y = (float)UpdateChangeLogFlyout.ActualHeight / 2;

            await UpdateChangeLogFlyout.Scale(.9f, .9f, x, y).Fade(0).StartAsync();
            UpdateChangeLogFlyout.Visibility = Visibility.Collapsed;
            PagePivot.Blur(0).Start();

            PagePivot.IsEnabled = true;
            AppBar.IsEnabled = true;
        }

        private void ChangelogDismissButton_Tapped(object sender, TappedRoutedEventArgs e) {
            HideUpdateChangelog();
        }

        private void CloseChangelogFlyout_Tapped(object sender, TappedRoutedEventArgs e) {
            HideUpdateChangelog();
        }

        #endregion update changelog

        #region animations

        private void AnimateDetailsItems() {
            foreach (Grid item in PanelWeatherDetails.Children) {
                foreach (StackPanel pan in item.Children) {
                    _delayDetailItem += 100;

                    pan.Fade(0, 0)
                        .Scale(.5f, .5f, 15, 15, 0)
                        .Then()
                        .Fade(1)
                        .Scale(1, 1, 15, 15)
                        .SetDelay(_delayDetailItem)
                        .Start();
                }
            }
        }

        #endregion animations

    }
}
