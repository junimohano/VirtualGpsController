using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using JunVirtualGpsController.Services;
using Format = Android.Graphics.Format;

namespace JunVirtualGpsController.Controllers
{
    public class FloatingJoyStickViewController : View, View.IOnTouchListener
    {
        private readonly FloatingWindowService _floatingWindow;
        private readonly View _floatingControlView;

        private readonly WindowManagerLayoutParams _updatedPramaters;
        private int _x, _y;
        private float _touchedX, _touchedY;

        private readonly ImageButton _imageButtonQ;
        private readonly ImageButton _imageButtonW;
        private readonly ImageButton _imageButtonE;
        private readonly ImageButton _imageButtonA;
        private readonly ImageButton _imageButtonS;
        private readonly ImageButton _imageButtonD;
        private readonly ImageButton _imageButtonZ;
        private readonly ImageButton _imageButtonX;
        private readonly ImageButton _imageButtonC;

        private Timer _pressTimer;
        private Timer _displayTimer;

        public FloatingJoyStickViewController(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public FloatingJoyStickViewController(Context context) : base(context)
        {
            _floatingWindow = (FloatingWindowService)context;
            var size = new Point();
            _floatingWindow.WindowManager.DefaultDisplay.GetSize(size);

            int tempX = size.X / 2;
            int tempY = size.Y / 4;

            // road preference
            tempX = _floatingWindow.SharedPreferences.GetInt("jx", tempX);
            tempY = _floatingWindow.SharedPreferences.GetInt("jy", tempY);

            var vi = (LayoutInflater)_floatingWindow.GetSystemService(Context.LayoutInflaterService);
            _floatingControlView = vi.Inflate(Resource.Layout.FloatingJoyStickView, null);
            var parameters = new WindowManagerLayoutParams(WindowManagerTypes.Phone, WindowManagerFlags.NotFocusable, Format.Translucent);

            parameters.X = tempX;
            parameters.Y = tempY;
            parameters.Height = WindowManagerLayoutParams.WrapContent;
            parameters.Width = WindowManagerLayoutParams.WrapContent;
            parameters.Gravity = GravityFlags.Center | GravityFlags.Center;
            _updatedPramaters = parameters;
            _floatingWindow.WindowManager.AddView(_floatingControlView, parameters);

            _imageButtonQ = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonQ);
            _imageButtonW = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonW);
            _imageButtonE = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonE);
            _imageButtonA = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonA);
            _imageButtonS = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonS);
            _imageButtonD = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonD);
            _imageButtonZ = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonZ);
            _imageButtonX = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonX);
            _imageButtonC = (ImageButton)_floatingControlView.FindViewById(Resource.Id.imageButtonC);

            _imageButtonQ.SetOnTouchListener(this);
            _imageButtonW.SetOnTouchListener(this);
            _imageButtonE.SetOnTouchListener(this);
            _imageButtonA.SetOnTouchListener(this);
            _imageButtonS.SetOnTouchListener(this);
            _imageButtonD.SetOnTouchListener(this);
            _imageButtonZ.SetOnTouchListener(this);
            _imageButtonX.SetOnTouchListener(this);
            _imageButtonC.SetOnTouchListener(this);

            ResetDisplayTimer();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            ISharedPreferencesEditor editor = _floatingWindow.SharedPreferences.Edit();
            editor.PutInt("jx", _updatedPramaters.X);
            editor.PutInt("jy", _updatedPramaters.Y);
            editor.Apply();

            if (_pressTimer != null)
            {
                _pressTimer.Dispose();
                _pressTimer = null;
            }

            if (_displayTimer != null)
            {
                _displayTimer.Dispose();
                _displayTimer = null;
            }
        }

        public void ResetDisplayTimer()
        {
            if (_displayTimer == null)
                _displayTimer = new Timer(SetControlDisplay, null, 0, 7000);

            _displayTimer.Change(7000, 0);

            if (_imageButtonQ.Drawable.Alpha != 255)
            {
                _imageButtonQ.Drawable.SetAlpha(255);
                _imageButtonW.Drawable.SetAlpha(255);
                _imageButtonE.Drawable.SetAlpha(255);
                _imageButtonA.Drawable.SetAlpha(255);
                _imageButtonS.Drawable.SetAlpha(255);
                _imageButtonD.Drawable.SetAlpha(255);
                _imageButtonZ.Drawable.SetAlpha(255);
                _imageButtonX.Drawable.SetAlpha(255);
                _imageButtonC.Drawable.SetAlpha(255);
            }
        }

        private void SetControlDisplay(object state)
        {
            Application.SynchronizationContext.Post(_ =>
            {
                if (_imageButtonQ.Drawable.Alpha != 100)
                {
                    _imageButtonQ.Drawable.SetAlpha(100);
                    _imageButtonW.Drawable.SetAlpha(100);
                    _imageButtonE.Drawable.SetAlpha(100);
                    _imageButtonA.Drawable.SetAlpha(100);
                    _imageButtonS.Drawable.SetAlpha(100);
                    _imageButtonD.Drawable.SetAlpha(100);
                    _imageButtonZ.Drawable.SetAlpha(100);
                    _imageButtonX.Drawable.SetAlpha(100);
                    _imageButtonC.Drawable.SetAlpha(100);
                }
            }, null);
        }

        public FloatingJoyStickViewController(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public FloatingJoyStickViewController(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public FloatingJoyStickViewController(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    if (v == _imageButtonS)
                    {
                        _x = _updatedPramaters.X;
                        _y = _updatedPramaters.Y;
                        _touchedX = e.RawX;
                        _touchedY = e.RawY;
                    }
                    else
                    {
                        _pressTimer = new Timer(Callback, v, 0, 500);
                    }
                    ResetDisplayTimer();
                    break;
                case MotionEventActions.Move:
                    if (v == _imageButtonS)
                    {
                        _updatedPramaters.X = (int)(_x + e.RawX - _touchedX);
                        _updatedPramaters.Y = (int)(_y + e.RawY - _touchedY);
                        _floatingWindow.WindowManager.UpdateViewLayout(_floatingControlView, _updatedPramaters);
                    }
                    break;
                case MotionEventActions.Up:
                    if (v == _imageButtonS)
                    {
                        //
                    }
                    else
                    {
                        _pressTimer.Dispose();
                    }
                    ResetDisplayTimer();
                    break;
            }
            return false;
        }

        private void Callback(object state)
        {
            double increasedLatitude = 0;
            double increasedLongitude = 0;

            if (state == _imageButtonQ)
            {
                increasedLatitude = +_floatingWindow.MoveSpeed;
                increasedLongitude = -_floatingWindow.MoveSpeed;
            }
            else if (state == _imageButtonW)
            {
                increasedLatitude = +_floatingWindow.MoveSpeed;
            }
            else if (state == _imageButtonE)
            {
                increasedLatitude = +_floatingWindow.MoveSpeed;
                increasedLongitude = +_floatingWindow.MoveSpeed;
            }
            else if (state == _imageButtonA)
            {
                increasedLongitude = -_floatingWindow.MoveSpeed;
            }
            else if (state == _imageButtonD)
            {
                increasedLongitude = +_floatingWindow.MoveSpeed;
            }
            else if (state == _imageButtonZ)
            {
                increasedLatitude = -_floatingWindow.MoveSpeed;
                increasedLongitude = -_floatingWindow.MoveSpeed;
            }
            else if (state == _imageButtonX)
            {
                increasedLatitude = -_floatingWindow.MoveSpeed;
            }
            else if (state == _imageButtonC)
            {
                increasedLatitude = -_floatingWindow.MoveSpeed;
                increasedLongitude = +_floatingWindow.MoveSpeed;
            }

            _floatingWindow.SetLocation(_floatingWindow.Location.Latitude + increasedLatitude, _floatingWindow.Location.Longitude + increasedLongitude);
        }

        public View GetFloatingJoyStickView()
        {
            return _floatingControlView;
        }

    }
}