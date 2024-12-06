using Microsoft.Maui.Devices;
namespace SensorAnimation;

public partial class MainPage : ContentPage
{

    /// <summary>
    /// The maximum angle the level can swing before being considered immobile
    /// Basically, this is used to stabilize the level when the device is not moving
    /// </summary>
    private const double maxSwingAngle = 0.25;

    private PeriodicTimer _clock;

    private double _pitch;
    private double _roll;
    private double _lastAngle;

    private DisplayOrientation _currentOrientation;

    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public MainPage()
    {
        InitializeComponent();

        ToggleAccelerometer();
        _currentOrientation = DeviceDisplay.MainDisplayInfo.Orientation;
        DeviceDisplay.MainDisplayInfoChanged += DeviceDisplayOnMainDisplayInfoChanged;
        AngleShiftLabel.Text = $"Angle: 0.00°";
    }

    /// <summary>
    /// Change the UI based on the orientation of the device
    /// </summary>
    /// <remarks>
    /// The Way you hold your device has an effect on how the orientation change 
    /// - This is done to avoid unwanted flips when the device is flat on a table or close to a vertical position
    /// Because the calculation is based on the value of the 3 axis values, 
    /// the device must be held in a way that the z-axis is pointing slightly towards the ground
    /// </remarks>
    private async void DeviceDisplayOnMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        if (_currentOrientation == e.DisplayInfo.Orientation) return;

        _currentOrientation = e.DisplayInfo.Orientation;
        // Animate between landscape and portrait visuals
        if (_currentOrientation == DisplayOrientation.Landscape)
        {
            //Await for all animations to complete
            await Task.WhenAll(
                PlumbLine.FadeTo(0, 600, Easing.CubicInOut),
                ReferenceLine.FadeTo(0, 600, Easing.CubicInOut),
                HorizontalSpiritGrid.FadeTo(1, 600, Easing.CubicInOut)
            );
        }
        else if (_currentOrientation == DisplayOrientation.Portrait)
        {
            await Task.WhenAll(
                PlumbLine.FadeTo(1, 600, Easing.CubicInOut),
                ReferenceLine.FadeTo(1, 600, Easing.CubicInOut),
                HorizontalSpiritGrid.FadeTo(0, 600, Easing.CubicInOut)
            );
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
                Accelerometer.Default.Start(SensorSpeed.Default);
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

    private async void AnimatePlumbOrBubble()
    {
        _clock = new PeriodicTimer(TimeSpan.FromMilliseconds(150));

        while (await _clock.WaitForNextTickAsync())
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_currentOrientation == DisplayOrientation.Portrait)
                {
                    var angleInDegrees = _pitch * 180 / Math.PI;
                    if (Math.Abs(angleInDegrees - _lastAngle) > maxSwingAngle)
                    {
                        _lastAngle = angleInDegrees;
                        AngleShiftLabel.Text = $"Angle: {-angleInDegrees:F2}°";
                        await PlumbLine.RotateTo(angleInDegrees, 100, Easing.CubicOut);
                    }
                }
                else
                {
                    var angleInDegrees = -(_roll * 180 / Math.PI);
                    if (Math.Abs(angleInDegrees - _lastAngle) > maxSwingAngle)
                    {
                        _lastAngle = angleInDegrees;
                        AngleShiftLabel.Text = $"Angle: {angleInDegrees:F2}°";
                        //Move the bubble image based on the angle
                        await BubbleImage.TranslateTo(angleInDegrees * 2.5, 0, 100, Easing.CubicOut);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AnimatePlumbOrBubble();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ToggleAccelerometer();
        DeviceDisplay.MainDisplayInfoChanged -= DeviceDisplayOnMainDisplayInfoChanged;
        _clock?.Dispose();
    }
}