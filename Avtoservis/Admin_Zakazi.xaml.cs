using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для Admin_Zakazi.xaml
    /// </summary>
    public partial class Admin_Zakazi : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_zakaza";
        private bool isAscending = true;
        private Border _currentPopup;
        private Button _currentButton; // Добавляем это поле
        private DateTime? _selectedDateFrom;
        private DateTime? _selectedDateTo;
        private decimal? _minSum = null;
        private decimal? _maxSum = null;

        // Фильтры
        private List<string> selectedStatuses = new List<string>();
        private List<int> selectedWorkPlaces = new List<int>();
        private List<bool?> selectedPaymentStatuses = new List<bool?>();
        private DispatcherTimer dateFilterTimer;
        private const int DateFilterDelay = 500;

        private int? _employeeId; // Добавляем поле для хранения ID сотрудника
        private int? _clientId; // Добавляем поле для хранения ID клиента

        // Внутри класса Admin_Zakazi:
        private bool _showBackButton;

        public Admin_Zakazi(int? employeeId = null, int? clientId = null)
        {
            bool userIsEmployee = App.CurrentUser != null && App.CurrentUser.Rol == 2;

            // 1) Если зашёл сотрудник и одновременно передан clientId – фильтруем по обоим: 
            //    список заказов этого клиента ТОЛЬКО по этому исполнителю.
            if (userIsEmployee && clientId.HasValue)
            {
                _employeeId = App.CurrentUser.ID_user;
                _clientId = clientId.Value;
                _showBackButton = true;
            }
            // 2) Если зашёл сотрудник, но clientId не передавали и employeeId=null – 
            //    просто показываем все свои заказы (back-button не нужен).
            else if (userIsEmployee && !employeeId.HasValue)
            {
                _employeeId = App.CurrentUser.ID_user;
                _showBackButton = false;
            }
            // 3) Если админ передал конкретный employeeId (и clientId отсутствует) – 
            //    фильтруем по этому сотруднику и показываем «Назад».
            else if (employeeId.HasValue && !clientId.HasValue)
            {
                _employeeId = employeeId.Value;
                _showBackButton = true;
            }
            // 4) Если передали clientId (без employeeId — значит, зашёл админ) – 
            //    показываем все заказы этого клиента, back-button не нужен.
            else if (clientId.HasValue)
            {
                _clientId = clientId.Value;
                _showBackButton = false;
            }
            // 5) Иначе (админ без фильтров) – показываем все заказы, back-button не нужен.
            else
            {
                _employeeId = null;
                _clientId = null;
                _showBackButton = false;
            }

            InitializeComponent();

            // Настройка кнопки «Назад» и заголовка
            if (_showBackButton)
            {
                BtnBack.Visibility = Visibility.Visible;
                TitleText.Margin = new Thickness(90, 0, 0, 30);

                if (_clientId.HasValue)
                {
                    var cli = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == _clientId.Value);
                    if (cli != null)
                        TitleText.Text = $"Заказ-наряды. Клиент: {cli.Familiya} {cli.Imya}";
                }
                else if (_employeeId.HasValue)
                {
                    var emp = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == _employeeId.Value);
                    if (emp != null)
                        TitleText.Text = $"Заказ-наряды. Сотрудник: {emp.Familiya} {emp.Imya}";
                }
            }
            else if (_clientId.HasValue)
            {
                BtnBack.Visibility = Visibility.Visible;
                TitleText.Margin = new Thickness(90, 0, 0, 30);

                var cli = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == _clientId);
                if (cli != null)
                {
                    TitleText.Text = $"Заказ-наряды. Клиент: {cli.Familiya} {cli.Imya}";
                }
            }
            else
            {
                BtnBack.Visibility = Visibility.Collapsed;
                TitleText.Margin = new Thickness(40, 0, 0, 30);
                TitleText.Text = "Все заказ-наряды";
            }

            // Вызываем остальную логику уже после полной отрисовки
            this.Loaded += (s, e) =>
            {
                LoadFilters();
                LoadZakazi();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            };

            ZakaziList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();

            // Таймер для отложенной фильтрации по датам
            dateFilterTimer = new DispatcherTimer();
            dateFilterTimer.Interval = TimeSpan.FromMilliseconds(DateFilterDelay);
            dateFilterTimer.Tick += DateFilterTimer_Tick;
        }

        private DateTime? _selectedAcceptDateFrom;
        private DateTime? _selectedAcceptDateTo;
        private DateTime? _selectedIssueDateFrom;
        private DateTime? _selectedIssueDateTo;
        private DateTime? _selectedCreateDateFrom;
        private DateTime? _selectedCreateDateTo;


        private void LoadZakazi()
        {
            if (ZakaziList == null || App.Context == null)
            {
                Debug.WriteLine("ZakaziList или контекст базы данных не инициализированы");
                return;
            }

            try
            {
                var query = App.Context.dm_Zakazi
                    .Include("dm_Users")
                    .Include("dm_Users1")
                    .Include("dm_Avtomobili")
                    .Include("dm_Avtomobili.dm_Komplektacii_avto")
                    .Include("dm_Avtomobili.dm_Komplektacii_avto.dm_Modeli_avto")
                    .Include("dm_Rabochie_mesta")
                    .Include("dm_Raboti_v_zakaze")
                    .Include("dm_Raboti_v_zakaze.dm_Raboti")
                    .Include("dm_Detali_v_zakaze")
                    .Include("dm_Detali_v_zakaze.dm_Detali")
                    .AsQueryable();

                // Фильтрация по сотруднику
                if (_employeeId.HasValue)
                {
                    query = query.Where(z => z.Ispolnitel == _employeeId.Value);
                }

                // Фильтрация по клиенту
                if (_clientId.HasValue)
                {
                    query = query.Where(z => z.Klient == _clientId.Value);
                }

                // Применение поиска (остается без изменений)
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    var searchText = currentSearchText.ToLower();
                    // На это (сначала получаем список, затем фильтруем):
                    query = query.ToList().Where(z =>
                        z.ID_zakaza.ToString().Contains(searchText) ||
                        z.Status.ToLower().Contains(searchText) ||
                        (z.dm_Users1.Familiya + " " + z.dm_Users1.Imya + " " + z.dm_Users1.Otchestvo).ToLower().Contains(searchText) ||
                        z.Data_i_vremya_priema_avto.ToString().Contains(searchText) ||
                        z.Data_i_vremya_vidachi_avto.ToString().Contains(searchText) ||
                        z.Data_sozdaniya.ToString().Contains(searchText) ||
                        (z.dm_Avtomobili != null && z.dm_Avtomobili.dm_Komplektacii_avto != null &&
                         z.dm_Avtomobili.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                         z.dm_Avtomobili.dm_Komplektacii_avto.dm_Modeli_avto.Model.ToLower().Contains(searchText)) ||
                        CalculateOrderSum(z).ToString().Contains(searchText)).AsQueryable();
                }

                // Применение фильтров по статусам, рабочим местам и оплате (остается без изменений)
                if (selectedStatuses.Count > 0)
                {
                    query = query.Where(z => selectedStatuses.Contains(z.Status));
                }

                if (selectedWorkPlaces.Count > 0)
                {
                    query = query.Where(z => selectedWorkPlaces.Contains(z.Rabochee_mesto));
                }

                if (selectedPaymentStatuses.Count > 0)
                {
                    query = query.Where(z => selectedPaymentStatuses.Contains(z.Oplata));
                }

                // Измененная фильтрация по дате приёма авто
                if (_selectedAcceptDateFrom.HasValue)
                {
                    query = query.Where(z => z.Data_i_vremya_priema_avto >= _selectedAcceptDateFrom.Value);
                }
                if (_selectedAcceptDateTo.HasValue)
                {
                    // Добавляем 1 день, чтобы включить все записи до конца указанного дня
                    var endDate = _selectedAcceptDateTo.Value.AddDays(1);
                    query = query.Where(z => z.Data_i_vremya_priema_avto < endDate);
                }

                // Измененная фильтрация по дате выдачи авто
                if (_selectedIssueDateFrom.HasValue)
                {
                    query = query.Where(z => z.Data_i_vremya_vidachi_avto >= _selectedIssueDateFrom.Value);
                }
                if (_selectedIssueDateTo.HasValue)
                {
                    // Добавляем 1 день, чтобы включить все записи до конца указанного дня
                    var endDate = _selectedIssueDateTo.Value.AddDays(1);
                    query = query.Where(z => z.Data_i_vremya_vidachi_avto < endDate);
                }

                // Измененная фильтрация по дате создания
                if (_selectedCreateDateFrom.HasValue)
                {
                    query = query.Where(z => z.Data_sozdaniya >= _selectedCreateDateFrom.Value);
                }
                if (_selectedCreateDateTo.HasValue)
                {
                    // Добавляем 1 день, чтобы включить все записи до конца указанного дня
                    var endDate = _selectedCreateDateTo.Value.AddDays(1);
                    query = query.Where(z => z.Data_sozdaniya < endDate);
                }

                // Фильтр по сумме
                if (_minSum.HasValue)
                {
                    var zakaziWithSum = query.ToList().Where(z => CalculateOrderSum(z) >= _minSum.Value);
                    query = zakaziWithSum.AsQueryable();
                }
                if (_maxSum.HasValue)
                {
                    var zakaziWithSum = query.ToList().Where(z => CalculateOrderSum(z) <= _maxSum.Value);
                    query = zakaziWithSum.AsQueryable();
                }

                // Остальная часть метода остается без изменений
                totalItems = query.Count();

                // Применяем сортировку
                IQueryable<Entities.dm_Zakazi> sortedQuery;
                switch (currentSortColumn)
                {
                    case "Data_i_vremya_priema_avto":
                        sortedQuery = isAscending ?
                            query.OrderBy(z => z.Data_i_vremya_priema_avto) :
                            query.OrderByDescending(z => z.Data_i_vremya_priema_avto);
                        break;
                    case "Data_i_vremya_vidachi_avto":
                        sortedQuery = isAscending ?
                            query.OrderBy(z => z.Data_i_vremya_vidachi_avto) :
                            query.OrderByDescending(z => z.Data_i_vremya_vidachi_avto);
                        break;
                    case "Summa":
                        var allZakazi = query.ToList();
                        foreach (var zakaz in allZakazi)
                        {
                            zakaz.Summa = CalculateOrderSum(zakaz);
                        }
                        var sortedZakazi = isAscending ?
                            allZakazi.OrderBy(z => z.Summa).ToList() :
                            allZakazi.OrderByDescending(z => z.Summa).ToList();

                        var pagedZakazi = sortedZakazi
                            .Skip((currentPage - 1) * itemsPerPage)
                            .Take(itemsPerPage)
                            .ToList();

                        ZakaziList.ItemsSource = pagedZakazi;
                        return;
                    default:
                        sortedQuery = isAscending ?
                            query.OrderBy(z => z.ID_zakaza) :
                            query.OrderByDescending(z => z.ID_zakaza);
                        break;
                }

                var resultZakazi = sortedQuery
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                foreach (var zakaz in resultZakazi)
                {
                    zakaz.Summa = CalculateOrderSum(zakaz);
                }

                ZakaziList.ItemsSource = resultZakazi;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateOrderSum(Entities.dm_Zakazi zakaz)
        {
            decimal sum = 0;

            // Загружаем связанные данные, если они еще не загружены
            if (zakaz.dm_Raboti_v_zakaze == null)
            {
                App.Context.Entry(zakaz).Collection(z => z.dm_Raboti_v_zakaze).Load();
            }

            // Сумма работ
            if (zakaz.dm_Raboti_v_zakaze != null)
            {
                foreach (var rabota in zakaz.dm_Raboti_v_zakaze)
                {
                    if (rabota.dm_Raboti == null)
                    {
                        App.Context.Entry(rabota).Reference(r => r.dm_Raboti).Load();
                    }

                    sum += rabota.Zakrep_stoimost ?? (rabota.dm_Raboti?.Stoimost ?? 0);
                }
            }

            // Загружаем связанные данные для деталей, если они еще не загружены
            if (zakaz.dm_Detali_v_zakaze == null)
            {
                App.Context.Entry(zakaz).Collection(z => z.dm_Detali_v_zakaze).Load();
            }

            // Сумма деталей (только если Detal_klienta = false)
            if (zakaz.dm_Detali_v_zakaze != null)
            {
                foreach (var detal in zakaz.dm_Detali_v_zakaze)
                {
                    if (!detal.Detal_klienta)
                    {
                        if (detal.dm_Detali == null)
                        {
                            App.Context.Entry(detal).Reference(d => d.dm_Detali).Load();
                        }

                        sum += (detal.Zakrep_cena ?? (detal.dm_Detali?.Cena ?? 0)) * detal.Kolichestvo;
                    }
                }
            }

            return sum;
        }

        private void LoadFilters()
        {
            // Загрузка статусов
            StatusFilterPanel.Children.Clear();
            var statuses = new List<string> { "Принят", "В работе", "Завершён", "Отменён" };
            foreach (var status in statuses)
            {
                var checkBox = new CheckBox
                {
                    Content = status,
                    Tag = status,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += StatusCheckBox_Checked;
                checkBox.Unchecked += StatusCheckBox_Unchecked;
                StatusFilterPanel.Children.Add(checkBox);
            }

            // Загрузка рабочих мест
            var workPlaces = App.Context.dm_Rabochie_mesta.ToList();
            WorkPlaceFilterPanel.Children.Clear();
            foreach (var wp in workPlaces)
            {
                var checkBox = new CheckBox
                {
                    Content = wp.Rabochee_mesto,
                    Tag = wp.ID_rabochego_mesta,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += WorkPlaceCheckBox_Checked;
                checkBox.Unchecked += WorkPlaceCheckBox_Unchecked;
                WorkPlaceFilterPanel.Children.Add(checkBox);
            }

            // Загрузка статусов оплаты
            PaymentFilterPanel.Children.Clear();
            var paymentStatuses = new List<Tuple<string, bool?>>
            {
                new Tuple<string, bool?>("Оплачен", true),
                new Tuple<string, bool?>("Не оплачен", false)
            };
            foreach (var ps in paymentStatuses)
            {
                var checkBox = new CheckBox
                {
                    Content = ps.Item1,
                    Tag = ps.Item2,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += PaymentCheckBox_Checked;
                checkBox.Unchecked += PaymentCheckBox_Unchecked;
                PaymentFilterPanel.Children.Add(checkBox);
            }
        }

        // Обработчики событий для чекбоксов фильтров
        private void StatusCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedStatuses.Add(checkBox.Tag.ToString());
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void StatusCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedStatuses.Remove(checkBox.Tag.ToString());
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void WorkPlaceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedWorkPlaces.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void WorkPlaceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedWorkPlaces.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void PaymentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedPaymentStatuses.Add((bool?)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void PaymentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedPaymentStatuses.Remove((bool?)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            LoadZakazi();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void CheckFiltersActive()
        {
            if (BtnResetFilters == null) return; // Добавьте эту проверку

            bool hasActiveFilters = selectedStatuses.Count > 0 ||
                                   selectedWorkPlaces.Count > 0 ||
                                   selectedPaymentStatuses.Count > 0 ||
                                   _selectedAcceptDateFrom.HasValue ||
                                   _selectedAcceptDateTo.HasValue ||
                                   _selectedIssueDateFrom.HasValue ||
                                   _selectedIssueDateTo.HasValue ||
                                   _selectedCreateDateFrom.HasValue ||
                                   _selectedCreateDateTo.HasValue ||
                                   _minSum.HasValue ||
                                   _maxSum.HasValue;

            // Проверяем текстовые поля только если они не null
            if (SumFromTextBox != null && SumToTextBox != null)
            {
                hasActiveFilters = hasActiveFilters ||
                                 (SumFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(SumFromTextBox.Text)) ||
                                 (SumToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(SumToTextBox.Text));
            }

            if (AcceptDateFromTextBox != null && AcceptDateToTextBox != null &&
                IssueDateFromTextBox != null && IssueDateToTextBox != null &&
                CreateDateFromTextBox != null && CreateDateToTextBox != null)
            {
                hasActiveFilters = hasActiveFilters ||
                                 (AcceptDateFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(AcceptDateFromTextBox.Text)) ||
                                 (AcceptDateToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(AcceptDateToTextBox.Text)) ||
                                 (IssueDateFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(IssueDateFromTextBox.Text)) ||
                                 (IssueDateToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(IssueDateToTextBox.Text)) ||
                                 (CreateDateFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(CreateDateFromTextBox.Text)) ||
                                 (CreateDateToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(CreateDateToTextBox.Text));
            }

            BtnResetFilters.IsEnabled = hasActiveFilters;
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем другие попапы, если они открыты
            if (_currentPopup != null)
            {
                ClosePopup();
            }
            if (itemsPerPagePopup != null && itemsPerPagePopup.IsOpen)
            {
                itemsPerPagePopup.IsOpen = false;
            }

            // Переключаем видимость панели фильтров
            FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void BtnCloseFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility = Visibility.Collapsed;

            // Закрываем все всплывающие окна внутри блока фильтров
            if (_currentPopup != null)
            {
                ClosePopup();
            }
            if (itemsPerPagePopup != null && itemsPerPagePopup.IsOpen)
            {
                itemsPerPagePopup.IsOpen = false;
            }
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Сброс всех фильтров
            foreach (CheckBox checkBox in StatusFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in WorkPlaceFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in PaymentFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }

            selectedStatuses.Clear();
            selectedWorkPlaces.Clear();
            selectedPaymentStatuses.Clear();

            // Сброс дат
            _selectedAcceptDateFrom = null;
            _selectedAcceptDateTo = null;
            _selectedIssueDateFrom = null;
            _selectedIssueDateTo = null;
            _selectedCreateDateFrom = null;
            _selectedCreateDateTo = null;

            // Сброс суммы
            _minSum = null;
            _maxSum = null;

            // Сброс текстовых полей
            AcceptDateFromTextBox.Text = "от";
            AcceptDateFromTextBox.Foreground = Brushes.Gray;
            AcceptDateToTextBox.Text = "до";
            AcceptDateToTextBox.Foreground = Brushes.Gray;
            IssueDateFromTextBox.Text = "от";
            IssueDateFromTextBox.Foreground = Brushes.Gray;
            IssueDateToTextBox.Text = "до";
            IssueDateToTextBox.Foreground = Brushes.Gray;
            CreateDateFromTextBox.Text = "от";
            CreateDateFromTextBox.Foreground = Brushes.Gray;
            CreateDateToTextBox.Text = "до";
            CreateDateToTextBox.Foreground = Brushes.Gray;
            SumFromTextBox.Text = "от";
            SumFromTextBox.Foreground = Brushes.Gray;
            SumToTextBox.Text = "до";
            SumToTextBox.Foreground = Brushes.Gray;

            BtnResetFilters.IsEnabled = false;
            ApplyFilters();
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header)
            {
                string newSortColumn;
                switch (header.Content.ToString())
                {
                    case "ID":
                        newSortColumn = "ID_zakaza";
                        break;
                    case "Принят":
                        newSortColumn = "Data_i_vremya_priema_avto";
                        break;
                    case "Закрыт":
                        newSortColumn = "Data_i_vremya_vidachi_avto";
                        break;
                    case "Сумма":
                        newSortColumn = "Summa";
                        break;
                    default:
                        newSortColumn = currentSortColumn;
                        break;
                }

                if (currentSortColumn == newSortColumn)
                {
                    isAscending = !isAscending;
                }
                else
                {
                    currentSortColumn = newSortColumn;
                    isAscending = true;
                }

                LoadZakazi();
                UpdatePaginationInfo();
                UpdateSortIndicators();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(ZakaziList))
            {
                if (header.Template.FindName("SortArrow", header) is System.Windows.Shapes.Path sortArrow)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_zakaza";
                            break;
                        case "Принят":
                            isCurrentSortColumn = currentSortColumn == "Data_i_vremya_priema_avto";
                            break;
                        case "Закрыт":
                            isCurrentSortColumn = currentSortColumn == "Data_i_vremya_vidachi_avto";
                            break;
                        case "Сумма":
                            isCurrentSortColumn = currentSortColumn == "Summa";
                            break;
                    }

                    sortArrow.Visibility = isCurrentSortColumn ? Visibility.Visible : Visibility.Collapsed;
                    if (isCurrentSortColumn)
                    {
                        sortArrow.RenderTransform = new RotateTransform(isAscending ? 0 : 180, 0.5, 0.5);
                    }
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var addEditWindow = new AddEdit_Zakaz(_employeeId, _clientId);
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Zakazi.Count();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadZakazi();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentZakaz = ZakaziList.SelectedItem as Entities.dm_Zakazi;

            if (currentZakaz == null)
            {
                MessageBox.Show("Пожалуйста, выберите заказ для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить заказ (ID: {currentZakaz.ID_zakaza})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем связанные записи
                    var raboti = App.Context.dm_Raboti_v_zakaze.Where(r => r.ID_zakaza == currentZakaz.ID_zakaza).ToList();
                    var detali = App.Context.dm_Detali_v_zakaze.Where(d => d.ID_zakaza == currentZakaz.ID_zakaza).ToList();
                    var defekti = App.Context.dm_Defekti.Where(d => d.Zakaz == currentZakaz.ID_zakaza).ToList();
                    var foto = App.Context.dm_Foto_v_zakaze.Where(f => f.ID_zakaza == currentZakaz.ID_zakaza).ToList();

                    App.Context.dm_Raboti_v_zakaze.RemoveRange(raboti);
                    App.Context.dm_Detali_v_zakaze.RemoveRange(detali);
                    App.Context.dm_Defekti.RemoveRange(defekti);
                    App.Context.dm_Foto_v_zakaze.RemoveRange(foto);
                    App.Context.dm_Zakazi.Remove(currentZakaz);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Zakazi.Count();
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadZakazi();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Заказ успешно удален!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении заказа:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadZakazi();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedZakaz = (sender as Button).DataContext as Entities.dm_Zakazi;

            if (selectedZakaz == null)
            {
                MessageBox.Show("Выберите заказ для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Загружаем полные данные заказа
            var zakazToEdit = App.Context.dm_Zakazi
                .Include("dm_Users")
                .Include("dm_Avtomobili")
                .Include("dm_Raboti_v_zakaze")
                .Include("dm_Detali_v_zakaze")
                .FirstOrDefault(z => z.ID_zakaza == selectedZakaz.ID_zakaza);

            if (zakazToEdit != null)
            {
                var addEditWindow = new AddEdit_Zakaz(zakazToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadZakazi(); 
                }
            }
            else
            {
                MessageBox.Show("Заказ не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime lastPopupInteractionTime = DateTime.MinValue;

        private void ItemsPerPageButton_Click(object sender, RoutedEventArgs e)
        {
            if ((DateTime.Now - lastPopupInteractionTime).TotalMilliseconds < 200)
                return;

            lastPopupInteractionTime = DateTime.Now;

            var button = sender as Button;

            if (itemsPerPagePopup != null && itemsPerPagePopup.IsOpen && currentButton == button)
            {
                itemsPerPagePopup.IsOpen = false;
                return;
            }

            if (itemsPerPagePopup != null)
            {
                itemsPerPagePopup.IsOpen = false;
            }

            currentButton = button;

            itemsPerPagePopup = new Popup
            {
                PlacementTarget = currentButton,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                Width = currentButton.ActualWidth,
                IsOpen = true
            };

            itemsPerPagePopup.Closed += (s, args) =>
            {
                lastPopupInteractionTime = DateTime.Now;
            };

            var stackPanel = new StackPanel { Background = Brushes.White };

            int[] items = { 20, 50, 100, 200 };
            foreach (var item in items)
            {
                var btn = new Button
                {
                    Content = item.ToString(),
                    Style = (Style)FindResource("ComboBoxItemStyle"),
                    Tag = item
                };
                btn.Click += (s, args) =>
                {
                    itemsPerPage = (int)((Button)s).Tag;
                    currentButton.Content = itemsPerPage.ToString();
                    currentPage = 1;
                    itemsPerPagePopup.IsOpen = false;
                    LoadZakazi();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();
                };
                stackPanel.Children.Add(btn);
            }

            itemsPerPagePopup.Child = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Child = stackPanel
            };
        }

        private void UpdatePaginationInfo()
        {
            try
            {
                if (PaginationInfo == null)
                {
                    Debug.WriteLine("PaginationInfo is null");
                    return;
                }

                int start = (currentPage - 1) * itemsPerPage + 1;
                int end = Math.Min(currentPage * itemsPerPage, totalItems);

                PaginationInfo.Text = $"{start}-{end} из {totalItems}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdatePaginationInfo: {ex.Message}");
                // Можно добавить MessageBox для пользователя в режиме отладки
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "Поиск...")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Поиск...";
                textBox.Foreground = Brushes.Gray;
                SearchTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            }
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;

            // Проверяем, был ли клик внутри панели фильтров или на кнопке фильтра
            bool clickInsideFilter = IsChildOf(originalSource, FilterPanel) || IsChildOf(originalSource, BtnFilter);

            // Проверяем, был ли клик внутри календаря
            bool clickInsideCalendar = _currentPopup != null && IsChildOf(originalSource, _currentPopup);

            // Если клик был не внутри панели фильтров и не на кнопке фильтра, и панель открыта - закрываем её
            if (!clickInsideFilter && !clickInsideCalendar && FilterPanel.Visibility == Visibility.Visible)
            {
                FilterPanel.Visibility = Visibility.Collapsed;
            }

            // Остальная логика обработки клика (поиск и пагинация)
            bool clickInsideSearchBox = IsChildOf(originalSource, SearchTextBox);

            if (!clickInsideSearchBox)
            {
                if (SearchTextBox.IsFocused)
                {
                    Keyboard.ClearFocus();

                    if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                    {
                        SearchTextBox.Text = "Поиск...";
                        SearchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                        SearchTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    }
                }
            }
            else
            {
                if (SearchTextBox.Text == "Поиск...")
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
                    SearchTextBox.BorderBrush = new SolidColorBrush(Colors.Blue);
                }
            }

            bool isClickInside = IsChildOf(originalSource, currentButton) ||
                                (itemsPerPagePopup != null && IsChildOf(originalSource, itemsPerPagePopup));

            if (!isClickInside && itemsPerPagePopup != null && itemsPerPagePopup.IsOpen)
            {
                itemsPerPagePopup.IsOpen = false;
            }
        }



        private void UpdatePaginationButtons()
        {
            try
            {
                if (BtnPrevPage == null || BtnNextPage == null)
                {
                    Debug.WriteLine("Кнопки пагинации не найдены");
                    return;
                }

                var activeStyle = FindResource("PaginationButtonStyle") as Style;
                var disabledStyle = FindResource("PaginationButtonDisabledStyle") as Style;

                if (activeStyle == null || disabledStyle == null)
                {
                    Debug.WriteLine("Стили пагинации не найдены");
                    return;
                }

                // Обновление кнопки "Назад"
                BtnPrevPage.Style = currentPage > 1 ? activeStyle : disabledStyle;
                BtnPrevPage.IsEnabled = currentPage > 1;

                // Обновление кнопки "Вперед"
                int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
                BtnNextPage.Style = currentPage < totalPages ? activeStyle : disabledStyle;
                BtnNextPage.IsEnabled = currentPage < totalPages;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления кнопок пагинации: {ex.Message}");
            }
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadZakazi();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadZakazi();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox.Text == "Поиск...")
            {
                currentSearchText = "";
                return;
            }

            currentSearchText = textBox.Text;
            currentPage = 1;
            LoadZakazi();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        // Обработчики для фильтрации по дате
        private void DateFilterTimer_Tick(object sender, EventArgs e)
        {
            dateFilterTimer.Stop();
            UpdateDateFilterValues();
            ApplyFilters();
        }

        private void UpdateDateFilterValues()
        {
            // Дата приёма авто
            if (AcceptDateFromTextBox != null && AcceptDateFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(AcceptDateFromTextBox.Text))
            {
                if (DateTime.TryParse(AcceptDateFromTextBox.Text, out DateTime value))
                    _selectedAcceptDateFrom = value;
                else
                    _selectedAcceptDateFrom = null;
            }
            else
            {
                _selectedAcceptDateFrom = null;
            }

            if (AcceptDateToTextBox != null && AcceptDateToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(AcceptDateToTextBox.Text))
            {
                if (DateTime.TryParse(AcceptDateToTextBox.Text, out DateTime value))
                    _selectedAcceptDateTo = value;
                else
                    _selectedAcceptDateTo = null;
            }
            else
            {
                _selectedAcceptDateTo = null;
            }

            // Дата выдачи авто
            if (IssueDateFromTextBox != null && IssueDateFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(IssueDateFromTextBox.Text))
            {
                if (DateTime.TryParse(IssueDateFromTextBox.Text, out DateTime value))
                    _selectedIssueDateFrom = value;
                else
                    _selectedIssueDateFrom = null;
            }
            else
            {
                _selectedIssueDateFrom = null;
            }

            if (IssueDateToTextBox != null && IssueDateToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(IssueDateToTextBox.Text))
            {
                if (DateTime.TryParse(IssueDateToTextBox.Text, out DateTime value))
                    _selectedIssueDateTo = value;
                else
                    _selectedIssueDateTo = null;
            }
            else
            {
                _selectedIssueDateTo = null;
            }

            // Дата создания
            if (CreateDateFromTextBox != null && CreateDateFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(CreateDateFromTextBox.Text))
            {
                if (DateTime.TryParse(CreateDateFromTextBox.Text, out DateTime value))
                    _selectedCreateDateFrom = value;
                else
                    _selectedCreateDateFrom = null;
            }
            else
            {
                _selectedCreateDateFrom = null;
            }

            if (CreateDateToTextBox != null && CreateDateToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(CreateDateToTextBox.Text))
            {
                if (DateTime.TryParse(CreateDateToTextBox.Text, out DateTime value))
                    _selectedCreateDateTo = value;
                else
                    _selectedCreateDateTo = null;
            }
            else
            {
                _selectedCreateDateTo = null;
            }
        }

        private void DateTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void DateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = textBox.Tag.ToString();
                    textBox.Foreground = Brushes.Gray;
                }
                UpdateDateFilterValues();
                ApplyFilters();
            }
        }

        private void DateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dateFilterTimer != null)
            {
                dateFilterTimer.Stop();
                dateFilterTimer.Start();
            }
            CheckFiltersActive(); // Добавьте эту строку
        }

        // Обработчики для фильтрации по сумме
        private void SumTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void SumTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = textBox.Tag.ToString();
                    textBox.Foreground = Brushes.Gray;
                }
                UpdateSumFilterValues();
                ApplyFilters();
            }
        }

        private void SumTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SumFromTextBox == null || SumToTextBox == null) return;

            UpdateSumFilterValues();
            CheckFiltersActive();
            ApplyFilters();
        }

        private void UpdateSumFilterValues()
        {
            try
            {
                if (SumFromTextBox == null || SumToTextBox == null) return;

                var fromText = SumFromTextBox.Text;
                var toText = SumToTextBox.Text;

                if (fromText != "от" && !string.IsNullOrWhiteSpace(fromText))
                {
                    fromText = fromText.Replace(".", ",");
                    if (decimal.TryParse(fromText, out decimal value))
                        _minSum = value;
                    else
                        _minSum = null;
                }
                else
                {
                    _minSum = null;
                }

                if (toText != "до" && !string.IsNullOrWhiteSpace(toText))
                {
                    toText = toText.Replace(".", ",");
                    if (decimal.TryParse(toText, out decimal value))
                        _maxSum = value;
                    else
                        _maxSum = null;
                }
                else
                {
                    _maxSum = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в UpdateSumFilterValues: {ex.Message}");
                _minSum = null;
                _maxSum = null;
            }
        }

        private double[] columnRatios = new double[] { 0.05, 0.1, 0.1, 0.2, 0.2, 0.1, 0.15, 0.1 };

        private void UpdateGridViewColumnWidths()
        {
            if (ZakaziList.View is GridView gridView)
            {
                double totalWidth = ZakaziList.ActualWidth;
                double availableWidth = totalWidth - 10;

                if (availableWidth <= 0)
                    return;

                int columnCount = gridView.Columns.Count;

                if (columnRatios.Length != columnCount)
                {
                    columnRatios = Enumerable.Repeat(1.0 / columnCount, columnCount).ToArray();
                }

                double sumRatios = columnRatios.Sum();

                for (int i = 0; i < columnCount; i++)
                {
                    gridView.Columns[i].Width = availableWidth * (columnRatios[i] / sumRatios);
                }
            }
        }

        // Обработчики для календаря
        private void DateButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            bool isAcceptFrom = button.Name == "BtnAcceptDateFrom";
            bool isAcceptTo = button.Name == "BtnAcceptDateTo";
            bool isIssueFrom = button.Name == "BtnIssueDateFrom";
            bool isIssueTo = button.Name == "BtnIssueDateTo";
            bool isCreateFrom = button.Name == "BtnCreateDateFrom";
            bool isCreateTo = button.Name == "BtnCreateDateTo";

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

            // Установка текущей даты (остаётся без изменений)
            if (isAcceptFrom && _selectedAcceptDateFrom.HasValue)
            {
                calendar.SelectedDate = _selectedAcceptDateFrom.Value;
                calendar.DisplayDate = _selectedAcceptDateFrom.Value;
            }
            else if (isAcceptTo && _selectedAcceptDateTo.HasValue)
            {
                calendar.SelectedDate = _selectedAcceptDateTo.Value;
                calendar.DisplayDate = _selectedAcceptDateTo.Value;
            }
            else if (isIssueFrom && _selectedIssueDateFrom.HasValue)
            {
                calendar.SelectedDate = _selectedIssueDateFrom.Value;
                calendar.DisplayDate = _selectedIssueDateFrom.Value;
            }
            else if (isIssueTo && _selectedIssueDateTo.HasValue)
            {
                calendar.SelectedDate = _selectedIssueDateTo.Value;
                calendar.DisplayDate = _selectedIssueDateTo.Value;
            }
            else if (isCreateFrom && _selectedCreateDateFrom.HasValue)
            {
                calendar.SelectedDate = _selectedCreateDateFrom.Value;
                calendar.DisplayDate = _selectedCreateDateFrom.Value;
            }
            else if (isCreateTo && _selectedCreateDateTo.HasValue)
            {
                calendar.SelectedDate = _selectedCreateDateTo.Value;
                calendar.DisplayDate = _selectedCreateDateTo.Value;
            }

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    // Обновление даты (остаётся без изменений)
                    if (isAcceptFrom)
                    {
                        _selectedAcceptDateFrom = calendar.SelectedDate.Value;
                        AcceptDateFromTextBox.Text = _selectedAcceptDateFrom.Value.ToString("dd.MM.yyyy");
                    }
                    else if (isAcceptTo)
                    {
                        _selectedAcceptDateTo = calendar.SelectedDate.Value;
                        AcceptDateToTextBox.Text = _selectedAcceptDateTo.Value.ToString("dd.MM.yyyy");
                    }
                    else if (isIssueFrom)
                    {
                        _selectedIssueDateFrom = calendar.SelectedDate.Value;
                        IssueDateFromTextBox.Text = _selectedIssueDateFrom.Value.ToString("dd.MM.yyyy");
                    }
                    else if (isIssueTo)
                    {
                        _selectedIssueDateTo = calendar.SelectedDate.Value;
                        IssueDateToTextBox.Text = _selectedIssueDateTo.Value.ToString("dd.MM.yyyy");
                    }
                    else if (isCreateFrom)
                    {
                        _selectedCreateDateFrom = calendar.SelectedDate.Value;
                        CreateDateFromTextBox.Text = _selectedCreateDateFrom.Value.ToString("dd.MM.yyyy");
                    }
                    else if (isCreateTo)
                    {
                        _selectedCreateDateTo = calendar.SelectedDate.Value;
                        CreateDateToTextBox.Text = _selectedCreateDateTo.Value.ToString("dd.MM.yyyy");
                    }

                    ClosePopup();
                    UpdateDateFilterValues();
                    ApplyFilters();
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

            // **1. Получаем позицию кнопки относительно Canvas**
            Point buttonPos = button.TranslatePoint(new Point(0, 0), PopupCanvas);

            // **2. Устанавливаем календарь строго под кнопкой с отступом 4px вниз**
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 4); // +4px вниз

            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentPopup != null && !_currentPopup.IsMouseOver &&
                (_currentButton == null || !_currentButton.IsMouseOver))
            {
                // Проверяем, не был ли клик внутри панели фильтров
                var originalSource = e.OriginalSource as DependencyObject;
                bool clickInsideFilterPanel = IsChildOf(originalSource, FilterPanel);

                if (!clickInsideFilterPanel)
                {
                    ClosePopup();
                }
            }
        }

        // Вспомогательный метод для проверки, является ли элемент дочерним для другого
        private bool IsChildOf(DependencyObject child, DependencyObject parent)
        {
            while (child != null)
            {
                if (child == parent)
                    return true;
                child = VisualTreeHelper.GetParent(child);
            }
            return false;
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

        private void SumTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, запятую и точку (для десятичных чисел)
            if (!char.IsDigit(e.Text, 0) && e.Text != "," && e.Text != ".")
            {
                e.Handled = true;
                return;
            }

            // Получаем текущий текст в TextBox
            var textBox = sender as TextBox;
            string currentText = textBox.Text;

            // Если текст равен "от" или "до" (значения по умолчанию), очищаем его
            if (currentText == "от" || currentText == "до")
            {
                textBox.Text = "";
                return;
            }

            // Проверяем, что в тексте уже есть десятичный разделитель
            if ((e.Text == "," || e.Text == ".") && (currentText.Contains(",") || currentText.Contains(".")))
            {
                e.Handled = true;
                return;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
