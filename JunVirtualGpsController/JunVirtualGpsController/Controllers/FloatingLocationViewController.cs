using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using JunVirtualGpsController.Services;

namespace JunVirtualGpsController.Controllers
{
    public class FloatingLocationViewController : View
    {
        private readonly FloatingWindowService _floatingWindow;
        private readonly View _floatingLocationView;

        private readonly TextView _textViewLatitude;
        private readonly TextView _textViewLongitude;

        public FloatingLocationViewController(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public FloatingLocationViewController(Context context) : base(context)
        {
            _floatingWindow = (FloatingWindowService)context;
            var vi = (LayoutInflater)_floatingWindow.GetSystemService(Context.LayoutInflaterService);

            _floatingLocationView = vi.Inflate(Resource.Layout.FloatingLocationView, null);
            var parameters = new WindowManagerLayoutParams(WindowManagerTypes.Phone, WindowManagerFlags.NotFocusable, Format.Translucent);
            var size = new Point();
            _floatingWindow.WindowManager.DefaultDisplay.GetSize(size);
            parameters.X += size.X / 2;
            parameters.Y += size.Y / 2;
            parameters.Height = WindowManagerLayoutParams.WrapContent;
            parameters.Width = WindowManagerLayoutParams.WrapContent;
            parameters.Gravity = GravityFlags.Center | GravityFlags.Center;
            _floatingWindow.WindowManager.AddView(_floatingLocationView, parameters);

            _textViewLatitude = (TextView)_floatingLocationView.FindViewById(Resource.Id.TextViewLatitude);
            _textViewLongitude = (TextView)_floatingLocationView.FindViewById(Resource.Id.TextViewLongitude);
        }

        public FloatingLocationViewController(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public FloatingLocationViewController(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public FloatingLocationViewController(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        public void SetLocation(double latitude, double longitude)
        {
            _textViewLatitude.Text = Resources.GetString(Resource.String.Latitude) + " : " + latitude.ToString("F6");
            _textViewLongitude.Text = Resources.GetString(Resource.String.Longitude) + " : " + longitude.ToString("F6");
        }

        public View GetFloatingLocationView()
        {
            return _floatingLocationView;
        }

    }
}