using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InvoiceMaker.Helpers;
using InvoiceMaker.Model;
using Microsoft.Win32;
using MyAssays.Data.Xml;
using MyAssays.ReportXmlConversion;

namespace InvoiceMaker
{
    public partial class MainWindow : Window
    {
        private readonly List<BillableUnit> _units;

        //https://app.hubstaff.com/reports/my/time_and_activities?utf8=%E2%9C%93&date=2017-10-01&date_end=2017-10-31&user_id=55290&group_by=date&filters%5Bshow_tasks%5D=1&filters%5Bshow_activity%5D=1&filters%5Bsum_date_ranges%5D=1&filters%5Bshow_notes%5D=1&filters%5Bshow_spent%5D=1&filters%5Bshow_billable%5D=&filters%5Binclude_archived%5D=1
        //TODO: Open in web
        //TODO: Chrome anonymous tabs

        public MainWindow()
        {
            InitializeComponent();

            _units = new List<BillableUnit>
            {
                new BillableUnit {Id = Constants.ID_AYurov, Description = Constants.Description_Dev, Price = 34.0},//ayurov
                new BillableUnit {Id = Constants.ID_BShchudlo, Description = Constants.Description_Dev, Price = 17.75 },//bshchudlo
                new BillableUnit {Id = Constants.ID_IPopkov, Description = Constants.Description_Dev, Price = 12.0 },//ipopkov
                new BillableUnit {Id = Constants.ID_ADvoretska, Description = Constants.Description_Content, Price = 8.32 },//advoretska
                new BillableUnit {Id = Constants.ID_OGaiduchok, Description = Constants.Description_DataAnalysis, Price = 12.0 },//ogaiduchok
                new BillableUnit {Id = Constants.ID_ShKassal, Description = Constants.Description_QualityAssurance, Price = 7.53 },//shkassal
                //new BillableUnit {Id = 0, Description = Constants.Description_Design, Price = 16.0 },//kvynnytska
            };
        }

