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
using System.Drawing;
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
    /// Логика взаимодействия для Schet_na_oplatu.xaml
    /// </summary>
    public partial class Schet_na_oplatu : Page
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

        public Schet_na_oplatu()
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
            var selectedZakaz = ZakaziList.SelectedItem as dm_Zakazi;

            if (selectedZakaz == null)
            {
                MessageBox.Show("Пожалуйста, выберите заказ для формирования акта.",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                GenerateWordDocument(selectedZakaz);
                // Дойдём до сюда, только если GenerateWordDocument отработал без исключений
                MessageBox.Show("Счёт на оплату успешно сформирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // В случае любой ошибки из GenerateWordDocument покажем только ошибку
                MessageBox.Show($"Ошибка при формировании счёта на оплату:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Базовые настройки документа (без шрифтов)
                doc.PageSetup.Orientation = Word.WdOrientation.wdOrientPortrait;
                doc.PageSetup.TopMargin = wordApp.CentimetersToPoints(2);
                doc.PageSetup.BottomMargin = wordApp.CentimetersToPoints(2);
                doc.PageSetup.LeftMargin = wordApp.CentimetersToPoints(3);
                doc.PageSetup.RightMargin = wordApp.CentimetersToPoints(1.5f);

                // Устанавливаем безопасный шрифт по умолчанию
                string safeFontName = "Times New Roman";
                doc.Content.Font.Name = safeFontName;
                doc.Content.Font.Size = 14;
                doc.Content.Font.Bold = 0;


                // Создаем таблицу 4x4
                Word.Table bankTable = doc.Tables.Add(doc.Paragraphs.Add().Range, 4, 4);
                bankTable.Borders.Enable = 1;
                bankTable.Range.Font.Size = 13;

                // Настройка интервала после абзаца для всей таблицы
                foreach (Word.Row row in bankTable.Rows)
                {
                    foreach (Word.Cell cell in row.Cells)
                    {
                        cell.Range.ParagraphFormat.SpaceAfter = 0;
                    }
                }

                // Установка ширины столбцов
                bankTable.Columns[1].Width = wordApp.CentimetersToPoints(4.12f); // 1-й столбец
                bankTable.Columns[2].Width = wordApp.CentimetersToPoints(4.12f); // 2-й столбец
                bankTable.Columns[3].Width = wordApp.CentimetersToPoints(3.12f); // 3-й столбец (уже!)
                bankTable.Columns[4].Width = wordApp.CentimetersToPoints(5.12f); // 4-й столбец

                /* Правильная последовательность объединения:
                   1. Сначала заполняем НЕобъединенные ячейки
                   2. Затем делаем объединения от простых к сложным
                */

                // 1. Заполняем ячейки, которые НЕ будут объединяться
                bankTable.Cell(1, 3).Range.Text = "БИК";
                bankTable.Cell(1, 4).Range.Text = "044525225";
                bankTable.Cell(2, 4).Range.Text = "40702810123456789012";
                bankTable.Cell(2, 3).Range.Text = "Сч. №";
                bankTable.Cell(3, 1).Range.Text = "ИНН 7712345678";
                bankTable.Cell(3, 2).Range.Text = "КПП 771201001";

                // 2. Делаем объединения (от простых к сложным)
                // Объединяем (3,3)-(4,3)
                Word.Cell cell33 = bankTable.Cell(3, 3);
                cell33.Merge(bankTable.Cell(4, 3));
                cell33.Range.Text = "Сч. №";

                // Объединяем (3,4)-(4,4)
                Word.Cell cell34 = bankTable.Cell(3, 4);
                cell34.Merge(bankTable.Cell(4, 4));
                cell34.Range.Text = "76543210987654423428";

                // Объединяем (4,1)-(4,2)
                Word.Cell cell41 = bankTable.Cell(4, 1);
                cell41.Merge(bankTable.Cell(4, 2));
                cell41.Range.Text = "GarageServis\n\nПолучатель";

                // ВСЕГДА В САМЫЙ ПОСЛЕДНИЙ МОМЕНТ объединяем самую большую область
                // Объединяем (1,1)-(2,2)
                Word.Cell cell11 = bankTable.Cell(1, 1);
                cell11.Merge(bankTable.Cell(2, 2));
                cell11.Range.Text = "ПАО «Супер-Банк»\n\nБанк получателя";
                cell11.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                AddSingleEmptyLine(doc);

                // Заголовок
                Word.Paragraph title = doc.Paragraphs.Add();
                title.Range.Text = $"Счёт на оплату № {zakaz.ID_zakaza} от {DateTime.Today:dd MMMM yyyy г.}";
                title.Range.Font.Size = 16;
                title.Range.Font.Bold = 1;
                title.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                title.Range.InsertParagraphAfter();

                // Вставляем линию после заголовка
                Word.Range lineRange = title.Range;
                lineRange.Collapse(Word.WdCollapseDirection.wdCollapseEnd); // переместиться после заголовка
                Word.Paragraph linePara = doc.Paragraphs.Add(lineRange);
                linePara.Borders[Word.WdBorderType.wdBorderBottom].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
                linePara.Borders[Word.WdBorderType.wdBorderBottom].Color = Word.WdColor.wdColorBlack;
                linePara.Borders[Word.WdBorderType.wdBorderBottom].LineWidth = Word.WdLineWidth.wdLineWidth150pt;
                linePara.Range.InsertParagraphAfter();

                // 4. Информация о сторонах (исправленное выравнивание)
                Word.Paragraph executorPara = doc.Paragraphs.Add();;
                executorPara.Range.Text = "Исполнитель: GarageServis, ИНН 7712345678 КПП 771201001, адрес: г. Ярославль, район Дядьково, посёлок Нефтебаза, 108А, тел. +7 (920) 108-81-87";
                executorPara.Range.Font.Bold = 0;
                executorPara.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphJustify;
                executorPara.Range.Font.Size = 14;
                executorPara.Range.ParagraphFormat.SpaceAfter = 8; // Меньший интервал
                executorPara.Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle; // Одинарный интервал
                executorPara.Range.InsertParagraphAfter();

                Word.Paragraph customerPara = doc.Paragraphs.Add();
                customerPara.Range.Text = $"Заказчик: {zakaz.dm_Users1.FullName}, тел. {zakaz.dm_Users1.Nomer_telefona ?? "не указан"}";
                customerPara.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphJustify;
                customerPara.Range.ParagraphFormat.SpaceAfter = 12; // Меньший интервал
                customerPara.Range.Font.Size = 14;
                customerPara.Range.Font.Bold = 0;
                customerPara.Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle; // Одинарный интервал
                customerPara.Range.InsertParagraphAfter();

                // 6. Работы и детали (вместе)
                var works = App.Context.dm_Raboti_v_zakaze
                    .Include("dm_Raboti")
                    .Where(r => r.ID_zakaza == zakaz.ID_zakaza)
                    .ToList();

                var groupedWorks = works
                    .GroupBy(r => r.ID_raboti)
                    .Select(g => new {
                        Work = g.First(),
                        Count = g.Count(),
                        TotalPrice = (g.First().Zakrep_stoimost ?? g.First().dm_Raboti?.Stoimost ?? 0) * g.Count()
                    })
                    .ToList();

                var parts = App.Context.dm_Detali_v_zakaze
                    .Include("dm_Detali")
                    .Where(d => d.ID_zakaza == zakaz.ID_zakaza && d.Detal_klienta == false)
                    .ToList();

                var combinedItems = groupedWorks
                    .Select(w => new {
                        Name = w.Work.dm_Raboti?.Naimenovanie,
                        Unit = "шт.",
                        Quantity = w.Count,
                        Price = w.Work.Zakrep_stoimost ?? w.Work.dm_Raboti?.Stoimost ?? 0,
                        Sum = w.TotalPrice
                    })
                    .ToList();

                combinedItems.AddRange(parts.Select(p => new {
                    Name = p.dm_Detali?.Nazvanie,
                    Unit = "шт.",
                    Quantity = p.Kolichestvo,
                    Price = p.Zakrep_cena ?? 0,
                    Sum = (p.Zakrep_cena ?? 0) * p.Kolichestvo
                }));

                if (combinedItems.Any())
                {
                    Word.Table table = doc.Tables.Add(doc.Paragraphs.Add().Range, combinedItems.Count + 4, 6);
                    table.Borders.Enable = 1;
                    table.Range.Font.Size = 13;

                    table.Columns[1].Width = 28.35f;
                    table.Columns[2].Width = 183.99f;
                    table.Columns[3].Width = 56.7f;
                    table.Columns[4].Width = 56.98f;
                    table.Columns[5].Width = 70.87f;
                    table.Columns[6].Width = 85.05f;

                    foreach (Word.Row row in table.Rows)
                    {
                        foreach (Word.Cell cell in row.Cells)
                        {
                            cell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                            cell.Range.ParagraphFormat.SpaceAfter = 8;
                        }
                    }

                    table.Cell(1, 1).Range.Text = "№";
                    table.Cell(1, 2).Range.Text = "Наименование услуги/детали";
                    table.Cell(1, 3).Range.Text = "Ед.изм.";
                    table.Cell(1, 4).Range.Text = "Кол-во";
                    table.Cell(1, 5).Range.Text = "Цена, руб.";
                    table.Cell(1, 6).Range.Text = "Сумма, руб.";
                    for (int c = 1; c <= 6; c++)
                        table.Cell(1, c).Range.Font.Bold = 1;

                    decimal totalSum = 0;
                    for (int i = 0; i < combinedItems.Count; i++)
                    {
                        var item = combinedItems[i];
                        table.Cell(i + 2, 1).Range.Text = (i + 1).ToString();
                        table.Cell(i + 2, 2).Range.Text = item.Name ?? "";
                        table.Cell(i + 2, 3).Range.Text = item.Unit;
                        table.Cell(i + 2, 4).Range.Text = item.Quantity.ToString();
                        table.Cell(i + 2, 5).Range.Text = item.Price.ToString("N2");
                        table.Cell(i + 2, 6).Range.Text = item.Sum.ToString("N2");
                        totalSum += item.Sum;
                    }

                    int baseRow = combinedItems.Count + 2;

                    table.Cell(baseRow, 1).Range.Text = "Итого:";
                    table.Cell(baseRow, 1).Range.Bold = 1;
                    table.Cell(baseRow, 6).Range.Text = totalSum.ToString("N2");
                    table.Cell(baseRow, 6).Range.Bold = 1;
                    table.Rows[baseRow].Cells[1].Merge(table.Rows[baseRow].Cells[5]);
                    table.Rows[baseRow].Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;

                    table.Cell(baseRow + 1, 1).Range.Text = "В том числе НДС:";
                    table.Cell(baseRow + 1, 1).Range.Bold = 1;
                    table.Cell(baseRow + 1, 6).Range.Text = "без НДС";
                    table.Cell(baseRow + 1, 6).Range.Bold = 1;
                    table.Rows[baseRow + 1].Cells[1].Merge(table.Rows[baseRow + 1].Cells[5]);
                    table.Rows[baseRow + 1].Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;

                    table.Cell(baseRow + 2, 1).Range.Text = "Всего к оплате:";
                    table.Cell(baseRow + 2, 1).Range.Bold = 1;
                    table.Cell(baseRow + 2, 6).Range.Text = totalSum.ToString("N2");
                    table.Cell(baseRow + 2, 6).Range.Bold = 1;
                    table.Rows[baseRow + 2].Cells[1].Merge(table.Rows[baseRow + 2].Cells[5]);
                    table.Rows[baseRow + 2].Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                }

                AddSingleEmptyLine(doc);

                // Остальные части документа (упрощенные)
                decimal totalAmount = CalculateOrderSum(zakaz);

                // Создаем абзац с суммой цифрами
                Paragraph total = doc.Paragraphs.Add();
                total.Range.Text = $"Всего выполнено работ (оказано услуг) на сумму: {totalAmount:N2} руб.";

                // Выделяем только текст (без суммы)
                int textLength = "Всего выполнено работ (оказано услуг) на сумму: ".Length;
                Word.Range italicRange = total.Range;
                italicRange.End = italicRange.Start + textLength;
                italicRange.Font.Italic = 1;

                total.Alignment = WdParagraphAlignment.wdAlignParagraphJustify;
                total.Range.ParagraphFormat.SpaceAfter = 8;
                total.Range.Font.Size = 14;
                total.Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
                total.Range.InsertParagraphAfter();

                // Добавляем сумму прописью
                Paragraph amountInWords = doc.Paragraphs.Add();
                string amountText = RuDateAndMoneyConverter.CurrencyToTxt(totalAmount, true);

                // Делаем первую букву заглавной
                if (!string.IsNullOrEmpty(amountText))
                {
                    amountText = char.ToUpper(amountText[0]) + amountText.Substring(1);
                }

                amountInWords.Range.Text = amountText;
                amountInWords.Range.Font.Bold = 1; // Жирный шрифт для суммы прописью
                amountInWords.Alignment = WdParagraphAlignment.wdAlignParagraphJustify;
                amountInWords.Range.Font.Size = 14;
                amountInWords.Range.InsertParagraphAfter();

                Word.Range lineRange2 = amountInWords.Range;
                lineRange2.Collapse(Word.WdCollapseDirection.wdCollapseEnd); // переместиться после заголовка
                Word.Paragraph linePara2 = doc.Paragraphs.Add(lineRange2);
                linePara2.Borders[Word.WdBorderType.wdBorderBottom].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
                linePara2.Borders[Word.WdBorderType.wdBorderBottom].Color = Word.WdColor.wdColorBlack;
                linePara2.Borders[Word.WdBorderType.wdBorderBottom].LineWidth = Word.WdLineWidth.wdLineWidth150pt;
                linePara2.Range.InsertParagraphAfter();

                // 9. Подписи в таблице (2 колонки)
                Word.Table signatureTable = doc.Tables.Add(doc.Paragraphs.Add().Range, 1, 2);
                signatureTable.Borders.Enable = 0;
                signatureTable.Range.Font.Italic = 0;
                signatureTable.Range.Font.Size = 13;
                signatureTable.AllowAutoFit = false;

                // Применяем межстрочный интервал ко всем ячейкам таблицы
                foreach (Word.Row row in signatureTable.Rows)
                {
                    foreach (Word.Cell cell in row.Cells)
                    {
                        // Устанавливаем межстрочный интервал для текста в ячейке
                        cell.Range.ParagraphFormat.SpaceAfter = 0;  // Убираем дополнительный интервал после абзаца
                        cell.Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceExactly;
                        cell.Range.ParagraphFormat.LineSpacing = wordApp.LinesToPoints(1.5f);
                    }
                }

                // Колонка 1: Руководитель
                Word.Cell executorCell = signatureTable.Cell(1, 1);
                executorCell.Range.Text = $"Руководитель:\n___________ / Кудрявцев А.А.";
                executorCell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                executorCell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalTop;
                executorCell.Range.Font.Bold = 0;

                // Колонка 2: Бухгалтер
                Word.Cell customerCell = signatureTable.Cell(1, 2);
                customerCell.Range.Text = $"Бухгалтер:\n___________ / Рыжова И.О.";
                customerCell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                customerCell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalTop;
                customerCell.Range.Font.Bold = 0;

                // Настройка ширины колонок
                signatureTable.Columns[1].Width = wordApp.CentimetersToPoints(8);
                signatureTable.Columns[2].Width = wordApp.CentimetersToPoints(8);

                // Сохранение документа
                string fileName = $"Счёт на оплату №{zakaz.ID_zakaza} от {DateTime.Today:dd.MM.yyyy}.docx";
                string folderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Счета на оплату");

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
