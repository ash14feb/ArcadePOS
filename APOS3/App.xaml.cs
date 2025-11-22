namespace APOS3
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var services = this.Handler.MauiContext.Services;
            var mainPage = services.GetService<MainPage>();

            return new Window(mainPage) { Title = "APOS3" };
        }

    }
}