        private async void OnGenerateButtonClick(object sender, RoutedEventArgs e)
        {
            await ProcessUnits(99266, _invoiceStartDate, _invoiceStartDate.AddMonths(1).Subtract(TimeSpan.FromSeconds(1)));
            var paymentRows = _units
                .Select((unit, i) =>
                    new ReportGroupTableRow
                    {
                        Col = new[]
                        {
                            new ReportGroupTableRowCol {Text = new[] {(i+1).ToString()}},
                            new ReportGroupTableRowCol {Text = new[] {unit.Description}, Alignment = Alignment.Left, AlignmentSpecified = true},
                            new ReportGroupTableRowCol {Text = new[] {unit.Hours.TotalHours.ToString("F2")}},
                            new ReportGroupTableRowCol {Text = new[] {unit.Price.ToString("F2")}},
                            new ReportGroupTableRowCol {Text = new[] {unit.GetTotal().ToString("F2")}},
                        }
                    })
                .Union(new[]
                    {
                        new ReportGroupTableRow
                        {
                            Col = new[]
                            {
                                new ReportGroupTableRowCol {Text = new[] {"Total to pay / Усього до сплати:"}, Alignment = Alignment.Left, AlignmentSpecified = true},
                                new ReportGroupTableRowCol {MergeLeftSpecified = true, MergeLeft = true},
                                new ReportGroupTableRowCol {MergeLeftSpecified = true, MergeLeft = true},
                                new ReportGroupTableRowCol {MergeLeftSpecified = true, MergeLeft = true},
                                new ReportGroupTableRowCol {Text = new[] {_units.Sum(unit =>unit.GetTotal()).ToString("F2")}},
                            }
                        }
                    });

            var report = new Report
            {
                WordTemplate = System.IO.Path.GetFullPath("Templates\\DC_Invoice_Template.docx"),
                FontName = "Arial",
                FontSize = 10,
                FontSizeSpecified = true,
                Items = new BaseReportElement[]
                {
                    new ReportText() {Id = "InvoiceNumberValue", Text = new[] {_invoiceNumber.ToString("00")}},
                    new ReportText() {Id = "DayValue", Text = new[] {DateTime.Today.Day.ToString("00")}},
                    new ReportText() {Id = "MonthValue", Text = new[] {DateTime.Today.Month.ToString("00")}},
                    new ReportText() {Id = "YearValue", Text = new[] {DateTime.Today.Year.ToString("0000")}},
                    new ReportGroupTable
                    {
                        Id = "PaymentsTable",
                        //AutoFit = AutoFitBehavior.AutoFitToWindow,
                        //AutoFitSpecified = true,
                        RepOrientation = ReportGroupTableRepOrientation.Vertical,
                        RepOrientationSpecified = true,
                        DisplayTableBorderSpecified = true,
                        DisplayTableBorder = true,
                        AutoFitColumns = true,
                        AutoFitColumnsSpecified = true,
                        Header = new ReportGroupTableHeader
                        {
                            Col = new[]
                            {
                                new ReportGroupTableHeaderCol {Value = "№", AlignmentBody = Alignment.Centre, AlignmentHeader = Alignment.Centre, AlignmentBodySpecified = true, AlignmentHeaderSpecified = true},
                                new ReportGroupTableHeaderCol {Value = "Description / Опис", AlignmentBody = Alignment.Left, AlignmentHeader = Alignment.Centre, AlignmentBodySpecified = true, AlignmentHeaderSpecified = true},
                                new ReportGroupTableHeaderCol {Value = "Quantity /\nКількість", AlignmentBody = Alignment.Centre, AlignmentHeader = Alignment.Centre, AlignmentBodySpecified = true, AlignmentHeaderSpecified = true},
                                new ReportGroupTableHeaderCol
                                {
                                    Value = "Price, GBP / Ціна,\nАнглійський фунт\nстерлінгів",
                                    AlignmentBody = Alignment.Centre,
                                    AlignmentHeader = Alignment.Centre,
                                    AlignmentBodySpecified = true,
                                    AlignmentHeaderSpecified = true,
                                    FormatAsText = true,
                                    FormatAsTextSpecified = true,
                                    FormatAsTextAll = true,
                                    FormatAsTextAllSpecified = true,
                                },
                                new ReportGroupTableHeaderCol
                                {
                                    Value = "Amount, GBP / Загальна\nвартість, Англійський\nфунт стерлінгів",
                                    AlignmentBody = Alignment.Centre,
                                    AlignmentBodySpecified = true,
                                    AlignmentHeader = Alignment.Centre,
                                    AlignmentHeaderSpecified = true,
                                    FormatAsText = true,
                                    FormatAsTextSpecified = true,
                                    FormatAsTextAll = true,
                                    FormatAsTextAllSpecified = true,
                                },
                            }
                        },
                        Row = paymentRows.ToArray()
                    },
                }
            };
            var format = FileSaveFormat.Docx;
            var ext = XmlConverter.GetExtensionFromSaveFormat(format);
            var defaultFileName = $"{InvoicePrefixTextBox.Text}_{_invoiceNumber}{ext}";
            var outputFilePath = System.IO.Path.GetFullPath(defaultFileName);
            var resultFilePath = outputFilePath;
            XmlConverter.Convert(report, outputFilePath, FileSaveFormat.Docx, ConverterEngineType.Word);
            var dialog = new SaveFileDialog
            {
                InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Contracts"),
                FileName = defaultFileName,
                Filter = $"{format} Format|*{ext}"
            };
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                resultFilePath = dialog.FileName;
                try
                {
                    File.Copy(outputFilePath, resultFilePath, true);
                }
                catch (Exception)
                {
                    MessageBox.Show(e.ToString());
                    return;
                }
            }
            MessageBox.Show(this, $"The report is generated and saved to {resultFilePath}.", "Invoice Maker", MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start(resultFilePath);
        }

        private async Task ProcessUnits(int projectId, DateTime startDate, DateTime endDate)
        {
            var fileUsers = HubstaffApi.Instance.GetUsers();
            foreach (var unit in _units)
            {
                var fileuser = fileUsers.FirstOrDefault(fu => fu.Id == unit.Id);
                unit.User = fileuser;
                var hours = await HubstaffApi.Instance.GetWorkedHours(unit, projectId, startDate, endDate);
                unit.Hours = hours;
            }
        }

        private int _invoiceNumber = 0;
        private DateTime _invoiceStartDate;

        private void OnInvoiceNumberTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(InvoiceNumberTextBox.Text, out _invoiceNumber))
            {
                _invoiceStartDate = Constants.ContractStartDate.AddMonths(_invoiceNumber - 1);
                if (InvoiceDateTextBlock != null)
                {
                    InvoiceDateTextBlock.Text = $"{_invoiceStartDate:dd.MM.yyyy} - {_invoiceStartDate.AddMonths(1).Subtract(TimeSpan.FromDays(1)):dd.MM.yyyy}";
                }
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            InvoiceNumberTextBox.Text = 17.ToString();
        }
    }
}
