using Avtoservis.Entities;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using Page = System.Windows.Controls.Page;
using Paragraph = Microsoft.Office.Interop.Word.Paragraph;
using Word = Microsoft.Office.Interop.Word;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для Defektnaya_vedomost.xaml
    /// </summary>
    public partial class Defektnaya_vedomost : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_zakaza";
        private bool isAscending = true;
        private System.Windows.Controls.Border _currentPopup;
        private Button _currentButton;
        private DateTime? _selectedAcceptDateFrom;
        private DateTime? _selectedAcceptDateTo;
        private DateTime? _selectedIssueDateFrom;
        private DateTime? _selectedIssueDateTo;
        private DateTime? _selectedCreateDateFrom;
        private DateTime? _selectedCreateDateTo;
        private decimal? _minSum = null;
        private decimal? _maxSum = null;
        private List<string> selectedStatuses = new List<string>();
        private List<int> selectedWorkPlaces = new List<int>();
        private List<bool?> selectedPaymentStatuses = new List<bool?>();
        private DispatcherTimer dateFilterTimer;
        private const int DateFilterDelay = 500;

        public Defektnaya_vedomost()
        {
            InitializeComponent();

            // Инициализация таймера для фильтрации по дате
            dateFilterTimer = new DispatcherTimer();
            dateFilterTimer.Interval = TimeSpan.FromMilliseconds(DateFilterDelay);
            dateFilterTimer.Tick += DateFilterTimer_Tick;

            this.Loaded += (s, e) =>
            {
                LoadFilters();
                LoadZakazi();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            };

            ZakaziList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }

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
                    .Where(z => z.Status == "Завершён")
                    .AsQueryable();

                // Применение поиска
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    var searchText = currentSearchText.ToLower();
                    query = query.ToList().Where(z =>
                        z.ID_zakaza.ToString().Contains(searchText) ||
                        z.Status.ToLower().Contains(searchText) ||
                        (z.dm_Users.Familiya + " " + z.dm_Users.Imya + " " + z.dm_Users.Otchestvo).ToLower().Contains(searchText) ||
                        (z.dm_Users1 != null && (z.dm_Users1.Familiya + " " + z.dm_Users1.Imya + " " + z.dm_Users1.Otchestvo).ToLower().Contains(searchText)) ||
                        z.Data_sozdaniya.ToString().Contains(searchText) ||
                        (z.dm_Avtomobili != null && z.dm_Avtomobili.dm_Komplektacii_avto != null &&
                         z.dm_Avtomobili.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                         z.dm_Avtomobili.dm_Komplektacii_avto.dm_Modeli_avto.Model.ToLower().Contains(searchText)) ||
                        (z.dm_Avtomobili != null && z.dm_Avtomobili.Gos_nomer != null &&
                         z.dm_Avtomobili.Gos_nomer.ToLower().Contains(searchText)) ||  // Добавлен поиск по гос номеру
                        CalculateOrderSum(z).ToString().Contains(searchText)).AsQueryable();
                }

                // Применение фильтров по статусам, рабочим местам и оплате
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

                // Фильтрация по дате приёма авто
                if (_selectedAcceptDateFrom.HasValue)
                {
                    query = query.Where(z => z.Data_i_vremya_priema_avto >= _selectedAcceptDateFrom.Value);
                }
                if (_selectedAcceptDateTo.HasValue)
                {
                    var endDate = _selectedAcceptDateTo.Value.AddDays(1);
                    query = query.Where(z => z.Data_i_vremya_priema_avto < endDate);
                }

                // Фильтрация по дате выдачи авто
                if (_selectedIssueDateFrom.HasValue)
                {
                    query = query.Where(z => z.Data_i_vremya_vidachi_avto >= _selectedIssueDateFrom.Value);
                }
                if (_selectedIssueDateTo.HasValue)
                {
                    var endDate = _selectedIssueDateTo.Value.AddDays(1);
                    query = query.Where(z => z.Data_i_vremya_vidachi_avto < endDate);
                }

                // Фильтрация по дате создания
                if (_selectedCreateDateFrom.HasValue)
                {
                    query = query.Where(z => z.Data_sozdaniya >= _selectedCreateDateFrom.Value);
                }
                if (_selectedCreateDateTo.HasValue)
                {
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

                totalItems = query.Count();

                // Применяем сортировку
                IQueryable<Entities.dm_Zakazi> sortedQuery;
                switch (currentSortColumn)
                {
                    case "Data_sozdaniya":
                        sortedQuery = isAscending ?
                            query.OrderBy(z => z.Data_sozdaniya) :
                            query.OrderByDescending(z => z.Data_sozdaniya);
                        break;
                    case "TotalSum":
                        var allZakazi = query.ToList();
                        foreach (var zakaz in allZakazi)
                        {
                            zakaz.TotalSum = CalculateOrderSum(zakaz);
                        }
                        var sortedZakazi = isAscending ?
                            allZakazi.OrderBy(z => z.TotalSum).ToList() :
                            allZakazi.OrderByDescending(z => z.TotalSum).ToList();

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
                    zakaz.TotalSum = CalculateOrderSum(zakaz);
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
            // Загрузка рабочих мест
            var workPlaces = App.Context.dm_Rabochie_mesta.ToList();
            WorkPlaceFilterPanel.Children.Clear();
            foreach (var wp in workPlaces)
            {
                var checkBox = new System.Windows.Controls.CheckBox
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
                var checkBox = new System.Windows.Controls.CheckBox
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

        private void StatusCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                selectedStatuses.Add(checkBox.Tag.ToString());
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void StatusCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                selectedStatuses.Remove(checkBox.Tag.ToString());
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void WorkPlaceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                selectedWorkPlaces.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void WorkPlaceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                selectedWorkPlaces.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void PaymentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                selectedPaymentStatuses.Add((bool?)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void PaymentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as System.Windows.Controls.CheckBox;
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
            if (BtnResetFilters == null) return;

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
            if (_currentPopup != null)
            {
                ClosePopup();
            }
            if (itemsPerPagePopup != null && itemsPerPagePopup.IsOpen)
            {
                itemsPerPagePopup.IsOpen = false;
            }

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
            foreach (System.Windows.Controls.CheckBox checkBox in WorkPlaceFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (System.Windows.Controls.CheckBox checkBox in PaymentFilterPanel.Children)
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
                    case "Дата создания":
                        newSortColumn = "Data_sozdaniya";
                        break;
                    case "Сумма":
                        newSortColumn = "TotalSum";
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
                        case "Дата создания":
                            isCurrentSortColumn = currentSortColumn == "Data_sozdaniya";
                            break;
                        case "Сумма":
                            isCurrentSortColumn = currentSortColumn == "TotalSum";
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

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadZakazi();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

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
                    Style = (System.Windows.Style)FindResource("ComboBoxItemStyle"),
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

            itemsPerPagePopup.Child = new System.Windows.Controls.Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Child = stackPanel
            };
        }

        private DateTime lastPopupInteractionTime = DateTime.MinValue;

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

                var activeStyle = FindResource("PaginationButtonStyle") as System.Windows.Style;
                var disabledStyle = FindResource("PaginationButtonDisabledStyle") as System.Windows.Style;

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
            CheckFiltersActive();
        }

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

        private double[] columnRatios = new double[] { 0.05, 0.1, 0.2, 0.2, 0.2, 0.1, 0.15 };

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

            // Установка текущей даты
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
                    // Обновление даты
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

            _currentPopup = new System.Windows.Controls.Border
            {
                Style = (System.Windows.Style)FindResource("PopupBlockStyle"),
                Child = calendar,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray
            };

            PopupCanvas.Children.Add(_currentPopup);

            // Получаем позицию кнопки относительно Canvas
            System.Windows.Point buttonPos = button.TranslatePoint(new System.Windows.Point(0, 0), PopupCanvas);

            // Устанавливаем календарь строго под кнопкой с отступом 4px вниз
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 4);

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

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем выбор заказа
            if (ZakaziList.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите заказ из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var zakaz = ZakaziList.SelectedItem as dm_Zakazi;
            if (zakaz == null)
            {
                MessageBox.Show("Не удалось получить данные заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1. Проверяем, есть ли дефекты в базе для выбранного заказа
            var defectsExist = App.Context.dm_Defekti.Any(d => d.Zakaz == zakaz.ID_zakaza);
            if (!defectsExist)
            {
                MessageBox.Show("Дефекты автомобиля по выбранному заказ-наряду отсутствуют", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                GenerateWordDocument(zakaz);
                // Дойдём до сюда, только если GenerateWordDocument отработал без исключений
                MessageBox.Show("Дефектная ведомость успешно сформирована!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // В случае любой ошибки из GenerateWordDocument покажем только ошибку
                MessageBox.Show($"Ошибка при формировании дефектной ведомости:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateWordDocument(dm_Zakazi zakaz)
        {
            Word.Application wordApp = null;
            Word.Document doc = null;

            try
            {
                wordApp = new Word.Application();
                wordApp.Visible = false;
                doc = wordApp.Documents.Add();

                // Базовые настройки документа (ориентация, поля)
                doc.PageSetup.Orientation = Word.WdOrientation.wdOrientPortrait;
                doc.PageSetup.TopMargin = wordApp.CentimetersToPoints(2);
                doc.PageSetup.BottomMargin = wordApp.CentimetersToPoints(2);
                doc.PageSetup.LeftMargin = wordApp.CentimetersToPoints(3);
                doc.PageSetup.RightMargin = wordApp.CentimetersToPoints(1.5f);

                // Устанавливаем безопасный шрифт по умолчанию
                string safeFontName = "Times New Roman";
                doc.Content.Font.Name = safeFontName;
                doc.Content.Font.Size = 14;

                // 1. Заголовок "Дефектная ведомость"
                Word.Paragraph title = doc.Paragraphs.Add();
                title.Range.Text = "Дефектная ведомость";
                title.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                title.Range.Font.Size = 16;
                title.Range.ParagraphFormat.SpaceAfter = 0;
                title.Range.Font.Bold = 1;
                title.Range.InsertParagraphAfter();

                // 2. Строка "Приложение к заказ-наряду № ... от ..."
                Word.Paragraph appPara = doc.Paragraphs.Add();
                appPara.Range.Text = $"Приложение к заказ-наряду № {zakaz.ID_zakaza} от {zakaz.Data_sozdaniya:dd.MM.yyyy}";
                appPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                appPara.Range.Font.Size = 14;
                appPara.Range.Font.Bold = 0;
                appPara.Range.ParagraphFormat.SpaceAfter = 8;
                appPara.Range.InsertParagraphAfter();

                AddSingleEmptyLine(doc);

                // 3. Раздел "Автомобиль:"
                Word.Paragraph autoLabel = doc.Paragraphs.Add();
                autoLabel.Range.Text = "Автомобиль:";
                autoLabel.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                autoLabel.Range.Font.Size = 14;
                autoLabel.Range.Font.Bold = 1;
                autoLabel.Range.ParagraphFormat.SpaceAfter = 8;
                autoLabel.Range.InsertParagraphAfter();

                // 3.1. Получаем информацию об автомобиле и владельце
                var auto = zakaz.dm_Avtomobili;
                string nazvanieMarki = "";
                string modelAvto = "";
                if (auto != null
                    && auto.dm_Komplektacii_avto != null
                    && auto.dm_Komplektacii_avto.dm_Modeli_avto != null
                    && auto.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto != null)
                {
                    nazvanieMarki = auto.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki;
                    modelAvto = auto.dm_Komplektacii_avto.dm_Modeli_avto.Model;
                }
                string gosNomer = auto?.Gos_nomer ?? "—";
                string vinNomer = auto?.WIN_nomer ?? "—";
                string famVlad = auto?.dm_Users?.Familiya ?? "";
                string imyaVlad = auto?.dm_Users?.Imya ?? "";
                string otchVlad = auto?.dm_Users?.Otchestvo ?? "";
                string phoneVlad = auto?.dm_Users?.Nomer_telefona ?? "";

                // 3.2. Марка
                Word.Paragraph markaPara = doc.Paragraphs.Add();
                markaPara.Range.Text = $"Марка: {nazvanieMarki}";
                markaPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                markaPara.Range.Font.Size = 14;
                markaPara.Range.Font.Bold = 0;
                markaPara.Range.ParagraphFormat.SpaceAfter = 8;
                markaPara.Range.InsertParagraphAfter();

                // 3.3. Модель
                Word.Paragraph modelPara = doc.Paragraphs.Add();
                modelPara.Range.Text = $"Модель: {modelAvto}";
                modelPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                modelPara.Range.Font.Size = 14;
                modelPara.Range.Font.Bold = 0;
                modelPara.Range.ParagraphFormat.SpaceAfter = 8;
                modelPara.Range.InsertParagraphAfter();

                // 3.4. Гос. номер
                Word.Paragraph gosPara = doc.Paragraphs.Add();
                gosPara.Range.Text = $"Гос. номер: {gosNomer}";
                gosPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                gosPara.Range.Font.Size = 14;
                gosPara.Range.Font.Bold = 0;
                gosPara.Range.ParagraphFormat.SpaceAfter = 8;
                gosPara.Range.InsertParagraphAfter();

                // 3.5. VIN (без лишней пустой строки после)
                Word.Paragraph vinPara = doc.Paragraphs.Add();
                vinPara.Range.Text = $"VIN: {vinNomer}";
                vinPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                vinPara.Range.Font.Size = 14;
                vinPara.Range.Font.Bold = 0;
                vinPara.Range.ParagraphFormat.SpaceAfter = 8;
                vinPara.Range.InsertParagraphAfter();

                // 3.6. Владелец
                Word.Paragraph vladPara = doc.Paragraphs.Add();
                vladPara.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                vladPara.Range.Font.Size = 14;
                vladPara.Range.Font.Bold = 0;

                // Вставляем весь текст
                string fullVladText = $"Владелец: {famVlad} {imyaVlad} {otchVlad}, тел. {phoneVlad}";
                vladPara.Range.Text = fullVladText;
                vladPara.Range.ParagraphFormat.SpaceAfter = 8;

                // Выделяем подстроку "Владелец:" и делаем её жирной
                var ownerLabelLength = "Владелец:".Length;
                Word.Range boldRange = doc.Range(vladPara.Range.Start, vladPara.Range.Start + ownerLabelLength);
                boldRange.Font.Bold = 1;

                vladPara.Range.InsertParagraphAfter();

                AddSingleEmptyLine(doc);

                // 4. Раздел "Выявленные дефекты:"
                Word.Paragraph defectLabel = doc.Paragraphs.Add();
                defectLabel.Range.Text = "Выявленные дефекты:";
                defectLabel.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                defectLabel.Range.Font.Size = 14;
                defectLabel.Range.Font.Bold = 1;
                defectLabel.Range.ParagraphFormat.SpaceAfter = 8;
                defectLabel.Range.InsertParagraphAfter();

                // 4.1. Получаем список дефектов из БД
                var defectsList = App.Context.dm_Defekti
                    .Include("dm_Uzli_avto")
                    .Where(d => d.Zakaz == zakaz.ID_zakaza)
                    .ToList();

                // 4.2. Создаём таблицу: строки = defectsList.Count + 1, столбцы = 5
                int rowsCount = defectsList.Count + 1;
                int colsCount = 5;
                Word.Table defectsTable = doc.Tables.Add(defectLabel.Range, rowsCount, colsCount);
                defectsTable.Borders.Enable = 1;
                defectsTable.Range.Font.Size = 13;
                defectsTable.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                // Устанавливаем точные ширины столбцов (в пунктах)
                defectsTable.Columns[1].Width = 28.35f;    // 1 см = 28.35 пунктов
                defectsTable.Columns[2].Width = 106.03f;   // примерно 3.75 см
                defectsTable.Columns[3].Width = 113.4f;    // примерно 4 см
                defectsTable.Columns[4].Width = 113.4f;    // примерно 4 см
                defectsTable.Columns[5].Width = 113.4f;    // примерно 4 см

                // Настройка интервала после абзаца для всей таблицы
                foreach (Word.Row row in defectsTable.Rows)
                {
                    foreach (Word.Cell cell in row.Cells)
                    {
                        cell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                        cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                        cell.Range.ParagraphFormat.SpaceAfter = 8;
                    }
                }

                // Заголовки столбцов
                defectsTable.Cell(1, 1).Range.Text = "№";
                defectsTable.Cell(1, 2).Range.Text = "Узел автомобиля";
                defectsTable.Cell(1, 3).Range.Text = "Описание дефекта";
                defectsTable.Cell(1, 4).Range.Text = "Рекомендации по устранению";
                defectsTable.Cell(1, 5).Range.Text = "Примечания";
                for (int c = 1; c <= colsCount; c++)
                {
                    defectsTable.Cell(1, c).Range.Font.Bold = 1;
                }

                // Заполняем строки данными, удаляя возможные лишние переводы строки в примечаниях
                for (int i = 0; i < defectsList.Count; i++)
                {
                    var def = defectsList[i];
                    int rowIndex = i + 2; // начинаем со второй строки

                    string uzel = def.dm_Uzli_avto?.Nazvanie_uzla_avto ?? "—";
                    string opis = (def.Opisanie ?? "—").Replace("\r\n", " ").Replace("\n", " ").Trim();
                    string rekom = (def.Rekomendacii ?? "—").Replace("\r\n", " ").Replace("\n", " ").Trim();
                    string prim = (def.Primechaniya ?? "—").Replace("\r\n", " ").Replace("\n", " ").Trim();

                    defectsTable.Cell(rowIndex, 1).Range.Text = (i + 1).ToString();
                    defectsTable.Cell(rowIndex, 2).Range.Text = uzel;
                    defectsTable.Cell(rowIndex, 3).Range.Text = opis;
                    defectsTable.Cell(rowIndex, 4).Range.Text = rekom;
                    defectsTable.Cell(rowIndex, 5).Range.Text = prim;

                    for (int c = 1; c <= colsCount; c++)
                    {
                        defectsTable.Cell(rowIndex, c).Range.Font.Bold = 0;
                        defectsTable.Cell(rowIndex, c).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    }
                }

                AddSingleEmptyLine(doc);
                AddSingleEmptyLine(doc);

                // 5. Таблица для подписей (1 строка, 2 столбца)
                Word.Table signatureTable = doc.Tables.Add(doc.Paragraphs.Add().Range, 1, 2);
                signatureTable.Borders.Enable = 0;
                signatureTable.Range.Font.Size = 13;
                signatureTable.AllowAutoFit = false;

                // Настраиваем межстрочный интервал в ячейках
                foreach (Word.Row row in signatureTable.Rows)
                {
                    foreach (Word.Cell cell in row.Cells)
                    {
                        cell.Range.ParagraphFormat.SpaceAfter = 0;
                        cell.Range.ParagraphFormat.SpaceBefore = 0;
                        cell.Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
                    }
                }

                // Колонка 1: Исполнитель
                Word.Cell executorCell = signatureTable.Cell(1, 1);
                executorCell.Range.Text = $"Исполнитель:\n___________ / {zakaz.dm_Users?.FullName}";
                executorCell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                executorCell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalTop;
                executorCell.Range.Font.Bold = 0;

                // Колонка 2: Заказчик
                Word.Cell customerCell = signatureTable.Cell(1, 2);
                customerCell.Range.Text = $"Заказчик:\n___________ / {zakaz.dm_Users1.FullName}";
                customerCell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                customerCell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalTop;
                customerCell.Range.Font.Bold = 0;

                // Ширины колонок (по 8 см каждая)
                signatureTable.Columns[1].Width = wordApp.CentimetersToPoints(8);
                signatureTable.Columns[2].Width = wordApp.CentimetersToPoints(8);

                // 6. Сохранение документа
                string fileName = $"Дефектная ведомость №{zakaz.ID_zakaza} от {DateTime.Today:dd.MM.yyyy}.docx";
                string folderPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Дефектные ведомости");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                string fullPath = System.IO.Path.Combine(folderPath, fileName);

                doc.SaveAs2(fullPath, Word.WdSaveFormat.wdFormatDocumentDefault);
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            catch
            {
                throw;
            }
            finally
            {
                doc?.Close(false);
                wordApp?.Quit();
            }
        }

        // Метод для добавления одной пустой строки
        private void AddSingleEmptyLine(Word.Document doc)
        {
            Word.Paragraph emptyPara = doc.Paragraphs.Add();
            emptyPara.Range.Text = string.Empty;
            emptyPara.Range.InsertParagraphAfter();
            emptyPara.Range.ParagraphFormat.SpaceAfter = 0;
            emptyPara.Range.ParagraphFormat.SpaceBefore = 0;
            emptyPara.Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
        }

        // Старый метод для добавления двойного интервала (оставлен для совместимости)
        private void AddEmptyLine(Word.Document doc)
        {
            Word.Paragraph emptyPara = doc.Paragraphs.Add();
            emptyPara.Range.Text = "\v"; // Вертикальный таб (надежный способ)
            emptyPara.Range.InsertParagraphAfter();
            emptyPara.Range.ParagraphFormat.SpaceAfter = 0;
            emptyPara.Range.ParagraphFormat.SpaceBefore = 0;
            emptyPara.Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
        }

        // Новый вспомогательный метод для установки текста с выравниванием по центру
        private void SetCellTextCenter(Word.Table table, int row, int column, string text)
        {
            Word.Cell cell = table.Cell(row, column);
            cell.Range.Text = text;
            cell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
        }
    }
}


