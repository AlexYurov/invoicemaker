using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MyAssays.Data.Xml;
using MyAssays.ReportXmlConversion;

namespace InvoiceMaker
{
    public partial class MainWindow : Window
    {
        private readonly List<BillableUnit> _units;

        public MainWindow()
        {
            InitializeComponent();

            _units = new List<BillableUnit>
            {
                new BillableUnit {Id = 55290, Description = Constants.Description_Dev, Price = 34.0},//ayurov
                new BillableUnit {Id = 115639, Description = Constants.Description_Dev, Price = 17.75, Hours = new TimeSpan(96,59,33 )},//bshchudlo
                new BillableUnit {Id = 95815, Description = Constants.Description_Dev, Price = 12.0, Hours = new TimeSpan(65,17,39)},//ipopkov
                new BillableUnit {Id = 63497, Description = Constants.Description_Content, Price = 6.93, Hours = new TimeSpan(66,36,31 )},//advoretska
                new BillableUnit {Id = 139623, Description = Constants.Description_DataAnalysis, Price = 12.0, Hours = new TimeSpan(97,59,38)},//ogaiduchok
                //new BillableUnit {Id = 0, Description = Constants.Description_Design, Price = 16.0, Hours = new TimeSpan(12,50,45)},//kvynnytska
            };
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            await ProcessUnits(99266, new DateTime(2017, 7, 1), new DateTime(2017, 8, 1).Subtract(TimeSpan.FromSeconds(1)));
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
            var outputFilePath = System.IO.Path.GetFullPath("test.docx");
            XmlConverter.Convert(report, outputFilePath, FileSaveFormat.Docx, ConverterEngineType.Word);
            MessageBox.Show(this, "Generated!");
            Process.Start(outputFilePath);
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
    }
}
