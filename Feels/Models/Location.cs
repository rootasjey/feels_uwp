namespace Feels.Models {
    public class Location {
        private string _City;

        public string City {
            get { return _City; }
            set { _City = value; }
        }

        private string _StateCode;

        public string StateCode {
            get { return _StateCode; }
            set { _StateCode = value; }
        }

        private string _CountryCode;

        public string CountryCode {
            get { return _CountryCode; }
            set { _CountryCode = value; }
        }

        private string _TimeZone;

        public string TimeZone {
            get { return _TimeZone; }
            set { _TimeZone = value; }
        }


        private string _Latitude;

        public string Latitude {
            get { return _Latitude; }
            set { _Latitude = value; }
        }

        private string _Longitude;

        public string Longitude {
            get { return _Longitude; }
            set { _Longitude = value; }
        }
    }
}
