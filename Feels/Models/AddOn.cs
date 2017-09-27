using System.ComponentModel;

namespace Feels.Models {
    public class AddOn : INotifyPropertyChanged {
        public string StoreID { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public float Price { get; set; }

        public string ImageURI { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
