using APOS3.DataAccess;
using APOS3.DataAccess.Repos;
using APOS3.Services;
using Microsoft.Extensions.Logging;

namespace APOS3
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });






            var connectionString = "Server=127.0.0.1;Database=esnew;Uid=root;Pwd=new_password123;";
            builder.Services.AddSingleton<IDatabaseConnection>(new DatabaseConnection(connectionString));

            // Register repositories
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IBillingRepository, BillingRepository>();
            builder.Services.AddScoped<IBonusRepository, BonusRepository>();

            // Add this with your other service registrations
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();


            builder.Services.AddScoped<OrderStateService>();
            //builder.Services.AddSingleton<IRfidService, RfidService>();

            // ADD NEW REPOSITORIES FOR RFID FUNCTIONALITY
            builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
            builder.Services.AddScoped<IRfidService, RfidService>();


            //builder.Services.AddSingleton<IUdpListenerService, UdpListenerService>();
            //builder.Services.AddHostedService<UdpBackgroundService>();
            builder.Services.AddSingleton<UdpService>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<UdpService>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
