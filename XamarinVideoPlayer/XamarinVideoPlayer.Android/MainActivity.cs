using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.Views;
using Android.OS;
using LibVLCSharp.Forms.Shared;
using Plugin.CurrentActivity;
using Plugin.Permissions;

namespace XamarinVideoPlayer.Droid
{
    [Activity(Label = "VlcXamarin", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    [IntentFilter(new[] { Intent.ActionView },
            DataScheme = "rtsp", DataHost = "*",
            Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
            DataMimeTypes = new string[] { "video/x-matroska", "video/mp4" },
            Icon = "@mipmap/icon")]
    [IntentFilter(new[] { Intent.ActionView }, DataMimeTypes = new string[] { "video/x-matroska", "video/mp4" }, Categories = new[] { Intent.CategoryDefault }, Icon = "@mipmap/icon")]
    [IntentFilter(new[] { Intent.ActionView }, DataScheme = "http", DataMimeTypes = new string[] { "video/x-matroska", "video/mp4" }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, Icon = "@mipmap/icon")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private const int VideoSelectionCode = 200;
        private int _filePointer;

        protected override async void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            this.Window.AddFlags(WindowManagerFlags.Fullscreen); //to show
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            base.OnCreate(bundle);

            CrossCurrentActivity.Current.Init(this, bundle);
            MobileAds.Initialize(ApplicationContext, "ca-app-pub-5565829267699786~5143529599");
            Xamarin.Essentials.Platform.Init(this, bundle);
            LibVLCSharpFormsRenderer.Init();
            global::Xamarin.Forms.Forms.Init(this, bundle);

            var fileDetails = await GetFileStreamAndFileType(Intent);

            //if (string.IsNullOrWhiteSpace(fileDetails.Item2))
            //{
            //	LoadApplication(new App());
            //}

            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Storage);
            if (status == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {
                if (!string.IsNullOrWhiteSpace(fileDetails.Item2))
                {
                    LoadApplication(new App(_filePointer));
                }
            }
            else if (_filePointer > 0)
            {
                this.Finish();
            }
        }

        //private bool IsPermissionGranted()
        //{
        //	const string permission = Manifest.Permission.ReadExternalStorage;
        //	if (CheckSelfPermission(permission) == (int)Permission.Granted)
        //	{
        //		return true;
        //	}

        //	RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage.ToString() }, 300);
        //	return false;
        //}

        private async Task RequestPermissions()
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Storage);
                if (status != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Plugin.Permissions.Abstractions.Permission.Storage))
                    {
                        //await DisplayAlert("Need location", "Gunna need that location", "OK");
                    }

                    var results = await CrossPermissions.Current.RequestPermissionsAsync(Plugin.Permissions.Abstractions.Permission.Storage);
                    status = results[Plugin.Permissions.Abstractions.Permission.Storage];
                }

                if (status == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    //var results = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(10));
                    //LabelGeolocation.Text = "Lat: " + results.Latitude + " Long: " + results.Longitude;
                }
                else if (status != Plugin.Permissions.Abstractions.PermissionStatus.Unknown)
                {
                    //await DisplayAlert("Location Denied", "Can not continue, try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                //LabelGeolocation.Text = "Error: " + ex;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            //Finish();
            //StartActivity(Intent);
            //if (_filePointer > 0)
            //{
            //	LoadApplication(new App(_filePointer));
            //}
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (resultCode == Result.Ok && data.Data != null)
            {
                var fileDetails = await GetFileStreamAndFileType(data);

                //await RequestPermissions();

                //IsPermissionGranted();
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Storage);
                if (status == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    if (!string.IsNullOrWhiteSpace(fileDetails.Item2))
                    {
                        LoadApplication(new App(_filePointer));
                    }
                }
                else if (_filePointer > 0)
                {
                    this.Finish();
                }
            }
        }

        private async Task<Tuple<int, string>> GetFileStreamAndFileType(Intent intentData)
        {
            int fd = 0;
            string fileType = null;
            if (!string.IsNullOrWhiteSpace(intentData.DataString))
            {
                await RequestPermissions();
                fileType = ".mkv";

                using (var inputStreamInvoker = ContentResolver.OpenFileDescriptor(intentData.Data, "r"))
                {
                    fd = inputStreamInvoker.Fd;
                    _filePointer = fd;
                }
            }
            else
            {
                var intent = new Intent();
                intent.SetType("video/mp4");
                intent.SetAction(Intent.ActionGetContent);
                this.StartActivityForResult(Intent.CreateChooser(intent, "Select video"), VideoSelectionCode);
            }

            return new Tuple<int, string>(fd, fileType);
        }
    }
}
