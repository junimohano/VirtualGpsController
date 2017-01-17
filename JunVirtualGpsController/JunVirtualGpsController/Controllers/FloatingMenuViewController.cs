using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using JunVirtualGpsController.Services;

namespace JunVirtualGpsController.Controllers
{
    public class FloatingMenuViewController : View, View.IOnTouchListener
    {
        private readonly FloatingWindowService _floatingWindow;
        private readonly View _floatingMenuView;

        private readonly WindowManagerLayoutParams _updatedPramaters;
        private int _x, _y;
        private float _touchedX, _touchedY;

        private readonly ImageButton _imageButtonHome;
        private readonly ImageButton _imageButtonSpeed;
        private readonly ImageButton _imageButtonAuto;
        private readonly ImageButton _imageButtonS;

        private readonly TextView _textViewHome;
        private readonly TextView _textViewSpeed;
        private readonly TextView _textViewAuto;

        public bool IsAutoTurnOn { get; set; } = false;

        private enum SpeedMode
        {
            Speed1 = 0,
            Speed2 = 1,
            Speed3 = 2,
            Speed4 = 3
        }

        private int _speedModeIndex = (int)SpeedMode.Speed1;

        private Timer _displayTimer;

        public FloatingMenuViewController(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public FloatingMenuViewController(Context context) : base(context)
        {
            _floatingWindow = (FloatingWindowService)context;
            var size = new Point();
            _floatingWindow.WindowManager.DefaultDisplay.GetSize(size);

            int tempX = -(size.X / 2);
            int tempY = -(size.Y / 30);

            // road preference
            _speedModeIndex = _floatingWindow.SharedPreferences.GetInt("ms", _speedModeIndex);
            tempX = _floatingWindow.SharedPreferences.GetInt("mx", tempX);
            tempY = _floatingWindow.SharedPreferences.GetInt("my", tempY);

            var vi = (LayoutInflater)_floatingWindow.GetSystemService(Context.LayoutInflaterService);
            _floatingMenuView = vi.Inflate(Resource.Layout.FloatingMenuView, null);
            var parameters = new WindowManagerLayoutParams(WindowManagerTypes.Phone, WindowManagerFlags.NotFocusable, Format.Translucent);

            parameters.X = tempX;
            parameters.Y = tempY;
            parameters.Height = WindowManagerLayoutParams.WrapContent;
            parameters.Width = WindowManagerLayoutParams.WrapContent;
            parameters.Gravity = GravityFlags.Center | GravityFlags.Center;
            _updatedPramaters = parameters;
            _floatingWindow.WindowManager.AddView(_floatingMenuView, parameters);

            _imageButtonHome = (ImageButton)_floatingMenuView.FindViewById(Resource.Id.ImageButtonHome);
            _imageButtonSpeed = (ImageButton)_floatingMenuView.FindViewById(Resource.Id.ImageButtonSpeed);
            _imageButtonAuto = (ImageButton)_floatingMenuView.FindViewById(Resource.Id.ImageButtonAuto);
            _imageButtonS = (ImageButton)_floatingMenuView.FindViewById(Resource.Id.imageButtonS);

            _textViewHome = (TextView)_floatingMenuView.FindViewById(Resource.Id.TextViewHome);
            _textViewSpeed = (TextView)_floatingMenuView.FindViewById(Resource.Id.TextViewSpeed);
            _textViewAuto = (TextView)_floatingMenuView.FindViewById(Resource.Id.TextViewAuto);

            _imageButtonHome.SetOnTouchListener(this);
            _imageButtonSpeed.SetOnTouchListener(this);
            _imageButtonAuto.SetOnTouchListener(this);
            _imageButtonS.SetOnTouchListener(this);


            ChangeSpeedMode();
            ChangeImageOfSpeedMode();

            ResetDisplayTimer();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            ISharedPreferencesEditor editor = _floatingWindow.SharedPreferences.Edit();
            editor.PutInt("ms", _speedModeIndex);
            editor.PutInt("mx", _updatedPramaters.X);
            editor.PutInt("my", _updatedPramaters.Y);
            editor.Apply();

            if (_displayTimer != null)
            {
                _displayTimer.Dispose();
                _displayTimer = null;
            }
        }

        private void ResetDisplayTimer()
        {
            if (_displayTimer == null)
                _displayTimer = new Timer(SetControlDisplay, null, 0, 7000);

            _displayTimer.Change(7000, 0);

            if (_imageButtonHome.Drawable.Alpha != 255)
            {
                _imageButtonHome.Drawable.SetAlpha(255);
                _imageButtonSpeed.Drawable.SetAlpha(255);
                _imageButtonAuto.Drawable.SetAlpha(255);
                _imageButtonS.Drawable.SetAlpha(255);

                _textViewHome.Alpha = 255;
                _textViewSpeed.Alpha = 255;
                _textViewAuto.Alpha = 255;
            }
        }

        private void SetControlDisplay(object state)
        {
            Application.SynchronizationContext.Post(_ =>
            {
                if (_imageButtonHome.Drawable.Alpha != 100)
                {
                    _imageButtonHome.Drawable.SetAlpha(100);
                    _imageButtonSpeed.Drawable.SetAlpha(100);
                    _imageButtonAuto.Drawable.SetAlpha(100);
                    _imageButtonS.Drawable.SetAlpha(100);

                    _textViewHome.Alpha = 100;
                    _textViewSpeed.Alpha = 100;
                    _textViewAuto.Alpha = 100;
                }
            }, null);
        }

        public void ChangeSpeedMode()
        {
            double speed = 0;

            switch (_speedModeIndex)
            {
                case (int)SpeedMode.Speed1:
                    speed = 0.000023;
                    break;
                case (int)SpeedMode.Speed2:
                    speed = 0.000050;
                    break;
                case (int)SpeedMode.Speed3:
                    speed = 0.000100;
                    break;
                case (int)SpeedMode.Speed4:
                    speed = 0.000150;
                    break;
            }

            _floatingWindow.MoveSpeed = speed;
        }

        private void ChangeImageOfSpeedMode()
        {
            switch (_speedModeIndex)
            {
                case (int)SpeedMode.Speed1:
                    _imageButtonSpeed.SetImageResource(Resource.Drawable.Speed1Button);
                    break;
                case (int)SpeedMode.Speed2:
                    _imageButtonSpeed.SetImageResource(Resource.Drawable.Speed2Button);
                    break;
                case (int)SpeedMode.Speed3:
                    _imageButtonSpeed.SetImageResource(Resource.Drawable.Speed3Button);
                    break;
                case (int)SpeedMode.Speed4:
                    _imageButtonSpeed.SetImageResource(Resource.Drawable.Speed4Button);
                    break;
            }
        }

        public FloatingMenuViewController(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public FloatingMenuViewController(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public FloatingMenuViewController(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    if (v == _imageButtonSpeed)
                    {
                        _speedModeIndex++;
                        if (_speedModeIndex > 3)
                            _speedModeIndex = 0;

                        ChangeSpeedMode();
                    }
                    else if (v == _imageButtonS)
                    {
                        _x = _updatedPramaters.X;
                        _y = _updatedPramaters.Y;
                        _touchedX = e.RawX;
                        _touchedY = e.RawY;
                    }

                    ResetDisplayTimer();
                    break;
                case MotionEventActions.Move:
                    if (v == _imageButtonS)
                    {
                        _updatedPramaters.X = (int)(_x + e.RawX - _touchedX);
                        _updatedPramaters.Y = (int)(_y + e.RawY - _touchedY);
                        _floatingWindow.WindowManager.UpdateViewLayout(_floatingMenuView, _updatedPramaters);
                    }
                    break;
                case MotionEventActions.Up:
                    if (v == _imageButtonHome)
                    {
                        var intent = new Intent(_floatingWindow, typeof(MenuLocationActivity));
                        intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                        var bundle = new Bundle();
                        bundle.PutParcelable("Location", _floatingWindow.Location);
                        bundle.PutParcelable("LocationReal", _floatingWindow.LocationReal);
                        intent.PutExtras(bundle);
                        _floatingWindow.StartActivity(intent);
                    }
                    else if (v == _imageButtonSpeed)
                    {
                        ChangeImageOfSpeedMode();
                    }
                    else if (v == _imageButtonAuto)
                    {
                        _imageButtonAuto.SetImageResource(IsAutoTurnOn ? Resource.Drawable.AutoOffButton : Resource.Drawable.AutoOnButton);
                        IsAutoTurnOn = !IsAutoTurnOn;

                        _floatingWindow.FloatingJoyStickViewController.GetFloatingJoyStickView().Visibility = IsAutoTurnOn ? ViewStates.Gone : ViewStates.Visible;
                        _floatingWindow.FloatingJoyStickViewController.ResetDisplayTimer();
                    }
                    ResetDisplayTimer();
                    break;
            }
            return false;
        }

        public View GetFloatingMenuView()
        {
            return _floatingMenuView;
        }
    }
}