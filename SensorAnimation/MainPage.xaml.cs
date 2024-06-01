namespace SensorAnimation;

public partial class MainPage : ContentPage
{


    PeriodicTimer _clock;

    private double _pitch;
    private double _roll;

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
                // Fade out the plumb line
                PlumbLine.FadeTo(0, 300, Easing.CubicInOut);
                // Fade out the Reference Line
                ReferenceLine.FadeTo(0, 300, Easing.CubicInOut);
                break;
            case DisplayOrientation.Portrait:
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
            var xAccell = e.Reading.Acceleration.X;
            var yAccell = e.Reading.Acceleration.Y;
            var zAccell = e.Reading.Acceleration.Z;
            //Calculate the tilt angle (pitch or roll) based on the accelerometer readings.
            _pitch = Math.Atan2(xAccell, Math.Sqrt(yAccell * yAccell + zAccell * zAccell));
            _roll = Math.Atan2(yAccell, Math.Sqrt(xAccell * xAccell + zAccell * zAccell));

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