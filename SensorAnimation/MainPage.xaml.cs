namespace SensorAnimation;

public partial class MainPage : ContentPage
{

    private double _xAccell;
    private double _yAccell;
    private double _zAccell;

    PeriodicTimer _clock;

    private double _pitch;
    private double _roll;

    private bool _isAnimating;

    private DisplayOrientation _currentOrientation;

    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);


    public MainPage()
    {
        InitializeComponent();

        ToggleAccelerometer();
        _currentOrientation = DeviceDisplay.MainDisplayInfo.Orientation;
        DeviceDisplay.MainDisplayInfoChanged += DeviceDisplayOnMainDisplayInfoChanged;
    }

    private void DeviceDisplayOnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        if (_currentOrientation == e.DisplayInfo.Orientation) return;

        _currentOrientation = e.DisplayInfo.Orientation;
        // Animate between landscape and portrait
        switch (_currentOrientation)
        {
            case DisplayOrientation.Landscape:
                _isAnimating = true;
                break;
            case DisplayOrientation.Portrait:
                _isAnimating = false;
                break;
        }
    }

    private void ToggleAccelerometer()
    {
        if (Accelerometer.Default.IsSupported)
        {
            if (!Accelerometer.Default.IsMonitoring)
            {
                // Turn on accelerometer
                Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);
            }
            else
            {
                // Turn off accelerometer
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
            }
        }
    }


    private async void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
    {
        // use semaphore to prevent multiple threads from accessing the same resource
        if (_isAnimating) return;
        await _semaphore.WaitAsync();
        try
        {
            _xAccell = e.Reading.Acceleration.X;
            _yAccell = e.Reading.Acceleration.Y;
            _zAccell = e.Reading.Acceleration.Z;
            //Calculate the tilt angle (pitch or roll) based on the accelerometer readings.
            _pitch = Math.Atan2(_xAccell, Math.Sqrt(_yAccell * _yAccell + _zAccell * _zAccell));
            _roll = Math.Atan2(_yAccell, Math.Sqrt(_xAccell * _xAccell + _zAccell * _zAccell));

        }
        finally
        {
            _semaphore.Release();
        }


    }
    private double _lastAngle;
    private async void AnimatePlumbLine()
    {

        if (_clock == null)
        {
            _clock = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        }

        base.OnAppearing();

        while (await _clock.WaitForNextTickAsync())
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_currentOrientation == DisplayOrientation.Portrait)
                {
                    var angleInDegrees = _pitch * 180 / Math.PI;
                    if (Math.Abs(angleInDegrees - _lastAngle) > 1)
                    {
                        _lastAngle = angleInDegrees;
                        AngleShiftLabel.Text = $"Angle: {-angleInDegrees:F2}°";
                        await PlumbLine.RotateTo(angleInDegrees, 100, Easing.CubicInOut);
                    }
                }
                else
                {
                    //Do the bubble thing
                    //await PlumbLine.RotateTo(_pitch * 180 / Math.PI, 50, Easing.SpringOut);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }                // Rotate the plumb line based on the pitch and roll angles


    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AnimatePlumbLine();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ToggleAccelerometer();

        DeviceDisplay.MainDisplayInfoChanged -= DeviceDisplayOnMainDisplayInfoChanged;

    }
}