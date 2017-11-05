# Feels
Minimalistic weather app

## screenshot
<img src="./home.png" height="400" alt="cloudy weather animation" style="display: inline-block;"/>

<img src="./hourly.png"  height="400" alt="hourly weather" style="display: inline-block;" />

<img src="./feels_tile.gif" height="200" />

## features

* Current weather based on geolocalization
* Add city manually
* Hourly forecast
* Daily forecast
* Live tile
* Pin multiple locations to start view
* Lockscreen text status

## setup
Steps to build and run this project:

1. Clone or download this repository
2. (Optional) Unzip the archive to your favorite location
3. Navigate to the ```Feels/``` folder
4. Open ```Feels.sln``` in [Visual Studio](https://www.visualstudio.com/thank-you-downloading-visual-studio/?sku=Community&rel=15)
5. Choose your favorite platform and click on Run :)


## architecture overview

This section describes the way I've organized my files and directories
to build this app in the clearest way possible.

**Views**

All the views are localized inside the ```Views/``` folder, except for the ```App.xaml``` and ```App.xaml.cs``` which is the main app's view page.

**Data**

All data are managed inside the ```Data/``` folder.

For more information, visit the corresponding folders.

## contributing

You can contribute to improve this project by:

* edit the code
* creating a pull request
* submitting new ideas / features suggestions
* reporting a bug

## todo

* Lockscreen background

## platforms

* Windows Mobile 10
* Windows 10


## Get a personal API key

To run this project, it's better to get your personal API key from Unsplash:

1. Login or Register a new account on [DarkSky](https://darksky.net/dev/login?next=/account)
2. On the account page, you'll get your Secret Key
3. Copy and paste the key when you create a new client:

```csharp
var client = new DarkSkyService("YOUR API KEY HERE");
```