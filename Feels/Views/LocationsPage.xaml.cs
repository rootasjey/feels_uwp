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

namespace Feels.Views {
    public sealed partial class LocationsPage : Page {
        #region variables

        private ObservableCollection<LocationItem> _savedLocations { get; set; }

        private ObservableCollection<MapLocation> _foundLocations { get; set; }

        private static LocationItem _lastLocationSelected { get; set; }

        private string _lastLocationQuery { get; set; }

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

        public LocationsPage() {
            InitializeComponent();
            InitializeVariables();
            InitializePageAnimation();
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
            SavedLocationRightTappedFlyout.ShowAt(listItem);
        }

        #endregion events

        #region others methods

        private void DeleteSavedLocation(LocationItem location) {
            if (location == null) return;

            if (string.IsNullOrEmpty(location.Id)) {
                return;
            }

            if (location.IsSelected) {
                Settings.DeleteFavoriteLocation(); // NOTE: wait ?
            }

            _savedLocations.Remove(location);
            Settings.SaveLocationsAsync(_savedLocations.ToList());
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

        #endregion others methods
    }
}
