using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AndroidX.AppCompat.App;
using Java.Lang;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace MapXamGeoCGeoLPer
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Button myButton;
        private Button myButtonL;
        private AlertDialog.Builder builder;
        private ISharedPreferences sharedpref;
        private ISharedPreferencesEditor editor;
        private TextView distanceloc;
        private TextView addressText;
        private TextView addresslastloc;
        CancellationTokenSource cts;

       
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            UIReference();
            UIClickevent();
            IntializedSharedPreferences();
        }

     
        private void IntializedSharedPreferences()
        {
            sharedpref = PreferenceManager.GetDefaultSharedPreferences(this);
            editor = sharedpref.Edit();
        }

        private void UIClickevent()
        {
            myButton.Click += MyButton_Click;
            myButtonL.Click += MyButtonL_Click;
        }

        private async void MyButtonL_Click(object sender, EventArgs e)
        {
            var lastknownlocation = await Geolocation.GetLastKnownLocationAsync();
            if (lastknownlocation != null)
            {
                addresslastloc.Text = $"Last known Location Latitude: "+ lastknownlocation.Latitude.ToString() + $"\n" +
                                      $"Last known Location Longitude:"+ lastknownlocation.Longitude.ToString() + $"\n" +
                                      $"Last known Location Altitude"+ lastknownlocation.Altitude.ToString();

            }

 
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15));
                cts = new CancellationTokenSource();
                var newlocation = await Geolocation.GetLocationAsync(request, cts.Token);

                var option = new MapLaunchOptions { NavigationMode = NavigationMode.Walking};
                if (newlocation != null)
                {

                    addressText.Text = $"New Location: " +newlocation.ToString();
                    
                }

                await Map.OpenAsync(newlocation, option);

            double miles = Location.CalculateDistance(lastknownlocation, newlocation, DistanceUnits.Miles);

            distanceloc.Text = $"Distance:" + miles.ToString();
            
        }

        private async void MyButton_Click(object sender, EventArgs e)
        {
            PermissionStatus permission = await CheckStatusOfPermission();

            if (permission == PermissionStatus.Granted)
            {
               // ShowToast("Location Permission is Granted");
                OpenLocationPlaceMark();
            }

            else
            {
                if (Permissions.ShouldShowRationale<LocationPermissions>())
                {
                    ShowRationaleDialog();
                }
                else if (sharedpref.GetBoolean(Manifest.Permission.AccessFineLocation, false))
                {

                    builder = new AlertDialog.Builder(this);
                    builder.SetTitle(Resource.String.location_permission_title);
                    builder.SetMessage(Resource.String.location_permission_message);
                    builder.SetPositiveButton(Resource.String.grant,

                        (s, e) =>
                        {
                            Intent intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                            Android.Net.Uri uri = Android.Net.Uri.FromParts(scheme: "package", PackageName, null);
                            intent.SetData(uri);
                            StartActivityForResult(intent, requestCode: 12);


                        });
                    builder.SetNegativeButton(Resource.String.cancel, (s, e) => { builder.Dispose(); });
                    builder.Show();


                }
                else
                {
                    PermissionStatus status = await RequestLocationPermissions();

                    if (status == PermissionStatus.Granted)
                    {
                        //ShowToast("Location Permission is Granted");
                        OpenLocationPlaceMark();


                    }


                    else 
                    
                    {
                        ShowToast("Permission is Denied");


                    }
                
                }

                editor.PutBoolean(Manifest.Permission.AccessFineLocation, true);
                editor.Commit();
            }
        }

        private async void OpenLocationPlaceMark()
        {
            var lat = 27.171064;
            var lon = 78.043571;
           
            var placemarks = await Geocoding.GetPlacemarksAsync(lat, lon);

            var placemark = placemarks?.FirstOrDefault();

            if (placemark != null)
            {
                var geocoderassress =

                $"SubThoroughfare: {placemark.SubThoroughfare}\n" +
                $"Thoroughfare:    {placemark.Thoroughfare}\n" +
                $"SubLocality:     {placemark.SubLocality}\n" +
                $"Locality:        {placemark.Locality}\n" +
                $"SubAdminArea:    {placemark.SubAdminArea}\n" +
                $"AdminArea:       {placemark.AdminArea}\n" +
                $"CountryName:     {placemark.CountryName}\n" +
                $"CountryCode:     {placemark.CountryCode}\n" +
                 $"PostalCode:      {placemark.PostalCode}\n" +
                $"FeatureName:     {placemark.FeatureName}\n";


                addressText.Text = geocoderassress.ToString();
              
            }


            var options = new MapLaunchOptions { NavigationMode = NavigationMode.Driving };

            try
            {

                await Map.OpenAsync(placemark, options);
            }
            catch(System.Exception ex)
            {

                ShowToast("Exception Found: "+ ex.Message);
            }


  
        }

        private void ShowToast(string v)
        {
            Toast.MakeText(this, v, ToastLength.Short).Show();
        }

        private async Task<PermissionStatus> RequestLocationPermissions()
        {
            return await Permissions.RequestAsync<LocationPermissions>();
        }

        private  void ShowRationaleDialog()
        {
            builder = new AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.location_permission_title);
            builder.SetMessage(Resource.String.location_permission_message);
            builder.SetPositiveButton(Resource.String.ok, async(s, e) => { await RequestLocationPermissions(); });
            builder.SetNegativeButton(Resource.String.cancel, (s, e) => { builder.Dispose(); });
            builder.Show();
        }

        private async Task<PermissionStatus> CheckStatusOfPermission()
        {
            return await Permissions.CheckStatusAsync<LocationPermissions>();
        }

        private void UIReference()
        {
            myButton = FindViewById<Button>(Resource.Id.button1);
            myButtonL = FindViewById<Button>(Resource.Id.button2);
            addressText = FindViewById<TextView>(Resource.Id.geoaddress);
            addresslastloc = FindViewById<TextView>(Resource.Id.lastlocation);
            distanceloc = FindViewById<TextView>(Resource.Id.distance);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
           
        }
    }
}