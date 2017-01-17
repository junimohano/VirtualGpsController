using System;
using Android.App;
using Android.Content;
using Android.Gms.Ads;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Provider;
using JunVirtualGpsController.Services;

namespace JunVirtualGpsController
{
    [Activity(Label = "Virtual Gps Controller", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, View.IOnClickListener, IServiceConnection
    {
        protected AdView AdView;
        protected InterstitialAd InterstitialAd;
        private ImageButton _imageButtonStart;
        private ImageButton _imageButtonStop;
        private ImageButton _imageButtonDeveloper;

        private FloatingWindowService _floatingWindowService = null;
        private bool _isStart = true;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            AdView = FindViewById<AdView>(Resource.Id.AdView);
            var adRequest = new AdRequest.Builder().Build();
            AdView.LoadAd(adRequest);
            InterstitialAd = new InterstitialAd(this);
            InterstitialAd.AdUnitId = GetString(Resource.String.InterstitialAdUnitId);
            InterstitialAd.AdListener = new AdListener(this);

            _imageButtonStart = FindViewById<ImageButton>(Resource.Id.ImageButtonStart);
            _imageButtonStop = FindViewById<ImageButton>(Resource.Id.ImageButtonStop);
            _imageButtonDeveloper = FindViewById<ImageButton>(Resource.Id.ImageButtonDeveloper);

            _imageButtonStart.SetOnClickListener(this);
            _imageButtonStop.SetOnClickListener(this);
            _imageButtonDeveloper.SetOnClickListener(this);
        }

        protected void RequestNewInterstitial()
        {
            var adRequest = new AdRequest.Builder().Build();
            InterstitialAd.LoadAd(adRequest);
        }

        protected void ShowRun()
        {
            if (_isStart)
            {
                StartService(new Intent(BaseContext, typeof(FloatingWindowService)));
                BindService(new Intent(BaseContext, typeof(FloatingWindowService)), this, Bind.None);
            }
            else
            {
                StopService(new Intent(BaseContext, typeof(FloatingWindowService)));
            }
        }

        protected override void OnPause()
        {
            AdView?.Pause();
            base.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            AdView?.Resume();
            if (!InterstitialAd.IsLoaded)
            {
                RequestNewInterstitial();
            }
        }

        protected override void OnDestroy()
        {
            AdView?.Destroy();
            _floatingWindowService?.SaveSharedPreferences();
            base.OnDestroy();
        }

        private class AdListener : Android.Gms.Ads.AdListener
        {
            readonly MainActivity that;

            public AdListener(MainActivity t)
            {
                that = t;
            }

            public override void OnAdClosed()
            {
                that.RequestNewInterstitial();
                that.ShowRun();
            }
        }

        public void OnClick(View v)
        {
            if (v == _imageButtonStart)
            {
                if (IsAllowMockLocation())
                {
                    _isStart = true;
                    if (InterstitialAd.IsLoaded)
                        InterstitialAd.Show();
                    else
                        ShowRun();
                }
            }
            else if (v == _imageButtonStop)
            {
                if (IsAllowMockLocation())
                {
                    _isStart = false;

                    if (InterstitialAd.IsLoaded)
                        InterstitialAd.Show();
                    else
                        ShowRun();
                }
            }
            else if (v == _imageButtonDeveloper)
            {
                StartActivity(new Intent(Settings.ActionApplicationDevelopmentSettings));
            }
        }

        private bool IsAllowMockLocation()
        {
            var isAllowMockLocation = true;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                try
                {
                    if (((AppOpsManager)GetSystemService("appops")).CheckOp("android:mock_location", Process.MyUid(),
                            PackageName) != 0)
                    {

                    }
                }
                catch (Exception)
                {
                    isAllowMockLocation = false;
                }
            }
            else
            {
                bool allowMocks = Settings.Secure.GetInt(this.ContentResolver, Settings.Secure.AllowMockLocation, 0) == 1;
                if (!allowMocks)
                {
                    if (this.CheckCallingPermission("android.Manifest.permission.ACCESS_MOCK_LOCATION") != 0)
                    {
                        isAllowMockLocation = false;
                    }
                }
            }

            if (isAllowMockLocation == false)
            {
                Toast.MakeText(this, Resources.GetString(Resource.String.DialogIsAllowMockLocationTitle), ToastLength.Long).Show();

                var alertDialog = new AlertDialog.Builder(this)
                    .SetTitle(Resources.GetString(Resource.String.DialogIsAllowMockLocationTitle))
                    .SetMessage(Resources.GetString(Resource.String.DialogIsAllowMockLocationMessage))
                    .Create();

                alertDialog.Window.SetType(WindowManagerTypes.SystemAlert);
                alertDialog.SetButton(Resources.GetString(Resource.String.DialogCancel), delegate (object sender, DialogClickEventArgs args)
                {
                    alertDialog.Cancel();
                });
                alertDialog.SetButton2(Resources.GetString(Resource.String.DialogOk), delegate (object sender, DialogClickEventArgs args)
                {
                    StartActivity(new Intent(Settings.ActionApplicationDevelopmentSettings));
                    alertDialog.Cancel();
                });

                alertDialog.Show();
            }

            return isAllowMockLocation;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            var floatingWindowServiceBinder = service as FloatingWindowService.FloatingWindowServiceBinder;
            if (floatingWindowServiceBinder != null)
            {
                _floatingWindowService = floatingWindowServiceBinder.GetFloatingWindowService();
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            //_floatingWindowService?.Dispose();
        }
    }

}

