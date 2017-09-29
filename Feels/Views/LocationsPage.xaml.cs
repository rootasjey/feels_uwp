using System;
using Feels.Services;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Windows.UI.Xaml.Navigation;
using Feels.Models;
using System.Collections.ObjectModel;
using Windows.Services.Maps;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Windows.ApplicationModel.Core;

namespace Feels.Views {
    public sealed partial class LocationsPage : Page {
        #region variables

        private ObservableCollection<LocationItem> _savedLocations { get; set; }

        private ObservableCollection<MapLocation> _foundLocations { get; set; }

        private static LocationItem _lastLocationSelected { get; set; }

        private string _lastLocationQuery { get; set; }

        private double _delaySavedLocationList { get; set; }

        #endregion variables

        #region navigation

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            if (SearchLocationPanel.Visibility == Visibility.Visible) {
                ShowContentLocationPanel();
                e.Cancel = true;

            } else { CoreWindow.GetForCurrentThread().KeyDown -= Page_KeyDown; }
            
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;
            base.OnNavigatedTo(e);
        }

        private void Page_KeyDown(CoreWindow sender, KeyEventArgs args) {
            if (SearchLocationBox.FocusState != FocusState.Unfocused) {
                return;
            }

            if (Events.IsBackOrEscapeKey(args.VirtualKey) && Frame.CanGoBack) {
                Frame.GoBack();
            }
        }

