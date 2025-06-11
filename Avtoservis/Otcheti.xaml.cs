using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Avtoservis.Entities;    // Ваш DbContext, модели
using LiveCharts;
using LiveCharts.Wpf;
using System.Data.Entity;    // Для Include("...") в EF6
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для Otcheti.xaml
    /// </summary>
    public partial class Otcheti : Page
    {
        private Border _currentPopup;
        private Button _currentButton;

        public Otcheti()
        {
            InitializeComponent();

            // Устанавливаем даты: месяц назад и сегодня
            DateTime today = DateTime.Now.Date;
            DateTime monthAgo = today.AddMonths(-1);

            BtnDateFrom.Content = monthAgo.ToString("dd.MM.yyyy");
            BtnDateTo.Content = today.ToString("dd.MM.yyyy");

            LoadData();
        }

        private void BtnDateFrom_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (_currentPopup != null && _currentButton == button)
            {
                ClosePopup();
                return;
            }

            ClosePopup();
            _currentButton = button;

            var calendar = new Calendar
            {
                DisplayMode = CalendarMode.Month,
                SelectionMode = CalendarSelectionMode.SingleDate,
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                Padding = new Thickness(5),
                Width = 200,
                Height = 160,
                FontSize = 25
            };

            // Устанавливаем выбранную дату, если она есть
            DateTime currentDate;
            if (DateTime.TryParse(BtnDateFrom.Content.ToString(), out currentDate))
            {
                calendar.SelectedDate = currentDate;
                calendar.DisplayDate = currentDate;
            }

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    BtnDateFrom.Content = calendar.SelectedDate.Value.ToString("dd.MM.yyyy");
                    ClosePopup();
                }
            };

            _currentPopup = new Border
            {
                Style = (Style)FindResource("PopupBlockStyle"),
                Child = calendar,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray
            };

            PopupCanvas.Children.Add(_currentPopup);
            Point buttonPos = button.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 5);
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void BtnDateTo_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (_currentPopup != null && _currentButton == button)
            {
                ClosePopup();
                return;
            }

            ClosePopup();
            _currentButton = button;

            var calendar = new Calendar
            {
                DisplayMode = CalendarMode.Month,
                SelectionMode = CalendarSelectionMode.SingleDate,
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                Padding = new Thickness(5),
                Width = 200,
                Height = 160,
                FontSize = 25
            };

            // Устанавливаем выбранную дату, если она есть
            DateTime currentDate;
            if (DateTime.TryParse(BtnDateTo.Content.ToString(), out currentDate))
            {
                calendar.SelectedDate = currentDate;
                calendar.DisplayDate = currentDate;
            }

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    BtnDateTo.Content = calendar.SelectedDate.Value.ToString("dd.MM.yyyy");
                    ClosePopup();
                }
            };

            _currentPopup = new Border
            {
                Style = (Style)FindResource("PopupBlockStyle"),
                Child = calendar,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray
            };

            PopupCanvas.Children.Add(_currentPopup);
            Point buttonPos = button.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 5);
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void ClosePopup()
        {
            if (_currentPopup != null)
            {
                PopupCanvas.Children.Remove(_currentPopup);
                _currentPopup = null;
                this.PreviewMouseDown -= Window_PreviewMouseDown;
            }
            _currentButton = null;
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentPopup != null && !_currentPopup.IsMouseOver &&
                (_currentButton == null || !_currentButton.IsMouseOver))
            {
                ClosePopup();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            DateTime startDate, endDate;

            if (!DateTime.TryParse(BtnDateFrom.Content.ToString(), out startDate) ||
                !DateTime.TryParse(BtnDateTo.Content.ToString(), out endDate))
            {
                MessageBox.Show("Выберите корректный период для отображения отчетов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadData();
        }

        private void LoadData()
        {
            DateTime startDate = DateTime.Parse(BtnDateFrom.Content.ToString()).Date;
            DateTime endDate = DateTime.Parse(BtnDateTo.Content.ToString()).Date.AddDays(1).AddTicks(-1);

            // Остальной код загрузки данных остается без изменений
            LoadOrdersStatistics(startDate, endDate);
            LoadCarsAnalysis();
            LoadServicesAndParts(startDate, endDate);
            LoadClientsAndEmployees(startDate, endDate);
            LoadDefectsAnalysis(startDate, endDate);
            LoadFinancialReports(startDate, endDate);
        }

        private void LoadOrdersStatistics(DateTime startDate, DateTime endDate)
        {
            // 1.1 Распределение заказов по статусам
            var ordersByStatus = App.Context.dm_Zakazi
                .Where(z => z.Data_sozdaniya >= startDate && z.Data_sozdaniya <= endDate)
                .GroupBy(z => z.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var statusSeries = new SeriesCollection();
            foreach (var item in ordersByStatus)
            {
                statusSeries.Add(new PieSeries
                {
                    Title = item.Status,
                    Values = new ChartValues<int> { item.Count },
                    DataLabels = true
                });
            }
            OrdersByStatusChart.Series = statusSeries;


            // 1.2 Количество заказов по (год–месяц)
            var ordersByMonth = App.Context.dm_Zakazi
                .Where(z => z.Data_sozdaniya >= startDate && z.Data_sozdaniya <= endDate)
                .GroupBy(z => new { z.Data_sozdaniya.Year, z.Data_sozdaniya.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var monthLabels = ordersByMonth.Select(x => $"{x.Year:0000}-{x.Month:00}").ToArray();
            var monthCounts = ordersByMonth.Select(x => x.Count).ToArray();

            OrdersByMonthChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Заказы",
                    Values = new ChartValues<int>(monthCounts)
                }
            };
            OrdersByMonthChart.AxisX[0].Labels = monthLabels;


            // 1.3 Динамика доходов за каждый месяц из выбранного диапазона
            // Вместо «только те месяцы, где есть заказы», теперь перебираем каждый месяц от startDate до endDate
            var incomeLabels = new List<string>();
            var incomeValues = new List<decimal>();

            // Начинаем с первого числа месяца startDate, заканчиваем первым числом месяца endDate
            DateTime cursor = new DateTime(startDate.Year, startDate.Month, 1);
            DateTime lastMonth = new DateTime(endDate.Year, endDate.Month, 1);

            while (cursor <= lastMonth)
            {
                // Считаем доход за текущий месяц (cursor.Year–cursor.Month)
                decimal monthIncome = App.Context.dm_Zakazi
                    .Include("dm_Raboti_v_zakaze.dm_Raboti")
                    .Include("dm_Detali_v_zakaze.dm_Detali")
                    .Where(z => z.Oplata
                             && z.Data_sozdaniya.Year == cursor.Year
                             && z.Data_sozdaniya.Month == cursor.Month)
                    .ToList()
                    .Sum(z =>
                        // Сумма по работам (стоящим со значением Zakrep_stoimost, либо Stoimost)
                        z.dm_Raboti_v_zakaze.Sum(r =>
                            r.Zakrep_stoimost.HasValue
                                ? r.Zakrep_stoimost.Value
                                : r.dm_Raboti.Stoimost
                        )
                        // + себестоимость деталей (Detal_klienta == false)
                        + z.dm_Detali_v_zakaze
                            .Where(d => !d.Detal_klienta)
                            .Sum(d =>
                                (d.Zakrep_cena.HasValue ? d.Zakrep_cena.Value : d.dm_Detali.Cena)
                                * d.Kolichestvo
                            )
                    );

                incomeLabels.Add($"{cursor:yyyy-MM}");
                incomeValues.Add(monthIncome);

                cursor = cursor.AddMonths(1);
            }

            // Если нет ни одного месяца (в теории cursor > lastMonth сразу), всё равно создадим пустую серию
            IncomeChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Доход",
                    Values = new ChartValues<decimal>(incomeValues)
                }
            };
            IncomeChart.AxisX[0].Labels = incomeLabels.ToArray();
        }

        private void LoadCarsAnalysis()
        {
            // 2.1 Распределение автомобилей по маркам
            var carsByBrand = App.Context.dm_Avtomobili
                .Include("dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto")
                .GroupBy(a => a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki)
                .Select(g => new { Brand = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var brandSeries = new SeriesCollection();
            foreach (var item in carsByBrand)
            {
                brandSeries.Add(new PieSeries
                {
                    Title = item.Brand,
                    Values = new ChartValues<int> { item.Count },
                    DataLabels = true
                });
            }
            CarsByBrandChart.Series = brandSeries;


            // 2.2 Количество автомобилей по годам выпуска
            var carsByYear = App.Context.dm_Avtomobili
                .Include("dm_Komplektacii_avto.dm_Modeli_avto")
                .GroupBy(a => a.dm_Komplektacii_avto.dm_Modeli_avto.God_vipuska)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToList();

            var yearLabels = carsByYear.Select(x => x.Year.ToString()).ToArray();
            var yearCounts = carsByYear.Select(x => x.Count).ToArray();

            CarsByYearChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Автомобили",
                    Values = new ChartValues<int>(yearCounts)
                }
            };
            CarsByYearChart.AxisX[0].Labels = yearLabels;
        }

        private void LoadServicesAndParts(DateTime startDate, DateTime endDate)
        {
            // 3. Топ-10 самых частых работ
            var topServices = App.Context.dm_Raboti_v_zakaze
                .Include("dm_Raboti")
                .Include("dm_Zakazi")
                .Where(r => r.dm_Zakazi.Data_sozdaniya >= startDate && r.dm_Zakazi.Data_sozdaniya <= endDate)
                .GroupBy(r => r.dm_Raboti.Naimenovanie)
                .Select(g => new { Service = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var serviceLabels = topServices.Select(x => x.Service).ToArray();
            var serviceCounts = topServices.Select(x => x.Count).ToArray();

            TopServicesChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Количество",
                    Values = new ChartValues<int>(serviceCounts)
                }
            };
            TopServicesChart.AxisX[0].Labels = serviceLabels;
        }

        private void LoadClientsAndEmployees(DateTime startDate, DateTime endDate)
        {
            // 4.1 Распределение клиентов по возрасту
            var allClients = App.Context.dm_Users
                .Where(u => u.dm_Roli.Rol == "Клиент")
                .ToList();

            var clientsByGroup = allClients
                .GroupBy(u => GetAgeGroup(u.Data_rojdeniya))
                .Select(g => new { AgeGroup = g.Key, Count = g.Count() })
                .OrderBy(x => x.AgeGroup)
                .ToList();

            var ageLabels = clientsByGroup.Select(x => x.AgeGroup).ToArray();
            var ageCounts = clientsByGroup.Select(x => x.Count).ToArray();

            ClientsByAgeChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Клиенты",
                    Values = new ChartValues<int>(ageCounts)
                }
            };
            ClientsByAgeChart.AxisX[0].Labels = ageLabels;


            // 4.2 Статистика выполнения заказов по сотрудникам
            var ordersByEmployee = App.Context.dm_Zakazi
                .Include("dm_Users")
                .Where(z => z.Data_sozdaniya >= startDate && z.Data_sozdaniya <= endDate && z.Ispolnitel != null)
                .GroupBy(z => z.dm_Users.Familiya + " " +
                              z.dm_Users.Imya.Substring(0, 1) + "." +
                              (z.dm_Users.Otchestvo != null && z.dm_Users.Otchestvo.Length > 0
                                 ? z.dm_Users.Otchestvo.Substring(0, 1) + "."
                                 : ""))
                .Select(g => new { Employee = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var empLabels = ordersByEmployee.Select(x => x.Employee).ToArray();
            var empCounts = ordersByEmployee.Select(x => x.Count).ToArray();

            OrdersByEmployeeChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Заказы",
                    Values = new ChartValues<int>(empCounts)
                }
            };
            OrdersByEmployeeChart.AxisX[0].Labels = empLabels;
        }

        private void LoadDefectsAnalysis(DateTime startDate, DateTime endDate)
        {
            // 5. Топ-5 самых частых дефектов
            var topDefects = App.Context.dm_Defekti
                .Include("dm_Uzli_avto")
                .Include("dm_Zakazi")
                .Where(d => d.dm_Zakazi.Data_sozdaniya >= startDate && d.dm_Zakazi.Data_sozdaniya <= endDate)
                .GroupBy(d => d.dm_Uzli_avto.Nazvanie_uzla_avto)
                .Select(g => new { Defect = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var defectLabels = topDefects.Select(x => x.Defect).ToArray();
            var defectCounts = topDefects.Select(x => x.Count).ToArray();

            TopDefectsChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Количество",
                    Values = new ChartValues<int>(defectCounts)
                }
            };
            TopDefectsChart.AxisX[0].Labels = defectLabels;
        }

        private void LoadFinancialReports(DateTime startDate, DateTime endDate)
        {
            // Получаем все оплаченные заказы в диапазоне
            var paidOrders = App.Context.dm_Zakazi
                .Include("dm_Raboti_v_zakaze.dm_Raboti")
                .Include("dm_Detali_v_zakaze.dm_Detali")
                .Where(z => z.Oplata
                         && z.Data_sozdaniya >= startDate
                         && z.Data_sozdaniya <= endDate)
                .ToList();

            // 6.1 Доходы: работы + детали (себестоимость)
            decimal servicesIncome = paidOrders.Sum(z =>
                z.dm_Raboti_v_zakaze.Sum(r => (r.Zakrep_stoimost.HasValue ? r.Zakrep_stoimost.Value : r.dm_Raboti.Stoimost)));
            decimal partsIncome = paidOrders.Sum(z =>
                z.dm_Detali_v_zakaze
                 .Where(d => !d.Detal_klienta)
                 .Sum(d => (d.Zakrep_cena.HasValue ? d.Zakrep_cena.Value : d.dm_Detali.Cena) * d.Kolichestvo));
            decimal totalIncome = servicesIncome + partsIncome;

            IncomeSourcesChart.Series = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Услуги",
                    Values = new ChartValues<decimal> { servicesIncome },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "Детали",
                    Values = new ChartValues<decimal> { partsIncome },
                    DataLabels = true
                }
            };
            TotalIncomeText.Text = $"Общий доход: {totalIncome:N0} ₽";


            // 6.2 Расходы: только себестоимость деталей, купленных сервисом
            decimal totalExpenses = paidOrders.Sum(z =>
                z.dm_Detali_v_zakaze
                 .Where(d => !d.Detal_klienta)
                 .Sum(d => d.dm_Detali.Cena * d.Kolichestvo));
            ExpensesChart.Series = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Детали (себестоимость)",
                    Values = new ChartValues<decimal> { totalExpenses },
                    DataLabels = true
                }
            };
            TotalExpensesText.Text = $"Общие расходы: {totalExpenses:N0} ₽";

            // 6.3 Прибыль = доход – расходы
            decimal profit = totalIncome - totalExpenses;
            ProfitText.Text = $"Прибыль: {profit:N0} ₽";


            // 6.4 Финансовая отчётность за текущий год
            int thisYear = DateTime.Now.Year;
            var yearlyData = App.Context.dm_Zakazi
                .Include("dm_Raboti_v_zakaze.dm_Raboti")
                .Include("dm_Detali_v_zakaze.dm_Detali")
                .Where(z => z.Oplata && z.Data_sozdaniya.Year == thisYear)
                .ToList()
                .GroupBy(z => z.Data_sozdaniya.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Income = g.Sum(z =>
                        z.dm_Raboti_v_zakaze.Sum(r => (r.Zakrep_stoimost.HasValue ? r.Zakrep_stoimost.Value : r.dm_Raboti.Stoimost))
                        + z.dm_Detali_v_zakaze.Where(d => !d.Detal_klienta)
                                              .Sum(d => (d.Zakrep_cena.HasValue ? d.Zakrep_cena.Value : d.dm_Detali.Cena) * d.Kolichestvo)
                    ),
                    Expense = g.Sum(z =>
                        z.dm_Detali_v_zakaze.Where(d => !d.Detal_klienta)
                                            .Sum(d => d.dm_Detali.Cena * d.Kolichestvo)
                    )
                })
                .OrderBy(x => x.Month)
                .ToList();

            // Заполняем массивы по всем 12 месяцам
            var monthlyIncome = new decimal[12];
            var monthlyExpense = new decimal[12];
            var monthlyProfit = new decimal[12];
            var yearLabels = new string[12];
            for (int m = 1; m <= 12; m++)
            {
                yearLabels[m - 1] = $"{thisYear}-{m:00}";
                var data = yearlyData.FirstOrDefault(x => x.Month == m);
                if (data != null)
                {
                    monthlyIncome[m - 1] = data.Income;
                    monthlyExpense[m - 1] = data.Expense;
                    monthlyProfit[m - 1] = data.Income - data.Expense;
                }
            }

            YearlyFinanceChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Доход",
                    Values = new ChartValues<decimal>(monthlyIncome)
                },
                new LineSeries
                {
                    Title = "Расход",
                    Values = new ChartValues<decimal>(monthlyExpense)
                },
                new LineSeries
                {
                    Title = "Прибыль",
                    Values = new ChartValues<decimal>(monthlyProfit)
                }
            };
            YearlyFinanceChart.AxisX[0].Labels = yearLabels;
        }

        private string GetAgeGroup(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now.Month < birthDate.Month ||
                (DateTime.Now.Month == birthDate.Month && DateTime.Now.Day < birthDate.Day))
            {
                age--;
            }

            if (age < 18) return "<18";
            if (age < 25) return "18–24";
            if (age < 30) return "25–29";
            if (age < 35) return "30–34";
            if (age < 40) return "35–39";
            if (age < 45) return "40–44";
            if (age < 50) return "45–49";
            if (age < 55) return "50–54";
            if (age < 60) return "55–59";
            return "60+";
        }


    }
}
