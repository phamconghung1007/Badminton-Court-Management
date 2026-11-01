using System;
using System.Globalization;
using System.Windows;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;

namespace BadmintonManager.Views.Dialogs;

public partial class PaymentDialog : Window
{
    private readonly int _bookingId;
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoicePrinter _invoicePrinter;
    private readonly IDialogService _dialogService;

    private PaymentSummaryDto? _summary;

    public PaymentDialog(
        int bookingId,
        IPaymentService paymentService,
        IInvoiceService invoiceService,
        IInvoicePrinter invoicePrinter,
        IDialogService dialogService)
    {
        InitializeComponent();
        _bookingId = bookingId;
        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _invoicePrinter = invoicePrinter;
        _dialogService = dialogService;

        Loaded += PaymentDialog_Loaded;
    }

    private async void PaymentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _summary = await _paymentService.GetPaymentSummaryAsync(_bookingId);
            BindData();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể tải thông tin thanh toán: {ex.Message}");
            Close();
        }
    }

    private void BindData()
    {
        if (_summary == null) return;

        TxtBookingCode.Text = $"Đơn #{_summary.BookingCode}";
        TxtStatus.Text = _summary.StatusText;
        TxtCourtAndBranch.Text = $"{_summary.CourtName} - {_summary.BranchName}";
        string custText = _summary.CustomerName ?? "Khách vãng lai";
        if (!string.IsNullOrEmpty(_summary.CustomerPhone)) custText += $" ({_summary.CustomerPhone})";
        if (!string.IsNullOrEmpty(_summary.CustomerTierName)) custText += $" - Hạng {_summary.CustomerTierName}";
        TxtCustomerInfo.Text = custText;

        int durationMin = (int)(_summary.EndTime - _summary.StartTime).TotalMinutes;
        TxtTimeRange.Text = $"{_summary.StartTime:dd/MM/yyyy HH:mm} - {_summary.EndTime:HH:mm} ({durationMin} phút)";

        TxtFinalAmount.Text = $"{_summary.FinalAmount:N0} đ";
        TxtTotalPaid.Text = $"{_summary.TotalPaid:N0} đ";
        TxtRemainingAmount.Text = $"{_summary.RemainingAmount:N0} đ";

        // Gợi ý loại thanh toán ban đầu
        if (_summary.Status == BookingStatus.Pending)
        {
            RbDeposit.IsChecked = true;
            TxtAmount.Text = $"{_summary.DepositAmount:N0}";
        }
        else
        {
            RbFinal.IsChecked = true;
            TxtAmount.Text = $"{_summary.RemainingAmount:N0}";
        }
    }

    private void RbPaymentType_Changed(object sender, RoutedEventArgs e)
    {
        if (_summary == null) return;

        if (RbDeposit.IsChecked == true)
        {
            TxtAmount.Text = $"{_summary.DepositAmount:N0}";
        }
        else if (RbFinal.IsChecked == true)
        {
            TxtAmount.Text = $"{_summary.RemainingAmount:N0}";
        }
    }

    private void BtnQuickDeposit_Click(object sender, RoutedEventArgs e)
    {
        if (_summary == null) return;
        RbDeposit.IsChecked = true;
        TxtAmount.Text = $"{_summary.DepositAmount:N0}";
    }

    private void BtnQuickRemaining_Click(object sender, RoutedEventArgs e)
    {
        if (_summary == null) return;
        RbFinal.IsChecked = true;
        TxtAmount.Text = $"{_summary.RemainingAmount:N0}";
    }

    private void RbMethod_Changed(object sender, RoutedEventArgs e)
    {
        if (TxtReferenceNo == null) return;
        TxtReferenceNo.Visibility = (RbBankTransfer.IsChecked == true)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void BtnConfirmPayment_Click(object sender, RoutedEventArgs e)
    {
        if (_summary == null) return;

        string cleanText = (TxtAmount.Text ?? string.Empty).Replace(".", "").Replace(",", "").Trim();
        if (!decimal.TryParse(cleanText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
        {
            _dialogService.ShowWarning("Vui lòng nhập số tiền thanh toán hợp lệ (lớn hơn 0).");
            return;
        }

        var method = (RbBankTransfer.IsChecked == true) ? PaymentMethod.BankTransfer : PaymentMethod.Cash;
        var type = (RbDeposit.IsChecked == true) ? PaymentType.Deposit : PaymentType.Final;

        try
        {
            var result = await _paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
            {
                BookingId = _summary.BookingId,
                Amount = amount,
                Method = method,
                Type = type,
                ReferenceNo = TxtReferenceNo.Text?.Trim(),
                Note = TxtNote.Text?.Trim()
            });

            _dialogService.ShowInfo(result.Message, "Thanh toán thành công");

            // Hỏi người dùng có muốn xem và in hóa đơn không
            bool wantInvoice = _dialogService.ShowConfirmation(
                "Bạn có muốn xem trước và in hóa đơn thanh toán cho đơn này không?",
                "In hóa đơn");

            if (wantInvoice)
            {
                var invoiceData = await _invoiceService.GetInvoiceDataAsync(_summary.BookingId);
                var previewDlg = new InvoicePreviewDialog(invoiceData, _invoicePrinter)
                {
                    Owner = this.Owner
                };
                previewDlg.ShowDialog();
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