        #endregion navigation

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
                //BackButtonGrid.Visibility = Visibility.Visible;
                MainTitleBar.Opacity = 1;
            } else {
                //BackButtonGrid.Visibility = Visibility.Collapsed;
                MainTitleBar.Opacity = 0.5;
            }
        }

        #endregion titlebar

        public LocationsPage() {
            InitializeComponent();
            InitializeVariables();
            InitializePageAnimation();
            InitializeTitleBar();
            LoadData();
        }

        #region data

        private void InitializeVariables() {
            _savedLocations = new ObservableCollection<LocationItem>();
            _foundLocations = new ObservableCollection<MapLocation>();

            FoundLocationsListView.ItemsSource = _foundLocations;
        }

        private void LoadData() {
            LoadSavedLocations();
        }

        private async void LoadSavedLocations() {
            var savedLocations = await Settings.GetSavedLocationAsync();

            if (savedLocations?.Count > 0) {
                var favoriteLocation = _lastLocationSelected;

                if (favoriteLocation == null) {
                    favoriteLocation = await Settings.GetFavoriteLocation();
                }

                foreach (var location in savedLocations) {
                    if (favoriteLocation != null && 
                        location.Latitude == favoriteLocation.Latitude && 
                        location.Longitude == favoriteLocation.Longitude) {

                        location.IsSelected = true;

                    } else if (favoriteLocation == null & string.IsNullOrEmpty(location.Id)) {
                        location.IsSelected = true; // select GPS if no favorite location
                    }

                    _savedLocations.Add(location);
                }

            } else {
                var currentLocation = new LocationItem() {
                    Id = "",
                    Name = App.ResourceLoader.GetString("MyCurrentPosition"),
                    Latitude = 0,
                    Longitude = 0
                };

                _savedLocations.Add(currentLocation);
                _savedLocations[0].IsSelected = true;
            }

            LoadingView.Visibility = Visibility.Collapsed;
            SavedLocationsListView.ItemsSource = _savedLocations;
        }

        #endregion data

        #region animations

        private void InitializePageAnimation() {
            TransitionCollection collection = new TransitionCollection();
            NavigationThemeTransition theme = new NavigationThemeTransition();

            var info = new SlideNavigationTransitionInfo();

            theme.DefaultNavigationTransitionInfo = info;
            collection.Add(theme);
            Transitions = collection;
        }

        private void ShowSearchLocationPanel() {
            SearchLocationPanel.Opacity = 0;
            SearchLocationPanel.Visibility = Visibility.Visible;

            SearchLocationPanel.Offset(0, 0).Fade(1).Start();
            PageLocationContent.Offset(0, 50, 0).Then()
                .Offset(0, -50).Fade(0).Start();

            CmdCancelAddCity.Visibility = Visibility.Visible;
            CmdAddNewCity.Visibility = Visibility.Collapsed;

            SearchLocationBox.Focus(FocusState.Programmatic);
        }

        private void ShowContentLocationPanel() {
            var offsetAnimation = SearchLocationPanel.Offset(0, 50).Fade(0);
            offsetAnimation.Completed += (s, e) => {
                SearchLocationPanel.Visibility = Visibility.Collapsed;
            };

            offsetAnimation.Start();

            PageLocationContent.Offset(0, 0).Fade(1).Start();

            CmdCancelAddCity.Visibility = Visibility.Collapsed;
            CmdAddNewCity.Visibility = Visibility.Visible;
        }

        #endregion animations

        #region commandbar

        private void CmdAddNewCity_Tapped(object sender, TappedRoutedEventArgs e) {
            ShowSearchLocationPanel();
        }

        private void CmdCancelAddCity_Tapped(object sender, TappedRoutedEventArgs e) {
            ShowContentLocationPanel();
        }

        #endregion commandbar

        #region events
        protected override void OnGotFocus(RoutedEventArgs e) {
            RefreshSavedLocationsItems();
            base.OnGotFocus(e);
        }

        private async void SearchLocationBox_KeyUp(object sender, KeyRoutedEventArgs e) {
            var query = ((TextBox)sender).Text;

            if (_lastLocationQuery == query) return;
            _lastLocationQuery = query;

            if (query.Length < 3) return;

            var results = await GetLocationFrom(query);

            if (results.Count == 0) {
                EmptyViewFoundLocations.Visibility = Visibility.Visible;
                FoundLocationsListView.Visibility = Visibility.Collapsed;
                return;
            }

            FoundLocationsListView.Visibility = Visibility.Visible;
            EmptyViewFoundLocations.Visibility = Visibility.Collapsed;

            _foundLocations.Clear();

            foreach (var mapLocation in results) {
                _foundLocations.Add(mapLocation);
            }
        }

        private void AddNewLocation_Tapped(object sender, TappedRoutedEventArgs e) {
            var grid = (Grid)sender;
            var mapLocation = (MapLocation)grid.DataContext;

            var newLocation = new LocationItem() {
                Id = mapLocation.DisplayName,
                Name = mapLocation.DisplayName,
                Town = mapLocation.Address.Town,
                Address = mapLocation.Address.FormattedAddress,
                Latitude = mapLocation.Point.Position.Latitude,
                Longitude = mapLocation.Point.Position.Longitude
            };

            _savedLocations.Add(newLocation);

            ShowContentLocationPanel();

            Settings.SaveLocationsAsync(_savedLocations.ToList());
        }

        private async void LocationSelected_Tapped(object sender, TappedRoutedEventArgs e) {
            var listItem = (SlidableListItem)sender;
            var location = (LocationItem)listItem.DataContext;

            UnselectAnyLocation();
            location.IsSelected = true;
            _lastLocationSelected = location;

            if (string.IsNullOrEmpty(location.Id)) {
                Settings.SavePrimaryTileTaskType(Settings._GPSTaskTypeKey);

            } else { Settings.SavePrimaryTileTaskType(Settings._LocationTaskTypeKey); }

            await Settings.SaveFavoriteLocation(location);
            await Settings.SaveLocationsAsync(_savedLocations.ToList());

            HomePage._ForceDataRefresh = true;
            Frame.Navigate(typeof(HomePage));
        }

        private void SlidableListItem_RightCommandRequested(object sender, EventArgs e) {
            var listItem = (SlidableListItem)sender;
            var location = (LocationItem)listItem.DataContext;

            DeleteSavedLocation(location);
        }

        private void CmdDeleteSavedLocation_Tapped(object sender, TappedRoutedEventArgs e) {
            DeleteSavedLocation(_lastLocationSelected);
        }

        private void SavedLocation_RightTapped(object sender, RightTappedRoutedEventArgs ev) {
            var listItem = (SlidableListItem)sender;
            var location = (LocationItem)listItem.DataContext;

            _lastLocationSelected = location;

            if (string.IsNullOrEmpty(location.Id)) return;

            var isPinned = TileDesigner.IsSecondaryTilePinned(location);

            if (isPinned) {
                CmdUnpinLocation.Visibility = Visibility.Visible;
                CmdPinLocation.Visibility = Visibility.Collapsed;

            } else {
                CmdUnpinLocation.Visibility = Visibility.Collapsed;
                CmdPinLocation.Visibility = Visibility.Visible;
            }

            SavedLocationRightTappedFlyout.ShowAt(listItem);
        }

        private async void SlidableListItem_LeftCommandRequested(object sender, EventArgs e) {
            var item = (SlidableListItem)sender;
            var location = (LocationItem)item.DataContext;

            var isPinned = TileDesigner.IsSecondaryTilePinned(location);

            if (isPinned) {
                var unpinSuccess = await UnpinLocationOnStart(location);

                if (unpinSuccess) {
                    item.LeftLabel = App.ResourceLoader.GetString("Pin");
                    item.LeftIcon = Symbol.Pin;

                    DataTransfer.ShowLocalToast(App.ResourceLoader.GetString("UnpinSuccess"));
                }

                return;
            }

            var pinSuccess = await PinLocationOnStart(location);

            if (pinSuccess) {
                item.LeftLabel = App.ResourceLoader.GetString("Unpin");
                item.LeftIcon = Symbol.UnPin;

                DataTransfer.ShowLocalToast(App.ResourceLoader.GetString("PinSuccess"));
            }
        }

        private void CmdPinLocation_Tapped(object sender, TappedRoutedEventArgs e) {
            var flyout = (MenuFlyoutItem)sender;
            var location = (LocationItem)flyout.DataContext;
            PinLocationOnStart(location);
        }

        private void CmdUnpinLocation_Tapped(object sender, TappedRoutedEventArgs e) {
            var flyout = (MenuFlyoutItem)sender;
            var location = (LocationItem)flyout.DataContext;
            UnpinLocationOnStart(location);
        }

        private async Task<bool> PinLocationOnStart(LocationItem location) {
            // 1.Ask for pin
            var locationId = TileDesigner.ConvertLocationNameToTileId(location.Name);
            var isPined = await TileDesigner.PinSecondaryTile(location);

            if (!isPined) { return false; }

            // 2.Register task config            
            await Settings.SaveSecondaryTaskLocation(locationId, location);

            // 3.Register task
            BackgroundTasks.RegisterSecondaryTileTask(locationId);

            // 4.Update the tile
            var forecast = await App.DataSource.GetCurrentForecast(location.Latitude, location.Longitude);
            TileDesigner.UpdateSecondary(locationId, forecast, location);

            return true;
        }

        private async Task<bool> UnpinLocationOnStart(LocationItem location) {
            // 1.Unpin
            var locationId = TileDesigner.ConvertLocationNameToTileId(location.Name);
            var isUnpinned = await TileDesigner.UnpinSecondaryTile(locationId);

            if (!isUnpinned) { return false; }

            // 2.Delete task config
            await Settings.DeleteSecondaryTaskLocation(locationId);

            // 3.Unregister task
            BackgroundTasks.UnregisterSecondaryTileTask(locationId);

            return true;
        }

        private void SavedLocation_Loaded(object sender, RoutedEventArgs e) {
            _delaySavedLocationList += 100;

            var item = (SlidableListItem)sender;

            item.Offset(0, 50, 0)
                .Fade(0, 0)
                .Then()
                .Offset(0)
                .Fade(1)
                .SetDelay(_delaySavedLocationList)
                .Start();

            var location = (LocationItem)item.DataContext;
            var isPinned = TileDesigner.IsSecondaryTilePinned(location);

            if (isPinned) {
                item.LeftLabel = App.ResourceLoader.GetString("Unpin");
                item.LeftIcon = Symbol.UnPin;
            }
        }
        #endregion events

        #region others methods

        private async void DeleteSavedLocation(LocationItem location) {
            if (location == null) return;

            if (string.IsNullOrEmpty(location.Id)) {
                return;
            }

            _savedLocations.Remove(location);
            Settings.SaveLocationsAsync(_savedLocations.ToList());

            if (location.IsSelected) {
                await Settings.DeleteFavoriteLocation(); // NOTE: wait ?
                Settings.SavePrimaryTileTaskType(Settings._GPSTaskTypeKey);
            }
        }

        private async Task<IReadOnlyList<MapLocation>> GetLocationFrom(string query) {
            var position = new BasicGeoposition {
                Latitude = 0,
                Longitude = 0
            };

            var referencePoint = new Geopoint(position);
            var results = await MapLocationFinder.FindLocationsAsync(query, referencePoint);

            if (results.Status == MapLocationFinderStatus.Success) {
                return results.Locations;
            }

            return null;
        }

        private void UnselectAnyLocation() {
            foreach (var item in SavedLocationsListView.Items) {
                var location = (LocationItem)item;
                location.IsSelected = false;
            }
        }

        private void RefreshSavedLocationsItems() {
            if (SavedLocationsListView == null || SavedLocationsListView.Items.Count == 0) {
                return;
            }

            foreach (LocationItem location in SavedLocationsListView.Items) {
                var isPinned = TileDesigner.IsSecondaryTilePinned(location);

                var item = (ListViewItem)SavedLocationsListView.ContainerFromItem(location);
                if (item == null) continue;

                var slidableListItem = (SlidableListItem)item.ContentTemplateRoot;

                if (isPinned) {
                    slidableListItem.LeftLabel = App.ResourceLoader.GetString("Unpin");
                    slidableListItem.LeftIcon = Symbol.UnPin;

                } else {
                    slidableListItem.LeftLabel = App.ResourceLoader.GetString("Pin");
                    slidableListItem.LeftIcon = Symbol.Pin;
                }
            }
        }

        #endregion others methods

    }
}
