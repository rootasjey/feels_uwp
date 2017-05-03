using Feels.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.ApplicationModel.Email;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Feels.Views {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage_Desktop : Page {
        public SettingsPage_Desktop() {
            InitializeComponent();
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

        private void TasksSection_Loaded(object sender, RoutedEventArgs e) {
            //UpdateQuoteTaskSwitcher();
            //UpdateWallTaskSwitcher();
        }

        private void PersonalizationSection_Loaded(object sender, RoutedEventArgs e) {
            //UpdateThemeSwitcher();
            //UpdateSelectedLanguage();
        }

        private void UpdateQuoteTaskSwitcher() {
            var TaskSwitch = (ToggleSwitch)UI.FindChildControl<ToggleSwitch>(TasksSection, "TaskSwitch");
            TaskSwitch.IsOn = BackgroundTasks.IsQuoteTaskActivated();
        }

        private void UpdateWallTaskSwitcher() {
            var LockscreenSwitch = (ToggleSwitch)UI.FindChildControl<ToggleSwitch>(TasksSection, "LockscreenSwitch");
            LockscreenSwitch.IsOn = BackgroundTasks.IsLockscreenTaskActivated();
        }

        private void UpdateThemeSwitcher() {
            var ThemeSwitch = (ToggleSwitch)UI.FindChildControl<ToggleSwitch>(PersonalizationSection, "ThemeSwitch");
            ThemeSwitch.IsOn = Settings.IsApplicationThemeLight();
        }

        /// <summary>
        /// Add or remove background task when the toggle changes state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) BackgroundTasks.RegisterQuoteTask();
            else BackgroundTasks.UnregisterQuoteTask();
        }

        private void LockscreenSwitch_Toggled(object sender, RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) BackgroundTasks.RegisterLockscreenTask();
            else BackgroundTasks.UnregisterLockscreenTask();
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
            string appID = "9wzdncrcwfqr";
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=" + appID));
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
            if (toggle.IsOn) {
                ChangeTheme(ApplicationTheme.Light);
            } else {
                ChangeTheme(ApplicationTheme.Dark);
            }
        }

        private void Background_Choosed(object sender, RoutedEventArgs e) {
            var radioButton = (RadioButton)sender;
            string background = radioButton.Name;

            //Scontroller.UpdateAppBackground(background);
        }

        void UpdateSelectedLanguage(IList<MenuFlyoutItemBase> FlyoutItems) {
            var EnglishLanguageItem = (ToggleMenuFlyoutItem)FlyoutItems[0];
            var FrenchLanguageItem = (ToggleMenuFlyoutItem)FlyoutItems[1];

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
            var LanguageButton = (Button)UI.FindChildControl<Button>(PersonalizationSection, "LanguageButton");
            var LanguageFlyout = (MenuFlyout)LanguageButton.Flyout;

            foreach (ToggleMenuFlyoutItem item in LanguageFlyout.Items) {
                item.IsChecked = false;
            }

            selectedItem.IsChecked = true;
        }

        private void LanguageFlyout_Opened(object sender, object e) {
            var LanguageFlyout = (MenuFlyout)sender;
            var FlyoutItems = (IList<MenuFlyoutItemBase>)LanguageFlyout.Items;
            UpdateSelectedLanguage(FlyoutItems);
        }

    }
}
