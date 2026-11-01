using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BadmintonManager.DTOs;

namespace BadmintonManager.Infrastructure;

public interface IInvoicePrinter
{
    FlowDocument BuildDocument(InvoiceDto invoice);
    bool PrintDocument(FlowDocument document, string jobTitle = "HoaDon");
    bool PrintInvoice(InvoiceDto invoice);
    bool PrintInvoice(BookingItemDto booking, string cashierName, string paymentMethod);
}

public class InvoicePrinter : IInvoicePrinter
{
    public FlowDocument BuildDocument(InvoiceDto invoice)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(35),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Segoe UI"),
            Background = Brushes.White
        };

        // 1. Header: Brand & Branch
        var titleParagraph = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        titleParagraph.Inlines.Add(new Run("HỆ THỐNG SÂN CẦU LÔNG")
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
        });
        doc.Blocks.Add(titleParagraph);

        var branchParagraph = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        branchParagraph.Inlines.Add(new Run(invoice.BranchName.ToUpper())
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235))
        });
        doc.Blocks.Add(branchParagraph);

        if (!string.IsNullOrEmpty(invoice.BranchAddress) || !string.IsNullOrEmpty(invoice.BranchPhone))
        {
            var contactParagraph = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            string contactText = $"{invoice.BranchAddress}";
            if (!string.IsNullOrEmpty(invoice.BranchPhone)) contactText += $" - Hotline: {invoice.BranchPhone}";
            contactParagraph.Inlines.Add(new Run(contactText)
            {
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
            });
            doc.Blocks.Add(contactParagraph);
        }

        // 2. Receipt Title
        var invoiceTitle = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        invoiceTitle.Inlines.Add(new Run("PHIẾU THANH TOÁN TIỀN SÂN")
        {
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
        });
        doc.Blocks.Add(invoiceTitle);

        var codeParagraph = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        codeParagraph.Inlines.Add(new Run($"Mã đơn: #{invoice.BookingCode} | Trạng thái: {invoice.StatusText}")
        {
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105))
        });
        doc.Blocks.Add(codeParagraph);

        // 3. Information Table
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 0, 0, 14)
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(170) });
        table.Columns.Add(new TableColumn { Width = new GridLength(270) });

        var rowGroup = new TableRowGroup();

        void AddRow(string label, string value, bool isBold = false, Brush? foreground = null, bool isDivider = false)
        {
            var row = new TableRow();
            if (isDivider)
            {
                var divCell = new TableCell(new Paragraph(new Run("--------------------------------------------------------------------------------"))
                {
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    Margin = new Thickness(0, 4, 0, 4)
                })
                {
                    ColumnSpan = 2
                };
                row.Cells.Add(divCell);
            }
            else
            {
                var fg = foreground ?? new SolidColorBrush(Color.FromRgb(30, 41, 59));
                var c1 = new TableCell(new Paragraph(new Run(label))
                {
                    FontSize = 11,
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isBold ? fg : new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 2, 0, 2)
                });
                var c2 = new TableCell(new Paragraph(new Run(value))
                {
                    FontSize = 11,
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = fg,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 2, 0, 2)
                });
                row.Cells.Add(c1);
                row.Cells.Add(c2);
            }
            rowGroup.Rows.Add(row);
        }

        AddRow("Khách hàng:", invoice.CustomerName, isBold: true);
        AddRow("Số điện thoại:", invoice.CustomerPhone);
        if (!string.IsNullOrEmpty(invoice.CustomerTierName))
        {
            AddRow("Hạng thành viên:", invoice.CustomerTierName);
        }
        AddRow("Sân thi đấu:", invoice.CourtName, isBold: true);
        AddRow("Khung giờ chơi:", invoice.TimeRangeText);

        AddRow("", "", isDivider: true);

        AddRow("Tiền giờ gốc:", $"{invoice.SubTotal:N0} đ");
        if (invoice.DiscountAmount > 0)
        {
            AddRow($"Chiết khấu ({invoice.DiscountPercent}%):", $"-{invoice.DiscountAmount:N0} đ", foreground: new SolidColorBrush(Color.FromRgb(21, 128, 61)));
        }
        AddRow("TỔNG TIỀN SÂN:", $"{invoice.FinalAmount:N0} đ", isBold: true, foreground: new SolidColorBrush(Color.FromRgb(37, 99, 235)));

        if (invoice.DepositAmount > 0)
        {
            AddRow("Tiền cọc quy định:", $"{invoice.DepositAmount:N0} đ");
        }

        AddRow("ĐÃ THANH TOÁN:", $"{invoice.PaidAmount:N0} đ", isBold: true, foreground: new SolidColorBrush(Color.FromRgb(21, 128, 61)));

        if (invoice.RemainingAmount > 0)
        {
            AddRow("CÒN LẠI PHẢI THU:", $"{invoice.RemainingAmount:N0} đ", isBold: true, foreground: new SolidColorBrush(Color.FromRgb(220, 38, 38)));
        }

        AddRow("", "", isDivider: true);

        AddRow("Phương thức:", invoice.PaymentMethod);
        if (!string.IsNullOrEmpty(invoice.ReferenceNo))
        {
            AddRow("Mã giao dịch:", invoice.ReferenceNo);
        }
        AddRow("Thu ngân:", invoice.CashierName);
        AddRow("Thời gian in:", invoice.PrintedAt.ToString("dd/MM/yyyy HH:mm:ss"));

        table.RowGroups.Add(rowGroup);
        doc.Blocks.Add(table);

        // 4. Footer
        var footer = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };
        footer.Inlines.Add(new Run("Cảm ơn Quý khách! Hẹn gặp lại quý khách lần sau!")
        {
            FontSize = 11,
            FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
        });
        doc.Blocks.Add(footer);

        return doc;
    }

    public bool PrintDocument(FlowDocument document, string jobTitle = "HoaDon")
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource idp = document;
                printDialog.PrintDocument(idp.DocumentPaginator, jobTitle);
                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi in hóa đơn: {ex.Message}", "Lỗi in ấn", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return false;
    }

    public bool PrintInvoice(InvoiceDto invoice)
    {
        var doc = BuildDocument(invoice);
        return PrintDocument(doc, $"HoaDon_{invoice.BookingCode}");
    }

    public bool PrintInvoice(BookingItemDto booking, string cashierName, string paymentMethod)
    {
        var invoice = new InvoiceDto
        {
            BookingId = booking.Id,
            BookingCode = booking.Code,
            BranchName = booking.BranchName,
            CourtName = booking.CourtName,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            DurationMinutes = (int)(booking.EndTime - booking.StartTime).TotalMinutes,
            CustomerName = booking.CustomerName ?? booking.GuestName ?? "Khách vãng lai",
            CustomerPhone = booking.GuestPhone ?? "---",
            SubTotal = booking.SubTotal,
            DiscountPercent = booking.DiscountPercent,
            DiscountAmount = booking.DiscountAmount,
            FinalAmount = booking.FinalAmount,
            DepositAmount = booking.DepositAmount,
            PaidAmount = booking.PaidAmount,
            PaymentMethod = paymentMethod,
            CashierName = cashierName,
            PrintedAt = DateTime.Now,
            StatusText = booking.Status.ToString()
        };

        return PrintInvoice(invoice);
    }
}
