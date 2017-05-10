using Feels.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.ApplicationModel.Email;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Feels.Views {
    public sealed partial class SettingsPage_Mobile : Page {
        private List<string> Units;

        public SettingsPage_Mobile() {
            InitializeComponent();
            UpdateSelectedLanguage();
            InitializeUnits();
        }

        void InitializeUnits() {
            Units.Add("SI");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            CoreWindow.GetForCurrentThread().KeyDown += SettingsPage_KeyDown;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown -= SettingsPage_KeyDown;
            base.OnNavigatedFrom(e);
        }

        private void SettingsPage_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        private void UpdateQuoteTaskSwitcher() {
            QuotesTaskSwitch.IsOn = BackgroundTasks.IsQuoteTaskActivated();
        }

        private void UpdateWallTaskSwitcher() {
            LockTaskSwitch.IsOn = BackgroundTasks.IsLockscreenTaskActivated();
        }

        private void UpdateThemeSwitcher() {
            ThemeSwitch.IsOn = Settings.IsApplicationThemeLight();
        }

        /// <summary>
        /// Add or remove background task when the toggle changes state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QuotesTaskSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) BackgroundTasks.RegisterQuoteTask();
            else BackgroundTasks.UnregisterQuoteTask();
        }

        private void LockTaskSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) BackgroundTasks.RegisterLockscreenTask();
            else BackgroundTasks.UnregisterLockscreenTask();
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e) {
            EmailMessage email = new EmailMessage() {
                Subject = "[Citations 365] Feedback",
                Body = "send this email to metrodevapp@outlook.com"
            };

            // TODO : add app infos
            EmailManager.ShowComposeNewEmailAsync(email);
        }

        private async void NoteButton_Click(object sender, RoutedEventArgs e) {
            string appID = "9wzdncrcwfqr";
            var op = await Windows.System.Launcher
                .LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=" + appID));
        }

        private async void LockscreenButton_Click(object sender, RoutedEventArgs e) {
            // Launch URI for the lock screen settings screen. 
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:lockscreen"));
        }

        private void ChangeTheme(ApplicationTheme theme) {
            Settings.UpdateAppTheme(theme);
        }

        private void ThemeSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) ChangeTheme(ApplicationTheme.Light);
            else ChangeTheme(ApplicationTheme.Dark);
        }

        void UpdateSelectedLanguage() {
            var lang = Settings.GetLanguage();

            var culture = new CultureInfo(lang);
            if (culture.CompareInfo.IndexOf(lang, "fr", CompareOptions.IgnoreCase) >= 0) {
                FrenchLanguageItem.IsChecked = true;
                return;
            }
            if (culture.CompareInfo.IndexOf(lang, "en", CompareOptions.IgnoreCase) >= 0) {
                EnglishLanguageItem.IsChecked = true;
                return;
            }
        }

        private void SetLockscreen_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            Wallpaper.SetWallpaperAsync();
        }

        private void EnglishLanguage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            SaveAndUpdateLanguage((ToggleMenuFlyoutItem)sender);
        }

        private void FrenchLanguage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            SaveAndUpdateLanguage((ToggleMenuFlyoutItem)sender);
        }

        private void SaveAndUpdateLanguage(ToggleMenuFlyoutItem item) {
            var lang = (string)item.Tag;

            if (lang == Settings.GetLanguage()) return;

            Settings.SaveLanguage(lang);
            App.UpdateLanguage();
            UnselectOtherLanguages(item);
            ToastLanguageUpdated();

            void ToastLanguageUpdated()
            {
                var fullLang = lang == "EN" ? "English" : "French";
                var toastMessage = fullLang + " language selected!";
                DataTransfer.ShowLocalToast(toastMessage);
            }
        }

        void UnselectOtherLanguages(ToggleMenuFlyoutItem selectedItem) {
            foreach (ToggleMenuFlyoutItem item in LanguageFlyout.Items) {
                item.IsChecked = false;
            }

            selectedItem.IsChecked = true;
        }

        private void QuotesTaskSwitch_Loaded(object sender, RoutedEventArgs e) {
            //UpdateQuoteTaskSwitcher();
        }

        private void LockTaskSwitch_Loaded(object sender, RoutedEventArgs e) {
            //UpdateWallTaskSwitcher();
        }

        private void ThemeSwitch_Loaded(object sender, RoutedEventArgs e) {
            //UpdateThemeSwitcher();
        }

        private void UnitsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }
    }
}