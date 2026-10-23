using System;
using System.IO;
using System.Windows;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels;
using BadmintonManager.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BadmintonManager;

public partial class App : Application
{
    private readonly IHost _host;
    private Window? _currentWindow;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                string connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                    ?? "Server=localhost;Database=BadmintonDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

                // EF Core DbContextFactory (Section 5.4)
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                });

                // Singletons
                services.AddSingleton<CurrentUserContext>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IInvoicePrinter, InvoicePrinter>();

                // Business Services (Transient / Scoped)
                services.AddTransient<IAuthService, AuthService>();
                services.AddTransient<IBookingService, BookingService>();
                services.AddTransient<IPricingService, PricingService>();
                services.AddTransient<IPaymentService, PaymentService>();
                services.AddTransient<IMembershipService, MembershipService>();
                services.AddTransient<ICustomerService, CustomerService>();
                services.AddTransient<IBranchCourtService, BranchCourtService>();
                services.AddTransient<IUserService, UserService>();
                services.AddTransient<IReportService, ReportService>();
                services.AddTransient<ISettingService, SettingService>();

                // ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<ShellViewModel>();

                // Windows
                services.AddTransient<LoginWindow>();
                services.AddTransient<ShellWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await _host.StartAsync();

            // Run database migration and seed data
            var factory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
                await DbSeeder.SeedAsync(db);
            }

            if (e.Args.Contains("--seed-only"))
            {
                Shutdown(0);
                return;
            }

            ShowLogin();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khởi động cơ sở dữ liệu: {ex.Message}", "Lỗi Khởi Động", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ShowLogin()
    {
        var loginVm = _host.Services.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginVm);

        loginVm.LoginSucceeded += () =>
        {
            var shellVm = _host.Services.GetRequiredService<ShellViewModel>();
            var shellWindow = new ShellWindow(shellVm);

            shellVm.LogoutRequested += () =>
            {
                shellWindow.Close();
                ShowLogin();
            };

            _currentWindow = shellWindow;
            shellWindow.Show();
            loginWindow.Close();
        };

        _currentWindow = loginWindow;
        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }
        base.OnExit(e);
    }
}
