using System.Collections.ObjectModel;

namespace Feels.Models {
    public class Weather {
        private Location _Location;

        public Location Location {
            get { return _Location; }
            set { _Location = value; }
        }

        private Observation _Current;

        public Observation Current {
            get { return _Current; }
            set { _Current = value; }
        }

        public ObservableCollection<Observation> Hourly { get; set; }
        public ObservableCollection<Observation> Daily { get; set; }
    }
}
