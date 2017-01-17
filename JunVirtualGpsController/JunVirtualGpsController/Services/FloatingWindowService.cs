using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Locations;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using JunVirtualGpsController.Controllers;

namespace JunVirtualGpsController.Services
{
    [Service]
    public class FloatingWindowService : Service, ILocationListener
    {
        public Location LocationReal { get; set; }
        public Location Location { get; set; }
        public LocationManager LocationManager { get; set; }
        public double MoveSpeed { get; set; }

        public IWindowManager WindowManager { get; set; }
        public FloatingMenuViewController FloatingMenuViewController { get; set; }
        public FloatingLocationViewController FloatingLocationViewController { get; set; }
        public FloatingJoyStickViewController FloatingJoyStickViewController { get; set; }


        public readonly string Tag = "X:" + typeof(FloatingWindowService).Name;
        private Timer _preventNotFoundGpsTimer;
        private int _preventNotFoundCount = 0;

        public ISharedPreferences SharedPreferences { get; set; }
        private NotificationManager _notificationManager;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (intent?.Extras != null)
            {
                // from menuLocation activity
                SetLocation(Convert.ToDouble(intent.GetStringExtra("latitude")), Convert.ToDouble(intent.GetStringExtra("longitude")));
                Toast.MakeText(this, Resources.GetString(Resource.String.LocationChangeSuccessMessage), ToastLength.Short).Show();
            }
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);
            return StartCommandResult.Sticky;
        }

        /**
        * Class for clients to access. Because we know this service always runs in
        * the same process as its clients, we don't need to deal with IPC.
        */
        public class FloatingWindowServiceBinder : Binder
        {
            private readonly FloatingWindowService _service;

            public FloatingWindowServiceBinder(FloatingWindowService service)
            {
                this._service = service;
            }

            public FloatingWindowService GetFloatingWindowService()
            {
                return _service;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            SaveSharedPreferences();

            _notificationManager?.CancelAll();

            _preventNotFoundGpsTimer?.Dispose();
            _preventNotFoundGpsTimer = null;

            WindowManager.RemoveView(FloatingJoyStickViewController.GetFloatingJoyStickView());
            WindowManager.RemoveView(FloatingLocationViewController.GetFloatingLocationView());
            WindowManager.RemoveView(FloatingMenuViewController.GetFloatingMenuView());

            FloatingJoyStickViewController?.Dispose();
            FloatingLocationViewController?.Dispose();
            FloatingMenuViewController?.Dispose();

            LocationManager?.RemoveUpdates(this);
            LocationManager?.RemoveTestProvider(LocationManager.NetworkProvider);

            Log.Debug(Tag, "FloatingWindow Service destroyed at {0}.", DateTime.UtcNow);
        }

        public void SaveSharedPreferences()
        {
            ISharedPreferencesEditor editor = SharedPreferences.Edit();
            if (Location != null)
            {
                editor.PutFloat("latitude", (float)Location.Latitude);
                editor.PutFloat("longitude", (float)Location.Longitude);
            }
            if (LocationReal != null)
            {
                editor.PutFloat("latitudeReal", (float)LocationReal.Latitude);
                editor.PutFloat("longitudeReal", (float)LocationReal.Longitude);
            }
            editor.Apply();
        }

        public override IBinder OnBind(Intent intent)
        {
            return new FloatingWindowServiceBinder(this);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            SharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);

            WindowManager = GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            FloatingMenuViewController = new FloatingMenuViewController(this);
            FloatingLocationViewController = new FloatingLocationViewController(this);
            FloatingJoyStickViewController = new FloatingJoyStickViewController(this);

            InitializeLocationManager();
        }


        private void InitializeLocationManager()
        {
            LocationManager = (LocationManager)GetSystemService(LocationService);

            var providers = LocationManager.GetProviders(true);
            foreach (var provider in providers)
            {
                var l = LocationManager.GetLastKnownLocation(provider);
                if (l == null)
                {
                    continue;
                }
                else if (Location == null || l.Accuracy < Location.Accuracy)
                {
                    // Found best last known location: %s", l);
                    Location = l;
                }
            }

            LocationManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this);
            //LocationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 0, 0, this);

            LocationManager.AddTestProvider(LocationManager.NetworkProvider, true, false, false, false, false, false, false, Power.High, SensorStatus.AccuracyHigh);
            LocationManager.SetTestProviderEnabled(LocationManager.NetworkProvider, true);
            //LocationManager.SetTestProviderStatus(LocationManager.NetworkProvider, Availability.OutOfService, null, DateTime.Now.Ticks);

            if (SharedPreferences.Contains("latitude"))
            {
                SetLocation(SharedPreferences.GetFloat("latitude", (float)Convert.ToDouble(Resources.GetString(Resource.String.DefaultLatitude))),
                    SharedPreferences.GetFloat("longitude", (float)Convert.ToDouble(Resources.GetString(Resource.String.DefaultLongitude))));
            }
            else
            {
                if (Location == null)
                {
                    // Sanfrancisco
                    SetLocation(Convert.ToDouble(Resources.GetString(Resource.String.DefaultLatitude)),
                        Convert.ToDouble(Resources.GetString(Resource.String.DefaultLongitude)));
                }
                else
                {
                    SetLocation(Location.Latitude, Location.Longitude);
                    LocationReal = Location;
                }
            }

            _preventNotFoundGpsTimer = new Timer(delegate (object state)
            {
                if (Location != null)
                {
                    _preventNotFoundCount++;

                    if (FloatingMenuViewController.IsAutoTurnOn)
                    {
                        double increasedLatitude = 0;
                        double increasedLongitude = 0;
                        switch (DateTime.Now.Second / 15)
                        {
                            case 0:
                                increasedLatitude += MoveSpeed;
                                break;
                            case 1:
                                increasedLatitude -= MoveSpeed;
                                break;
                            case 2:
                                increasedLongitude += MoveSpeed;
                                break;
                            case 3:
                                increasedLongitude -= MoveSpeed;
                                break;
                        }
                        SetLocation(Location.Latitude + increasedLatitude, Location.Longitude + increasedLongitude);
                    }

                    if (_preventNotFoundCount >= 9)
                        SetLocation(Location.Latitude, Location.Longitude);
                }
            }, null, 0, 500);

            // notification
            OpenNotify();
        }

        public void SetLocation(double latitude, double longitude)
        {
            Location = new Location(string.Empty)
            {
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = 250,
                Speed = 0.0f,
                Bearing = 0.0f,
                //Bearing = FindAngle(Location.Latitude, Location.Longitude, latitude, longitude),
                Time = DateTime.Now.Ticks,
                Provider = LocationManager.GpsProvider,
                ElapsedRealtimeNanos = SystemClock.ElapsedRealtimeNanos()
            };

            LocationManager?.SetTestProviderLocation(LocationManager.NetworkProvider, Location);

            Application.SynchronizationContext.Post(_ =>
            {
                FloatingLocationViewController.SetLocation(Location.Latitude, Location.Longitude);
            }, null);

            _preventNotFoundCount = 0;
        }

        public void OnLocationChanged(Location location)
        {
            if (location.Extras != null)
            {
                SetLocation(Location.Latitude, Location.Longitude);
                LocationReal = location;
            }

            Log.Debug(Tag, location.ToString());
        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
        }

        //protected virtual float FindAngle(double x1, double y1, double x2, double y2)
        //{
        //    var dx = x2 - x1;
        //    var dy = y2 - y1;
        //    return (float)(Math.Atan2(dy, dx) * (180 / Math.PI));
        //}

        private void OpenNotify()
        {
            var builder = new NotificationCompat.Builder(this)
            .SetContentTitle(Resources.GetString(Resource.String.ApplicationName))
            .SetSmallIcon(Resource.Drawable.Icon)
            .SetOngoing(true)
            .SetContentIntent(PendingIntent.GetActivity(this, 0, new Intent(this, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent))
            .SetContentText(Resources.GetString(Resource.String.NotificationMessage));

            // Obtain a reference to the NotificationManager
            _notificationManager = (NotificationManager)GetSystemService(NotificationService);
            _notificationManager.Notify(0, builder.Build());
        }
    }
}
