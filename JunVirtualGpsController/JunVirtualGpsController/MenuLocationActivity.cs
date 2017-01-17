using System;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Locations;
using Android.OS;
using Android.Preferences;
using Android.Text;
using Android.Views;
using Android.Widget;
using JunVirtualGpsController.Services;

namespace JunVirtualGpsController
{
    [Activity]
    public class MenuLocationActivity : Activity, IOnMapReadyCallback, View.IOnClickListener
    {
        private GoogleMap _googleMap;
        private Button _buttonReal;
        private Button _buttonDefault;
        private Button _buttonOk;
        private Button _buttonCancel;
        private EditText _editTextLatitude;
        private EditText _editTextLongitude;

        private Location _location;
        private Location _locationReal;
        private Toast _toast;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MenuLocation);

            InitializeLocation();
        }

        protected void InitializeLocation()
        {
            _toast = Toast.MakeText(this, "null", ToastLength.Short);

            // from service
            _location = (Location)Intent.GetParcelableExtra("Location");
            _locationReal = (Location)Intent.GetParcelableExtra("LocationReal");

            if (_googleMap == null)
                FragmentManager.FindFragmentById<MapFragment>(Resource.Id.map).GetMapAsync(this);

            _editTextLatitude = (EditText)FindViewById(Resource.Id.EditTextLatitude);
            _editTextLongitude = (EditText)FindViewById(Resource.Id.EditTextLongitude);
            _buttonReal = (Button)FindViewById(Resource.Id.ButtonReal);
            _buttonDefault = (Button)FindViewById(Resource.Id.ButtonDefault);
            _buttonOk = (Button)FindViewById(Resource.Id.ButtonOk);
            _buttonCancel = (Button)FindViewById(Resource.Id.ButtonCancel);

            _buttonReal.SetOnClickListener(this);
            _buttonDefault.SetOnClickListener(this);
            _buttonOk.SetOnClickListener(this);
            _buttonCancel.SetOnClickListener(this);

            _editTextLatitude.Enabled = false;
            _editTextLongitude.Enabled = false;
            _buttonReal.Enabled = false;
            _buttonDefault.Enabled = false;
            _buttonOk.Enabled = false;

            _editTextLatitude.AfterTextChanged += EditTextOnAfterTextChanged;
            _editTextLongitude.AfterTextChanged += EditTextOnAfterTextChanged;
        }

        private void EditTextOnAfterTextChanged(object sender, AfterTextChangedEventArgs afterTextChangedEventArgs)
        {
            RefreshMap(_editTextLatitude.Text, _editTextLongitude.Text);
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            _googleMap = googleMap;
            _googleMap.MarkerDragEnd += GoogleMapOnMarkerDragEnd;

            _editTextLatitude.Enabled = true;
            _editTextLongitude.Enabled = true;
            _buttonReal.Enabled = true;
            _buttonDefault.Enabled = true;
            _buttonOk.Enabled = true;

            _editTextLatitude.Text = _location?.Latitude.ToString("F6");
            _editTextLongitude.Text = _location?.Longitude.ToString("F6");

            RefreshMap(_editTextLatitude.Text, _editTextLongitude.Text);
        }

        public void RefreshMap(string latitude, string longitude)
        {
            _googleMap.Clear();

            double latitudeDouble;
            double longitudeDouble;

            if (double.TryParse(latitude, out latitudeDouble) && double.TryParse(longitude, out longitudeDouble))
            {
                var latlng = new LatLng(Convert.ToDouble(_editTextLatitude.Text), Convert.ToDouble(_editTextLongitude.Text));
                var camera = CameraUpdateFactory.NewLatLngZoom(latlng, 10);
                _googleMap.MoveCamera(camera);

                var options = new MarkerOptions()
                    .SetPosition(latlng)
                    .SetTitle("Location")
                    .SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueBlue))
                    .SetSnippet("Here")
                    .Draggable(true);

                _googleMap.AddMarker(options);
            }
        }

        private void GoogleMapOnMarkerDragEnd(object sender, GoogleMap.MarkerDragEndEventArgs markerDragEndEventArgs)
        {
            var pos = markerDragEndEventArgs.Marker.Position;
            _editTextLatitude.Text = pos.Latitude.ToString("F6");
            _editTextLongitude.Text = pos.Longitude.ToString("F6");

            RefreshMap(pos.Latitude.ToString("F6"), pos.Longitude.ToString("F6"));
        }

        public void OnClick(View v)
        {
            if (v == _buttonReal)
            {
                if (_locationReal != null)
                {
                    _editTextLatitude.Text = _locationReal.Latitude.ToString("F6");
                    _editTextLongitude.Text = _locationReal.Longitude.ToString("F6");
                }
                else
                {
                    var sharedPreferences = PreferenceManager.GetDefaultSharedPreferences(this);
                    _editTextLatitude.Text = sharedPreferences.GetFloat("latitudeReal", (float)_location?.Latitude).ToString("F6");
                    _editTextLongitude.Text = sharedPreferences.GetFloat("longitudeReal", (float)_location?.Longitude).ToString("F6");
                }

                RefreshMap(_editTextLatitude.Text, _editTextLongitude.Text);

                _toast.SetText(Resources.GetString(Resource.String.LocationRealMessage));
                _toast.Show();
            }
            else if (v == _buttonDefault)
            {
                // Sanfrancisco
                _editTextLatitude.Text = Resources.GetString(Resource.String.DefaultLatitude);
                _editTextLongitude.Text = Resources.GetString(Resource.String.DefaultLongitude);
                RefreshMap(_editTextLatitude.Text, _editTextLongitude.Text);

                _toast.SetText(Resources.GetString(Resource.String.LocationDefaultMessage));
                _toast.Show();
            }
            else if (v == _buttonOk)
            {
                if (!string.IsNullOrEmpty(_editTextLatitude.Text) && !string.IsNullOrEmpty(_editTextLongitude.Text))
                {
                    var alertDialog = new AlertDialog.Builder(this)
                        .SetTitle(Resources.GetString(Resource.String.DialogChangeLocationTitle))
                        .SetMessage(Resources.GetString(Resource.String.DialogChangeLocationWarningMessage))
                        .Create();

                    alertDialog.SetButton2(Resources.GetString(Resource.String.DialogOk), delegate (object sender, DialogClickEventArgs args)
                    {
                        var intent = new Intent(this, typeof(FloatingWindowService));
                        var b = new Bundle();
                        // to service
                        b.PutString("latitude", _editTextLatitude.Text);
                        b.PutString("longitude", _editTextLongitude.Text);
                        intent.PutExtras(b);
                        StartService(intent);
                        alertDialog.Cancel();
                        Finish();
                    });

                    alertDialog.Window.SetType(WindowManagerTypes.SystemAlert);
                    alertDialog.SetButton(Resources.GetString(Resource.String.DialogCancel), delegate (object sender, DialogClickEventArgs args)
                    {
                        alertDialog.Cancel();
                    });

                    alertDialog.Show();
                }
            }
            else if (v == _buttonCancel)
            {
                Finish();
            }

        }
    }
}