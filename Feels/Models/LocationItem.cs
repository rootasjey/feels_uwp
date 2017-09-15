using System.ComponentModel;

namespace Feels.Models {
    public class LocationItem : INotifyPropertyChanged {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string Town { get; set; }

        private float _CurrentTemperature;

        public float CurrentTemperature {
            get { return _CurrentTemperature; }
            set {
                if (_CurrentTemperature != value) {
                    _CurrentTemperature = value;
                    NotifyPropertyChanged(nameof(CurrentTemperature));
                }
            }
        }

        private float _HighestTemperature;

        public float HighestTemperature {
            get { return _HighestTemperature; }
            set {
                if (_HighestTemperature != value) {
                    _HighestTemperature = value;
                    NotifyPropertyChanged(nameof(HighestTemperature));
                }
            }
        }

        private float _LowestTemperature;

        public float LowestTemperature {
            get { return _LowestTemperature; }
            set {
                if (_LowestTemperature != value) {
                    _LowestTemperature = value;
                    NotifyPropertyChanged(nameof(LowestTemperature));
                }

            }
        }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        private bool _IsSelected;

        public bool IsSelected {
            get { return _IsSelected; }
            set {
                if (_IsSelected != value) {
                    _IsSelected = value;
                    NotifyPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
