namespace SensorAnimation;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

    }

    protected override Window CreateWindow(IActivationState activationState)
    {

        //app.Services
        return new Window(new AppShell());
        // return 
    }
}
