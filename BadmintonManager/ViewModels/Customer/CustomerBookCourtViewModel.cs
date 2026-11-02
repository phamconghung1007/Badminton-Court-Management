using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels.Customer;

public partial class CustomerBookCourtViewModel : ViewModelBase
{
    private readonly IBranchCourtService _branchCourtService;
    private readonly IBookingService _bookingService;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    public ObservableCollection<BranchDto> Branches { get; } = new();
    public ObservableCollection<string> TimeSlots { get; } = new();
    public ObservableCollection<AvailableCourtDto> AvailableCourts { get; } = new();

    [ObservableProperty] private BranchDto? _selectedBranch;
    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private string _selectedStartTime = "17:00";
    [ObservableProperty] private string _selectedEndTime = "19:00";
    [ObservableProperty] private AvailableCourtDto? _selectedCourt;
    [ObservableProperty] private bool _hasSearched;
    [ObservableProperty] private int _foundCourtsCount;
    [ObservableProperty] private string _searchSummary = string.Empty;

    // Live Price Details
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _discountPercent;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _finalAmount;
    [ObservableProperty] private decimal _depositAmount;
    [ObservableProperty] private string _customerTierName = "Đồng";

    public CustomerBookCourtViewModel(
        IBranchCourtService branchCourtService,
        IBookingService bookingService,
        CurrentUserContext currentUser,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _branchCourtService = branchCourtService;
        _bookingService = bookingService;
        _currentUser = currentUser;
        _dialogService = dialogService;
        _navigationService = navigationService;

        CustomerTierName = _currentUser.TierName ?? "Đồng";

        InitTimeSlots();
    }

