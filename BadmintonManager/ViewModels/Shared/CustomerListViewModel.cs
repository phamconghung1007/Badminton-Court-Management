using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using BadmintonManager.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BadmintonManager.ViewModels.Shared;

public partial class CustomerListViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;
    private readonly IServiceProvider? _serviceProvider;

    [ObservableProperty] private string _searchKeyword = string.Empty;
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private CustomerDetailDto? _currentDetail;

    [ObservableProperty] private int _totalCustomerCount;
    [ObservableProperty] private int _activeCustomerCount;
    [ObservableProperty] private int _goldTierCount;
    [ObservableProperty] private int _silverTierCount;

    [ObservableProperty] private bool _isDetailDrawerOpen;
    [ObservableProperty] private bool _isEditDrawerOpen;
    [ObservableProperty] private bool _isEditing;

    // Form fields
    [ObservableProperty] private int _editCustomerId;
    [ObservableProperty] private string _editFullName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string? _editCardNo;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string? _formErrorMessage;

    public ObservableCollection<CustomerDto> Customers { get; } = new();

    public CustomerListViewModel(
        ICustomerService customerService,
        CurrentUserContext currentUser,
        IDialogService dialogService,
        IServiceProvider? serviceProvider = null)
    {
        _customerService = customerService;
        _currentUser = currentUser;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        await LoadCustomersInternalAsync();
    }

    [RelayCommand]
    public async Task LoadCustomersAsync()
    {
        await RunAsync(LoadCustomersInternalAsync);
    }

    private async Task LoadCustomersInternalAsync()
    {
        var list = await _customerService.SearchCustomersAsync(SearchKeyword);
        Customers.Clear();
        foreach (var item in list)
        {
            Customers.Add(item);
        }

        TotalCustomerCount = Customers.Count;
        ActiveCustomerCount = Customers.Count(c => c.IsActive);
        GoldTierCount = Customers.Count(c => c.TierName.Contains("Vàng", StringComparison.OrdinalIgnoreCase));
        SilverTierCount = Customers.Count(c => c.TierName.Contains("Bạc", StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        await LoadCustomersAsync();
    }

    [RelayCommand]
    public void OpenCreateDrawer()
    {
        IsEditing = false;
        EditCustomerId = 0;
        EditFullName = string.Empty;
        EditPhone = string.Empty;
        EditCardNo = string.Empty;
        EditIsActive = true;
        FormErrorMessage = null;

        IsDetailDrawerOpen = false;
        IsEditDrawerOpen = true;
    }

    [RelayCommand]
    public void OpenEditDrawer(CustomerDto? item)
    {
        var target = item ?? SelectedCustomer;
        if (target == null) return;

        IsEditing = true;
        EditCustomerId = target.Id;
        EditFullName = target.FullName;
        EditPhone = target.Phone;
        EditCardNo = target.MembershipCardNo;
        EditIsActive = target.IsActive;
        FormErrorMessage = null;

        IsDetailDrawerOpen = false;
        IsEditDrawerOpen = true;
    }

    [RelayCommand]
    public async Task SaveCustomerAsync()
    {
        FormErrorMessage = null;
        if (string.IsNullOrWhiteSpace(EditFullName))
        {
            FormErrorMessage = "Vui lòng nhập họ và tên khách hàng.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditPhone))
        {
            FormErrorMessage = "Vui lòng nhập số điện thoại.";
            return;
        }

        await RunAsync(async () =>
        {
            try
            {
                if (IsEditing)
                {
                    var req = new CustomerUpdateDto(
                        EditCustomerId,
                        EditFullName.Trim(),
                        EditPhone.Trim(),
                        string.IsNullOrWhiteSpace(EditCardNo) ? null : EditCardNo.Trim(),
                        EditIsActive
                    );
                    await _customerService.UpdateCustomerAsync(req);
                    _dialogService.ShowInfo("Cập nhật thông tin khách hàng thành công!", "Thông báo");
                }
                else
                {
                    var req = new CustomerCreateDto(
                        EditFullName.Trim(),
                        EditPhone.Trim(),
                        string.IsNullOrWhiteSpace(EditCardNo) ? null : EditCardNo.Trim()
                    );
                    await _customerService.CreateCustomerAsync(req);
                    _dialogService.ShowInfo("Thêm mới khách hàng thành công!", "Thông báo");
                }

                IsEditDrawerOpen = false;
                await LoadCustomersInternalAsync();
            }
            catch (BusinessException ex)
            {
                FormErrorMessage = ex.Message;
            }
        });
    }

    [RelayCommand]
    public async Task ToggleCustomerStatusAsync(CustomerDto? item)
    {
        var target = item ?? SelectedCustomer;
        if (target == null) return;

        string actionName = target.IsActive ? "khóa" : "kích hoạt lại";
        bool confirm = _dialogService.ShowConfirmation(
            $"Bạn có chắc chắn muốn {actionName} tài khoản khách hàng '{target.FullName}' ({target.Phone}) không?",
            "Xác nhận thay đổi trạng thái");

        if (!confirm) return;

        await RunAsync(async () =>
        {
            await _customerService.ToggleCustomerStatusAsync(target.Id);
            _dialogService.ShowInfo($"Đã {actionName} tài khoản khách hàng thành công.", "Thông báo");
            await LoadCustomersInternalAsync();
        });
    }

    [RelayCommand]
    public async Task ViewCustomerDetailAsync(CustomerDto? item)
    {
        var target = item ?? SelectedCustomer;
        if (target == null) return;

        await RunAsync(async () =>
        {
            CurrentDetail = await _customerService.GetCustomerDetailAsync(target.Id);
            if (CurrentDetail != null)
            {
                IsEditDrawerOpen = false;
                IsDetailDrawerOpen = true;
            }
        });
    }

    [RelayCommand]
    public void CloseDrawers()
    {
        IsDetailDrawerOpen = false;
        IsEditDrawerOpen = false;
        FormErrorMessage = null;
    }

    [RelayCommand]
    public async Task OpenFixedScheduleDialogAsync(CustomerDto? item)
    {
        var target = item ?? SelectedCustomer;
        if (target == null) return;

        if (_serviceProvider != null)
        {
            var fixedScheduleService = _serviceProvider.GetRequiredService<IFixedScheduleService>();
            var branchCourtService = _serviceProvider.GetRequiredService<IBranchCourtService>();

            var dialog = new FixedScheduleDialog(fixedScheduleService, branchCourtService, _currentUser, target.Id, target.FullName);
            dialog.Owner = Application.Current?.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                // Tải lại chi tiết khách hàng nếu đang mở
                if (IsDetailDrawerOpen && CurrentDetail != null && CurrentDetail.Customer.Id == target.Id)
                {
                    await ViewCustomerDetailAsync(target);
                }
            }
        }
    }
}
