using System;
using System.Windows;
using System.Windows.Documents;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;

namespace BadmintonManager.Views.Dialogs;

public partial class InvoicePreviewDialog : Window
{
    private readonly IInvoicePrinter _invoicePrinter;
    private readonly InvoiceDto _invoice;
    private FlowDocument? _document;

    public InvoicePreviewDialog(InvoiceDto invoice, IInvoicePrinter invoicePrinter)
    {
        InitializeComponent();
        _invoice = invoice;
        _invoicePrinter = invoicePrinter;

        LoadDocument();
    }

    private void LoadDocument()
    {
        TxtSubtitle.Text = $"Mã hóa đơn: #{_invoice.BookingCode} | Cơ sở: {_invoice.BranchName}";
        _document = _invoicePrinter.BuildDocument(_invoice);
        DocumentViewer.Document = _document;
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        if (_document != null)
        {
            bool printed = _invoicePrinter.PrintDocument(_document, $"HoaDon_{_invoice.BookingCode}");
            if (printed)
            {
                DialogResult = true;
                Close();
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
