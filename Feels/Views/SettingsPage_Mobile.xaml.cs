using DarkSkyApi;
using Feels.Models;
using Feels.Services;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.ApplicationModel.Core;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.ApplicationModel.Background;
using System.Collections.ObjectModel;

namespace Feels.Views {
    public sealed partial class SettingsPage_Mobile : Page {
        #region variables
        private List<WeatherUnit> Units;

        ResourceLoader _ResourcesLoader;

        private ObservableCollection<IBackgroundTaskRegistration> _ActiveTasks { get; set; }
        #endregion variables


        #region titlebar
        private void InitializeTitleBar() {
            App.DeviceType = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;

            if (App.DeviceType == "Windows.Mobile") {
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
                MainTitleBar.Opacity = 1;
            } else {
                MainTitleBar.Opacity = 0.5;
            }
        }
        #endregion titlebar

        public SettingsPage_Mobile() {
            InitializeComponent();
            InitializeVariables();
            InitializeTitleBar();
            SetUpPageAnimation();
            AnimatePersonalizationPivot();
        }

        private async void InitializeVariables() {
            InitializeUnits();
            _ResourcesLoader = new ResourceLoader();
            
        }

        void InitializeUnits() {
            Units = new List<WeatherUnit> {
                new WeatherUnit() { Name = "CA", Value = Unit.CA },
                new WeatherUnit() { Name = "SI", Value = Unit.SI },
                new WeatherUnit() { Name = "UK", Value = Unit.UK },
                new WeatherUnit() { Name = "US", Value = Unit.US }
            };
        }

        #region navigation
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            CoreWindow.GetForCurrentThread().KeyDown += SettingsPage_KeyDown;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= SettingsPage_KeyDown;
            base.OnNavigatedFrom(e);
        }

        #endregion navigation

        private void SetUpPageAnimation() {
            TransitionCollection collection = new TransitionCollection();
            NavigationThemeTransition theme = new NavigationThemeTransition();

            var info = new ContinuumNavigationTransitionInfo();

            theme.DefaultNavigationTransitionInfo = info;
            collection.Add(theme);
            Transitions = collection;
        }

        private void AnimatePersonalizationPivot() {
            PersonalizationContentPanel.AnimateSlideIn();
        }

        private void SettingsPage_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #region tile task

        private async void UpdateTileTaskSwitcher() {
            TileTaskSwitch.IsOn = BackgroundTasks.IsPrimaryTaskActivated();
        }

        /// <summary>
        /// Add or remove background task when the toggle changes state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TileTaskSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) {
                ShowTileTaskActivity();
                UpdateTileTaskActivityText();

                if (BackgroundTasks.IsPrimaryTaskActivated()) return;