    private void InitTimeSlots()
    {
        TimeSlots.Clear();
        // 06:00 đến 24:00 (mỗi bước 30 phút)
        for (int m = 360; m <= 1440; m += 30)
        {
            int h = m / 60;
            int min = m % 60;
            TimeSlots.Add($"{h:D2}:{min:D2}");
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            var branches = await _branchCourtService.GetAllBranchDtosAsync(onlyActive: true);
            Branches.Clear();
            foreach (var b in branches)
            {
                Branches.Add(b);
            }

            if (SelectedBranch == null && Branches.Count > 0)
            {
                SelectedBranch = Branches[0];
            }

            CustomerTierName = _currentUser.TierName ?? "Đồng";
        }, _dialogService);
    }

    [RelayCommand]
    public async Task SearchCourtsAsync()
    {
        if (SelectedBranch == null)
        {
            _dialogService.ShowWarning("Vui lòng chọn cơ sở bạn muốn đặt sân.", "Thông báo");
            return;
        }

        if (SelectedDate.Date < DateTime.Today)
        {
            _dialogService.ShowWarning("Không thể tìm và đặt sân trong quá khứ.", "Lỗi thời gian");
            return;
        }

        if (!TryParseTime(SelectedStartTime, out var startTime) || !TryParseTime(SelectedEndTime, out var endTime))
        {
            _dialogService.ShowWarning("Khung giờ bắt đầu hoặc kết thúc không hợp lệ.", "Lỗi thời gian");
            return;
        }

        if (endTime <= startTime)
        {
            _dialogService.ShowWarning("Giờ kết thúc phải lớn hơn giờ bắt đầu.", "Lỗi thời gian");
            return;
        }

        int durationMinutes = (int)(endTime - startTime).TotalMinutes;
        if (durationMinutes < 60 || durationMinutes > 240)
        {
            _dialogService.ShowWarning($"Thời lượng đặt sân phải từ 60 phút đến 4 giờ (Hiện tại: {durationMinutes} phút).", "Thời lượng không hợp lệ");
            return;
        }

        await RunAsync(async () =>
        {
            var request = new SearchAvailableCourtsRequest
            {
                BranchId = SelectedBranch.Id,
                Date = SelectedDate.Date,
                StartTime = startTime,
                EndTime = endTime
            };

            var list = await _bookingService.GetAvailableCourtsAsync(request);

            AvailableCourts.Clear();
            foreach (var c in list)
            {
                AvailableCourts.Add(c);
            }

            HasSearched = true;
            FoundCourtsCount = AvailableCourts.Count;

            if (FoundCourtsCount > 0)
            {
                SearchSummary = $"Tìm thấy {FoundCourtsCount} sân trống phù hợp tại {SelectedBranch.Name} cho khung giờ {SelectedStartTime} - {SelectedEndTime} ngày {SelectedDate:dd/MM/yyyy}.";
                SelectCourt(AvailableCourts[0]);
            }
            else
            {
                SearchSummary = $"Rất tiếc! Không còn sân nào trống tại {SelectedBranch.Name} cho khung giờ {SelectedStartTime} - {SelectedEndTime} ngày {SelectedDate:dd/MM/yyyy} do đã kín lịch hoặc để lại khoảng trống 30 phút. Vui lòng chọn khung giờ khác.";
                ClearPricingPreview();
            }
        }, _dialogService);
    }

    [RelayCommand]
    public void SelectCourt(AvailableCourtDto? court)
    {
        SelectedCourt = court;
        if (court != null)
        {
            SubTotal = court.SubTotal;
            DiscountPercent = court.DiscountPercent;
            DiscountAmount = court.DiscountAmount;
            FinalAmount = court.FinalAmount;
            DepositAmount = court.DepositAmount;
        }
        else
        {
            ClearPricingPreview();
        }
    }

    private void ClearPricingPreview()
    {
        SelectedCourt = null;
        SubTotal = 0;
        DiscountPercent = 0;
        DiscountAmount = 0;
        FinalAmount = 0;
        DepositAmount = 0;
    }

    [RelayCommand]
    public async Task BookCourtAsync()
    {
        if (SelectedCourt == null)
        {
            _dialogService.ShowWarning("Vui lòng chọn một sân trống từ danh sách để tiếp tục.", "Chưa chọn sân");
            return;
        }

        if (!_currentUser.CustomerId.HasValue)
        {
            _dialogService.ShowError("Không tìm thấy thông tin hồ sơ khách hàng của bạn.", "Lỗi xác thực");
            return;
        }

        if (!TryParseTime(SelectedStartTime, out var startTime) || !TryParseTime(SelectedEndTime, out var endTime))
        {
            _dialogService.ShowWarning("Khung giờ không hợp lệ.", "Lỗi thời gian");
            return;
        }

        DateTime start = SelectedDate.Date.Add(startTime);
        DateTime end = SelectedDate.Date.Add(endTime);

        bool confirmed = _dialogService.ShowConfirmation(
            $"Bạn có chắc chắn muốn đặt sân:\n\n" +
            $"• Cơ sở: {SelectedCourt.BranchName}\n" +
            $"• Sân: {SelectedCourt.CourtName}\n" +
            $"• Thời gian: {SelectedStartTime} - {SelectedEndTime} ({SelectedDate:dd/MM/yyyy})\n" +
            $"• Tổng tiền sân: {SelectedCourt.FinalAmount:N0} VNĐ (Đã giảm {SelectedCourt.DiscountPercent:0.#}%)\n" +
            $"• Tiền cọc cần thanh toán: {SelectedCourt.DepositAmount:N0} VNĐ\n\n" +
            $"Sau khi tạo đơn, quý khách vui lòng chuyển khoản đặt cọc trong 2 giờ để giữ sân.",
            "Xác nhận đặt sân trực tuyến");

        if (!confirmed) return;

        await RunAsync(async () =>
        {
            var createRequest = new CreateBookingRequest
            {
                CourtId = SelectedCourt.CourtId,
                CustomerId = _currentUser.CustomerId.Value,
                BookingType = BookingType.Single,
                StartTime = start,
                EndTime = end
            };

            var booking = await _bookingService.CreateBookingAsync(createRequest);

            _dialogService.ShowInfo(
                $"🎉 Đặt sân trực tuyến thành công!\n\n" +
                $"• Mã đơn: {booking.Code}\n" +
                $"• Trạng thái: Chờ duyệt (Pending)\n" +
                $"• Số tiền cọc: {booking.DepositAmount:N0} VNĐ\n\n" +
                $"Quý khách vui lòng chuyển khoản cọc qua số tài khoản của cơ sở trong vòng 2 giờ. Nhân viên sẽ xác nhận đơn ngay khi nhận được thanh toán.",
                "Đặt sân thành công");

            // Làm mới lại danh sách sân trống
            await SearchCourtsAsync();
        }, _dialogService);
    }

    private static bool TryParseTime(string timeStr, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(timeStr)) return false;

        if (timeStr == "24:00")
        {
            time = TimeSpan.FromHours(24);
            return true;
        }

        return TimeSpan.TryParse(timeStr, out time);
    }
}
