using DarkSkyApi.Models;
using Feels.Data;
using Feels.Models;
using Feels.Services;
using Feels.Services.WeatherScene;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;

namespace Feels.Views {
    public sealed partial class HomePage : Page {
        #region variables

        private SourceModel _PageDataSource { get; set; }

        CoreDispatcher _UIDispatcher { get; set; }

        private int _AnimationDelayHourForcast { get; set; }

        private int _AnimationDelayDailyForecast { get; set; }

        private int _DelayDetailItem { get; set; }

        static DateTime _LastFetchedTime { get; set; }

        static BasicGeoposition _LastPosition { get; set; }

        private static LocationItem _LastLocation { get; set; }

        public static bool _ForceDataRefresh { get; set; }

        // Composition
        private Compositor _Compositor { get; set; }

        private Visual _EarthVisual { get; set; }

        private Visual _PinEarthVisual { get; set; }

        private AnimationSet _LeftArrowAnimationOut { get; set; }

        private AnimationSet _RightArrowAnimationOut { get; set; }

        #endregion variables

        public HomePage() {
            InitializeComponent();
            InitializeTitleBar();
            InitializeVariables();
            InitialzeEvents();
            InitializePageData();

            BackgroundTasks.CheckAllTasks();

            ApplyCommandBarBarFrostedGlass();
            ShowUpdateChangelogIfUpdated();
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

        #region data

        private void InitializeVariables() {
            _PageDataSource = App.DataSource;
            _UIDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private async void InitializePageData() {
            LoadLastFetchedData();
            
            if (await MustRefreshData()) {
                CleanTheater();
                FetchNewData();
            }
        }

        /// <summary>
        /// Load old data before checking if refresh must be made
        /// so the app seems fast.
        /// </summary>
        private void LoadLastFetchedData() {
            if (_PageDataSource.Forecast != null && _ForceDataRefresh == false) {
                HideSplashView();
                HideLoadingView();
                SetCurrentCity();
                PopulateView();
            }
        }

        /// <summary>
        /// This test may take some seconds (~10sec) 
        /// because it has to retrieve GPS location, then fetch data.
        /// </summary>
        /// <returns>True if data must be refreshed.</returns>
        private async Task<bool> MustRefreshData() {
            if (_PageDataSource.Forecast == null) return true;

            if (_ForceDataRefresh) {
                _ForceDataRefresh = false;
                return true;
            }

            if (DateTime.Now.Hour - _LastFetchedTime.Hour > 0) {
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

            var prevLat = (int)_LastPosition.Latitude;
            var prevLon = (int)_LastPosition.Longitude;

            if (currLat != prevLat || currLon != prevLon) {
                return true;
            }

            return false;
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

            await FetchCurrentWeather(location);

            HideLoadingView();
            PopulateView();
        }

        private void InitialzeEvents() {
            Application.Current.Resuming += App_Resuming;
        }

        private void App_Resuming(object sender, object e) {
            var task = _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                InitializePageData();
            });
        }

        private void PopulateView() {
            PopulateFirstPage();
            BindHourlyListData();
            BindDailyListData();

            //ShowBetaMessageAsync();
        }

        private void SafeExit() {
            ShowEmptyView();
        }

        private void ShowEmptyView() {
            ShowNoAccessView();
        }

        private void PopulateFirstPage() {
            if (_PageDataSource.Forecast == null) return;

            WeatherView.Visibility = Visibility.Visible;

            var weatherCurrent = _PageDataSource.Forecast.Currently;
            var weatherToday = _PageDataSource.Forecast.Daily.Days[0];

            WeatherViewContent.AnimateSlideIn();

            UI.AnimateNumericValue((int)weatherCurrent.Temperature, Temperature, _UIDispatcher, "°");
            PopulateForecastView(weatherToday, weatherCurrent);
            AnimateDetailsItems();
            SetMoonPhase(weatherToday);
            AnimateWindDirectionIcons();

            DrawScene();
            UpdateMainTile();

            //var autoEvent = new AutoResetEvent(true);

            //var deffered = new Timer(async (object state) => {
            //    await _UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
            //        DrawScene();
            //        UpdateMainTile();
            //    });
            //}, autoEvent, 500, Timeout.Infinite);
        }

        private void PopulateForecastView(DayDataPoint todayWeather, CurrentDataPoint currentWeather) {
            Status.Text = currentWeather.Summary;
            FeelsLike.Text += string.Format(" {0}°{1}", currentWeather.ApparentTemperature, Settings.GetTemperatureUnit());
            PrecipProbaValue.Text = string.Format("{0}%", todayWeather.PrecipitationProbability * 100);
            HumidityValue.Text = string.Format("{0}%", currentWeather.Humidity * 100);

            SunriseTime.Text = todayWeather.SunriseTime.ToLocalTime().ToString("HH:mm");
            SunsetTime.Text = todayWeather.SunsetTime.ToLocalTime().ToString("HH:mm");
            MoonriseTime.Text = todayWeather.SunsetTime.ToLocalTime().ToString("HH:mm");
            MoonsetTime.Text = todayWeather.SunriseTime.ToLocalTime().ToString("HH:mm");

            WindSpeed.Text = string.Format("{0}{1}", currentWeather.WindSpeed, GetWindSpeedUnits());

            WindDirection.Text = string.Format("{0}°", currentWeather.WindBearing);
            WindDirectionIcon
                .Rotate(currentWeather.WindBearing + 180, 15, 15)
                .Start();

            CloudCover.Text = string.Format("{0}%", currentWeather.CloudCover * 100);
            

            LastTimeUpdate.Text = DateTime.Now.ToLocalTime().ToString("dddd HH:mm");

            if (_PageDataSource.Forecast.Daily.Days == null ||
                _PageDataSource.Forecast.Daily.Days.Count == 0) return;

            var currentDay = _PageDataSource.Forecast.Daily.Days[0];
            var maxTemp = (int)currentDay.MaxTemperature;
            var minTemp = (int)currentDay.MinTemperature;

            MaxTempValue.Text = maxTemp.ToString();
            MinTempValue.Text = minTemp.ToString();

            SetPressureValue(currentWeather, true);
        }

        private void AnimateWindDirectionIcons() {
            var baseAngle = 0;

            var visual = ElementCompositionPreview.GetElementVisual(WindDirectionIcon);
            var compositor = visual.Compositor;

            var animationRotate = compositor.CreateScalarKeyFrameAnimation();
            animationRotate.InsertKeyFrame(0f, baseAngle);
            animationRotate.InsertKeyFrame(1f, baseAngle + 20);
            animationRotate.Duration = TimeSpan.FromSeconds(3);
            animationRotate.IterationBehavior = AnimationIterationBehavior.Forever;
            animationRotate.Direction = Windows.UI.Composition.AnimationDirection.Alternate;

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
            if (_PageDataSource.Forecast == null) return;
            HourlyList.ItemsSource = _PageDataSource.Forecast.Hourly.Hours;
            HourlySummary.Text = _PageDataSource.Forecast.Hourly.Summary;
        }

        private void BindDailyListData() {
            if (_PageDataSource.Forecast == null) return;
            DailyList.ItemsSource = _PageDataSource.Forecast.Daily.Days;
            DailySummary.Text = _PageDataSource.Forecast.Daily.Summary;
        }

        private void ResetOpacity(Panel view) {
            view.Opacity = 0;
            var children = view.Children;

            foreach (var child in children) {
                child.Opacity = 0;
            }
        }

        private void ShowNoAccessView() {
            Theater.Visibility = Visibility.Collapsed;
            SplashView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Collapsed;

            UI.AnimateSlideIn(LocationDisabledMessage);
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
            var location = await Settings.GetFavoriteLocation();

            if (UseGPSLocation(location)) {
                Geopoint pointToReverseGeocode = new Geopoint(_LastPosition);

                // Reverse geocode the specified geographic location.
                MapLocationFinderResult result =
                    await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

                // If the query returns results, display the name of the town
                // contained in the address of the first result.
                if (result.Status == MapLocationFinderStatus.Success) {
                    if (result.Locations.Count == 0) return;
                    TownTextBlock.Text = result.Locations[0].Address.Town;
                }

                return;
            }
            
            TownTextBlock.Text = location.Town;
        }

        private bool UseGPSLocation(LocationItem location) {
            if (location == null) return true;
            return string.IsNullOrEmpty(location.Id);
        }

        private async Task FetchCurrentWeather(LocationItem location) {
            await _PageDataSource.FetchCurrentForecast(location.Latitude, location.Longitude);

            _LastFetchedTime = DateTime.Now;

            if (_PageDataSource.Forecast == null) {
                SafeExit();
                return;
            }

            NowPivot.DataContext = _PageDataSource.Forecast.Currently;
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

            UI.AnimateSlideIn(LoadingView);
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
            animationRotate.Direction = Windows.UI.Composition.AnimationDirection.Alternate;

            visual.RotationAxis = new Vector3(0, 0, 1);
            visual.CenterPoint = new Vector3((float)EarthIcon.Height / 2, (float)EarthIcon.Width / 2, 0);

            visual.StartAnimation("RotationAngle", animationRotate);

            _EarthVisual = visual;
            _Compositor = compositor;
        }

        void StopEarthRotation() {
            _EarthVisual?.StopAnimation("RotationAngle");
        }

        void StartPinEarthOffsetAnimation() {
            var visual = ElementCompositionPreview.GetElementVisual(PinEarthIcon);
            ElementCompositionPreview.SetIsTranslationEnabled(PinEarthIcon, true);

            var animationOffset = _Compositor.CreateScalarKeyFrameAnimation();
            animationOffset.InsertKeyFrame(0f, 0);
            animationOffset.InsertKeyFrame(1f, 20);
            animationOffset.Duration = TimeSpan.FromSeconds(3);
            animationOffset.Direction = Windows.UI.Composition.AnimationDirection.Alternate;
            animationOffset.IterationBehavior = AnimationIterationBehavior.Forever;

            visual.StartAnimation("Translation.y", animationOffset);

            _PinEarthVisual = visual;
        }

        void StopPinEarthAnimation() {
            _PinEarthVisual?.StopAnimation("Translation.y");
        }

        #endregion loading view

        #region scene theater

        private async void DrawScene() {
            var scene = Scenes.CreateNew(
                _PageDataSource.Forecast.Currently, 
                _PageDataSource.Forecast.Daily.Days[0],
                Settings.IsSceneColorAnimationDeactivated());

            Theater.Children.Add(scene);

            await scene.Fade(0, 0).Offset(0, 200, 0).StartAsync();
            Theater.Fade(1, 1000, 500).Start();
            scene.Fade(1, 1000).Offset(0, 0, 1000).SetDelay(500).Start();
        }

        private void CleanTheater() {
            Theater.Children.Clear();
            Theater.Fade(0).Start();
        }

        #endregion scene theater

        #region buttons

        private async void LocationSettingsButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            bool result = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private void TryAgainButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            LocationDisabledMessage.Visibility = Visibility.Collapsed;

            _ForceDataRefresh = true;
            CleanTheater();
            InitializePageData();
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

                _LeftArrowAnimationOut?.Stop();
                _LeftArrowAnimationOut?.Dispose();
                _RightArrowAnimationOut?.Stop();
                _RightArrowAnimationOut?.Dispose();

                _LeftArrowAnimationOut = null;
                _RightArrowAnimationOut = null;

                PageArrowLeft.Visibility = Visibility.Visible;
                PageArrowRight.Visibility = Visibility.Visible;

                PageArrowLeft.Fade(.5f).Offset(0).Start();
                PageArrowRight.Fade(.5f).Offset(0).Start();
            }
        }