                BackgroundTasks.RegisterPrimaryTileTask(GetTileIntervalUpdate());
                UpdatePrimaryTaskType();                

            } else {
                BackgroundTasks.UnregisterPrimaryTileTask();
                HideTileTaskAcitvity();
            }
        }

        private async void UpdatePrimaryTaskType() {
            var location = await Settings.GetFavoriteLocation();

            if (location == null || string.IsNullOrEmpty(location.Id)) {
                Settings.SavePrimaryTileTaskType(Settings._GPSTaskTypeKey);
                return;
            }

            Settings.SavePrimaryTileTaskType(Settings._LocationTaskTypeKey);
        }

        private void ShowTileTaskActivity() {
            TileTaskInfosPanel.AnimateSlideIn();
        }

        private async void HideTileTaskAcitvity() {
            await TileTaskInfosPanel.Offset(0, 30).Fade(0).StartAsync();
            TileTaskInfosPanel.Visibility = Visibility.Collapsed;
        }

        private void TileTaskSwitch_Loaded(object sender, RoutedEventArgs e) {
            UpdateTileTaskSwitcher();
        }

        private void TileIntervalUpdate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!TileTaskSwitch.IsOn) return;

            BackgroundTasks.UnregisterPrimaryTileTask();
            BackgroundTasks.RegisterPrimaryTileTask(GetTileIntervalUpdate());
        }

        private uint GetTileIntervalUpdate() {
            var item = (ComboBoxItem)TileIntervalUpdate.SelectedItem;
            string value = (string)item.Tag;
            return uint.Parse(value);
        }

        private void RestartTileTask_Tapped(object sender, TappedRoutedEventArgs e) {
            BackgroundTasks.UnregisterPrimaryTileTask();
            BackgroundTasks.RegisterPrimaryTileTask(GetTileIntervalUpdate());
        }

        private void TileIntervalUpdate_Loaded(object sender, RoutedEventArgs e) {
            var savedInterval = BackgroundTasks.GetTileTaskInterval();

            for (int i = 0; i < TileIntervalUpdate.Items.Count; i++) {
                var item = (ComboBoxItem)TileIntervalUpdate.Items[i];
                var itemInterval = uint.Parse((string)item.Tag);

                if (itemInterval == savedInterval) {
                    TileIntervalUpdate.SelectedIndex = i;
                    break;
                }
            }
        }

        private void UpdateTileTaskActivityText() {
            var activity = BackgroundTasks.GetTileTaskActivity();
            if (activity == null) return;

            var message = _ResourcesLoader.GetString("TileLastRun");
            LastUpdatedTask.Text = message + activity["LastRun"];

            if (activity["Exception"] != null) {
                var reason = (string)activity["Exception"];
                LastTileTaskError.Text = reason;
            }
        }
        #endregion tile task

        #region lockscreen task

        private void LockTaskSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) BackgroundTasks.RegisterLockscreenTask();
            else BackgroundTasks.UnregisterLockscreenTask();
        }

        private void LockTaskSwitch_Loaded(object sender, RoutedEventArgs e) {
            //UpdateWallTaskSwitcher();
        }

        #endregion lockscreen task

        #region about
        private void Email_Tapped(object sender, TappedRoutedEventArgs e) {
            FeedbackButton_Click(sender, e);
        }

        private async void Twitter_Tapped(object sender, TappedRoutedEventArgs e) {
            var uriTwitter = new Uri("https://twitter.com/jeremiecorpinot");
            var success = await Windows.System.Launcher.LaunchUriAsync(uriTwitter);
        }

        private async void PrivacyPolicyGitHub_Tapped(object sender, TappedRoutedEventArgs e) {
            var uriGitHub = new Uri("https://github.com/rootasjey/Feels");
            var success = await Windows.System.Launcher.LaunchUriAsync(uriGitHub);
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e) {
            EmailMessage email = new EmailMessage() {
                Subject = "[Feels] Feedback",
                Body = "send this email to jeremiecorpinot@outlook.com"
            };

            // TODO : add app infos
            EmailManager.ShowComposeNewEmailAsync(email);
        }

        private async void NoteButton_Click(object sender, RoutedEventArgs e) {
            string appID = "9NB305KW0MBP";

            var op = await Windows.System.Launcher
                .LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=" + appID));
        }

        private async void LockscreenButton_Click(object sender, RoutedEventArgs e) {
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:lockscreen"));
        }

        #endregion about

        #region languages
        private void LanguageSelection_Loaded(object sender, RoutedEventArgs e) {
            var language = Settings.GetAppCurrentLanguage();

            var culture = new CultureInfo(language);

            if (culture.CompareInfo.IndexOf(language, "en", CompareOptions.IgnoreCase) >= 0) {
                LanguageSelection.SelectedIndex = 0;
                return;
            }

            if (culture.CompareInfo.IndexOf(language, "fr", CompareOptions.IgnoreCase) >= 0) {
                LanguageSelection.SelectedIndex = 1;
                return;
            }

            if (culture.CompareInfo.IndexOf(language, "ru", CompareOptions.IgnoreCase) >= 0) {
                LanguageSelection.SelectedIndex = 2;
                return;
            }
        }

        private void LanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var item = (ComboBoxItem)LanguageSelection.SelectedItem;
            var language = (string)item.Tag;
            var fullLang = (string)item.Content;

            if (language == Settings.GetAppCurrentLanguage()) return;
            Settings.SaveAppCurrentLanguage(language);

            App.UpdateLanguage();
            //ToastLanguageUpdated();
            AutoRefreshDataOnNextNavigation();

            void ToastLanguageUpdated()
            {
                var selectedMessage = _ResourcesLoader.GetString("LanguageSelected");
                var toastMessage = string.Format("{0} {1}", fullLang, selectedMessage);
                //Notify(toastMessage);
            }
        }


        //void UpdateSelectedLanguage() {
        //    var lang = Settings.GetLanguage();

        //    var culture = new CultureInfo(lang);
        //    if (culture.CompareInfo.IndexOf(lang, "fr", CompareOptions.IgnoreCase) >= 0) {
        //        FrenchLanguageItem.IsChecked = true;
        //        return;
        //    }
        //    if (culture.CompareInfo.IndexOf(lang, "en", CompareOptions.IgnoreCase) >= 0) {
        //        EnglishLanguageItem.IsChecked = true;
        //        return;
        //    }
        //}

        //private void EnglishLanguage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
        //    SaveAndUpdateLanguage((ToggleMenuFlyoutItem)sender);
        //}

        //private void FrenchLanguage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
        //    SaveAndUpdateLanguage((ToggleMenuFlyoutItem)sender);
        //}

        //private void SaveAndUpdateLanguage(ToggleMenuFlyoutItem item) {
        //    var lang = (string)item.Tag;

        //    if (lang == Settings.GetLanguage()) return;

        //    Settings.SaveLanguage(lang);
        //    App.UpdateLanguage();
        //    UnselectOtherLanguages(item);
        //    ToastLanguageUpdated();

        //    void ToastLanguageUpdated()
        //    {
        //        var fullLang = lang == "EN" ? "English" : "French";
        //        var toastMessage = fullLang + " language selected!";
        //        DataTransfer.ShowLocalToast(toastMessage);
        //    }
        //}

        //void UnselectOtherLanguages(ToggleMenuFlyoutItem selectedItem) {
        //    foreach (ToggleMenuFlyoutItem item in LanguageFlyout.Items) {
        //        item.IsChecked = false;
        //    }

        //    selectedItem.IsChecked = true;
        //}

        #endregion languages

        #region temperature unit
        private void UnitsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selectedUnit = (WeatherUnit)e.AddedItems[0];
            var unit = Settings.GetUnit();

            if (selectedUnit.Value == unit) return;

            Settings.SaveUnit(selectedUnit.Value);

            AutoRefreshDataOnNextNavigation();                      
        }

        private void UnitsCombo_Loaded(object sender, RoutedEventArgs e) {
            var unit = Settings.GetUnit();

            for (int i = 0; i < UnitsCombo.Items.Count; i++) {
                var currentUnit = (WeatherUnit)UnitsCombo.Items[i];

                if (currentUnit.Value == unit) {
                    UnitsCombo.SelectedIndex = i;
                    break;
                }
            }
        }
        #endregion temperature unit

        #region personalization
        void AutoRefreshDataOnNextNavigation() {
            HomePage._ForceDataRefresh = true;
        }

        private void BackgroundColorAnimationToggle_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            var isAlreadyDeactivated = Settings.IsSceneColorAnimationDeactivated();

            if (toggle.IsOn) {
                if (isAlreadyDeactivated) return;

                Settings.SaveSceneColorAnimationDeactivated(true);
                return;
            }

            Settings.SaveSceneColorAnimationDeactivated(false);
        }

        private void BackgroundColorAnimationToggle_Loaded(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;
            toggle.IsOn = Settings.IsSceneColorAnimationDeactivated();
        }

        private void ThemeSwitch_Loaded(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;
            toggle.IsOn = Settings.IsApplicationThemeLight();
        }

        private void ThemeSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) Settings.UpdateAppTheme(ApplicationTheme.Light);
            else Settings.UpdateAppTheme(ApplicationTheme.Dark);
        }

        #endregion personalization

        private void SecondaryTasksListView_Loaded(object sender, RoutedEventArgs e) {
            var listView = (ListView)sender;

            _ActiveTasks = new ObservableCollection<IBackgroundTaskRegistration>(BackgroundTasks.GetAllTasks());
            listView.ItemsSource = _ActiveTasks;

            if (listView.Items.Count > 0) {
                EmptyViewSecondaryTasks.Visibility = Visibility.Collapsed;
            }
        }

        private void TaskListItem_RightCommandRequested(object sender, EventArgs e) {
            var listItem = (SlidableListItem)sender;
            var task = (IBackgroundTaskRegistration)listItem.DataContext;
            DeleteSecondaryTask(task);
        }

        private async void DeleteSecondaryTask(IBackgroundTaskRegistration task) {
            var taskName = task.Name;

            await Settings.DeleteSecondaryTaskLocation(taskName);
            BackgroundTasks.UnregisterSecondaryTileTask(taskName);

            _ActiveTasks.Remove(task);

            if (_ActiveTasks.Count == 0) {
                EmptyViewSecondaryTasks.Visibility = Visibility.Visible;
            }
        }

        private void TaskListItem_RightTapped(object sender, RightTappedRoutedEventArgs e) {
            var listItem = (SlidableListItem)sender;
            var task = (IBackgroundTaskRegistration)listItem.DataContext;

            SavedLocationRightTappedFlyout.ShowAt(listItem);
        }

        private void CmdDeleteSecondaryTask_Tapped(object sender, TappedRoutedEventArgs e) {
            var menuFlyoutItem = (MenuFlyoutItem)sender;
            var task = (IBackgroundTaskRegistration)menuFlyoutItem.DataContext;
            DeleteSecondaryTask(task);
        }
    }
}