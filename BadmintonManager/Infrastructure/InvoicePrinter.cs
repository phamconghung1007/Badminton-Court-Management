using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BadmintonManager.DTOs;

namespace BadmintonManager.Infrastructure;

public interface IInvoicePrinter
{
    bool PrintInvoice(BookingItemDto booking, string cashierName, string paymentMethod);
}

public class InvoicePrinter : IInvoicePrinter
{
    public bool PrintInvoice(BookingItemDto booking, string cashierName, string paymentMethod)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(30),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Segoe UI")
        };

        // Header
        var titleParagraph = new Paragraph(new Run("PHIẾU THANH TOÁN TIỀN SÂN CẦU LÔNG"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Blocks.Add(titleParagraph);

        var branchParagraph = new Paragraph(new Run($"Cơ sở: {booking.BranchName}"))
        {
            FontSize = 13,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        doc.Blocks.Add(branchParagraph);

        // Body Table
        var table = new Table();
        table.Columns.Add(new TableColumn { Width = new GridLength(180) });
        table.Columns.Add(new TableColumn { Width = new GridLength(250) });

        var rowGroup = new TableRowGroup();

        void AddRow(string label, string value, bool bold = false)
        {
            var row = new TableRow();
            var cell1 = new TableCell(new Paragraph(new Run(label)) { FontSize = 12, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal });
            var cell2 = new TableCell(new Paragraph(new Run(value)) { FontSize = 12, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal });
            row.Cells.Add(cell1);
            row.Cells.Add(cell2);
            rowGroup.Rows.Add(row);
        }

        AddRow("Mã đặt sân:", booking.Code, true);
        AddRow("Khách hàng:", booking.CustomerName ?? booking.GuestName ?? "Khách vãng lai");
        AddRow("Số điện thoại:", booking.GuestPhone ?? "---");
        AddRow("Sân:", booking.CourtName, true);
        AddRow("Thời gian:", $"{booking.StartTime:dd/MM/yyyy HH:mm} - {booking.EndTime:HH:mm}");
        AddRow("--------------------------------", "--------------------------------");
        AddRow("Tiền gốc:", $"{booking.SubTotal:N0} đ");
        AddRow($"Giảm giá ({booking.DiscountPercent}%):", $"-{booking.DiscountAmount:N0} đ");
        AddRow("Tổng tiền sân:", $"{booking.FinalAmount:N0} đ", true);
        AddRow("Tiền đã cọc:", $"{booking.DepositAmount:N0} đ");
        decimal remaining = Math.Max(0, booking.FinalAmount - booking.PaidAmount);
        AddRow("Còn phải thanh toán:", $"{remaining:N0} đ", true);
        AddRow("Phương thức:", paymentMethod);
        AddRow("Thu ngân:", cashierName);
        AddRow("Ngày in:", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

        table.RowGroups.Add(rowGroup);
        doc.Blocks.Add(table);

        var footer = new Paragraph(new Run("Cảm ơn Quý khách! Hẹn gặp lại!"))
        {
            FontSize = 12,
            FontStyle = FontStyles.Italic,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 25, 0, 0)
        };
        doc.Blocks.Add(footer);

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            IDocumentPaginatorSource idp = doc;
            printDialog.PrintDocument(idp.DocumentPaginator, $"HoaDon_{booking.Code}");
            return true;
        }

        return false;
    }
}