        private void Page_PointerExited(object sender, PointerRoutedEventArgs ev) {
            if (PageArrowLeft.Visibility == Visibility.Visible) { /*App.DeviceType != "Windows.Mobile" &&*/
                _LeftArrowAnimationOut = PageArrowLeft.Fade(0).Offset(-30);
                _RightArrowAnimationOut = PageArrowRight.Fade(0).Offset(30);

                _LeftArrowAnimationOut.Completed += (s, e) => {
                    PageArrowLeft.Visibility = Visibility.Collapsed;
                };

                _RightArrowAnimationOut.Completed += (s, e) => {
                    PageArrowRight.Visibility = Visibility.Collapsed;
                };

                _LeftArrowAnimationOut.Start();
                _RightArrowAnimationOut.Start();
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

                SetPressureValue(_PageDataSource.Forecast.Currently);
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

        private void GoToAchievements_Tapped(object sender, TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(AchievementsPage));
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
            _ForceDataRefresh = true;
            CleanTheater();
            InitializePageData();
        }

        private void CmdLocations_Tapped(object sender, TappedRoutedEventArgs e) {
            Frame.Navigate(typeof(LocationsPage));
        }

        #endregion commandbar

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

        private void SetFlyoutWeatherDetailContent(Panel panel) {
            FlyoutWeatherDetailContent.Children.Clear();
            FlyoutWeatherDetailContent.Children.Add(panel);
        }

        #endregion others

        #region update changelog
        /// <summary>
        /// Show update change log if the app has been updated
        /// </summary>
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
                    _DelayDetailItem += 100;

                    pan.Fade(0, 0)
                        .Scale(.5f, .5f, 15, 15, 0)
                        .Then()
                        .Fade(1)
                        .Scale(1, 1, 15, 15)
                        .SetDelay(_DelayDetailItem)
                        .Start();
                }
            }
        }
        #endregion animations

    }
}
