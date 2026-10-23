using System;
using System.IO;
using System.Linq;
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
                services.AddTransient<RegisterViewModel>();
                services.AddTransient<ShellViewModel>();

                // Windows
                services.AddTransient<LoginWindow>();
                services.AddTransient<RegisterWindow>();
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

                if (e.Args.Contains("--seed-only"))
                {
                    Shutdown(0);
                    return;
                }

                if (e.Args.Contains("--verify-dod"))
                {
                    var report = await DbHealthChecker.VerifyDay1DoDAsync(db);
                    Console.WriteLine("==================================================");
                    Console.WriteLine("      BÁO CÁO KIỂM TRA ĐẠT TIÊU CHÍ NGÀY 1 (DoD)  ");
                    Console.WriteLine("==================================================");
                    foreach (var item in report.Items)
                    {
                        string status = item.IsPassed ? "[PASS]" : "[FAIL]";
                        Console.WriteLine($"{status} {item.Category}: {item.Description} -> {item.Details}");
                    }
                    Console.WriteLine("==================================================");
                    Console.WriteLine($"KẾT QUẢ TỔNG QUAN: {(report.IsAllPassed ? "ĐẠT TOÀN BỘ TIÊU CHÍ NGÀY 1 (DoD SUCCESS)!" : "CẦN KIỂM TRA LẠI")}");
                    Console.WriteLine("==================================================");
                    Shutdown(report.IsAllPassed ? 0 : 1);
                    return;
                }
            }

            ShowLogin();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khởi động cơ sở dữ liệu: {ex.Message}", "Lỗi Khởi Động", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ShowLogin()
    {
        var loginVm = _host.Services.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginVm);

        loginVm.RegisterRequested += () =>
        {
            var registerVm = _host.Services.GetRequiredService<RegisterViewModel>();
            var registerWindow = new RegisterWindow(registerVm);

            registerVm.RegistrationSucceeded += () =>
            {
                registerWindow.Close();
            };

            registerVm.CancelRequested += () =>
            {
                registerWindow.Close();
            };

            loginWindow.Hide();
            registerWindow.ShowDialog();
            loginWindow.Show();
        };

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
