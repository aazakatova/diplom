using System;
using System.Collections.Generic;
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

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для Admin_Sotrudniki.xaml
    /// </summary>
    public partial class Admin_Sotrudniki : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_user";
        private bool isAscending = true;

        private List<int> selectedExperience = new List<int>();
        private List<string> selectedStatuses = new List<string>();

        public Admin_Sotrudniki()
        {
            InitializeComponent();
            LoadEmployees();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
            LoadFilters();

            SotrudnikiList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }

        private void LoadFilters()
        {
            // Очищаем панель фильтров
            StazhFilterPanel.Children.Clear();
            StatusFilterPanel.Children.Clear();

            // Фильтр по стажу (первые 3 пункта без слова "лет")
            var experienceOptions = new List<(int Years, string Text)>
    {
        (1, "1 год и более"),
        (2, "2 года и более"),
        (3, "3 года и более"),
        (5, "5 лет и более"),
        (10, "10 лет и более"),
        (15, "15 лет и более"),
        (20, "20 лет и более")
    };

            foreach (var exp in experienceOptions)
            {
                var checkBox = new CheckBox
                {
                    Content = exp.Text,
                    Tag = exp.Years,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += ExperienceCheckBox_Checked;
                checkBox.Unchecked += ExperienceCheckBox_Unchecked;
                StazhFilterPanel.Children.Add(checkBox);
            }

            // Фильтр по статусу (оставляем как было)
            var statusOptions = new List<string> { "Работает", "В отпуске" };
            foreach (var status in statusOptions)
            {
                var checkBox = new CheckBox
                {
                    Content = status,
                    Tag = status,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += StatusCheckBox_Checked;
                checkBox.Unchecked += StatusCheckBox_Unchecked;
                StatusFilterPanel.Children.Add(checkBox);
            }
        }

        private void ExperienceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedExperience.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                BtnResetFilters.Cursor = Cursors.Hand;
                ApplyFilters();
            }
        }

        private void ExperienceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedExperience.Remove((int)checkBox.Tag);
                if (selectedExperience.Count == 0 && selectedStatuses.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
                    BtnResetFilters.Cursor = Cursors.No;
                }
                ApplyFilters();
            }
        }

        private void StatusCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedStatuses.Add((string)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                BtnResetFilters.Cursor = Cursors.Hand;
                ApplyFilters();
            }
        }

        private void StatusCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedStatuses.Remove((string)checkBox.Tag);
                if (selectedExperience.Count == 0 && selectedStatuses.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
                    BtnResetFilters.Cursor = Cursors.No;
                }
                ApplyFilters();
            }
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Visible ?
                Visibility.Collapsed :
                Visibility.Visible;
        }

        private void BtnCloseFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Сброс фильтров по стажу
            foreach (CheckBox checkBox in StazhFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedExperience.Clear();

            // Сброс фильтров по статусу
            foreach (CheckBox checkBox in StatusFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedStatuses.Clear();

            BtnResetFilters.IsEnabled = false;
            BtnResetFilters.Cursor = Cursors.No;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            LoadEmployees();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private double[] columnRatios = new double[] { 0.1, 0.15, 0.15, 0.15, 0.15, 0.1, 0.1, 0.1 };

        private void UpdateGridViewColumnWidths()
        {
            if (SotrudnikiList.View is GridView gridView)
            {
                double totalWidth = SotrudnikiList.ActualWidth;
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

        private void LoadEmployees()
        {
            try
            {
                // Получаем только сотрудников (роль ID = 2)
                var employeesQuery = App.Context.dm_Users.Where(u => u.Rol == 2).AsQueryable();

                // Применяем поиск
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    employeesQuery = employeesQuery.Where(u =>
                        u.Familiya.Contains(currentSearchText) ||
                        u.Imya.Contains(currentSearchText) ||
                        u.Otchestvo.Contains(currentSearchText) ||
                        u.Stazh.ToString().Contains(currentSearchText) ||
                        u.Status.Contains(currentSearchText)); 
                }

                // Применяем фильтры по стажу
                if (selectedExperience.Count > 0)
                {
                    employeesQuery = employeesQuery.Where(u => selectedExperience.Any(exp => u.Stazh >= exp));
                }

                // Применяем фильтры по статусу
                if (selectedStatuses.Count > 0)
                {
                    employeesQuery = employeesQuery.Where(u => selectedStatuses.Contains(u.Status));
                }

                totalItems = employeesQuery.Count();

                // Сортировка
                switch (currentSortColumn)
                {
                    case "ID_user":
                        employeesQuery = isAscending ?
                            employeesQuery.OrderBy(u => u.ID_user) :
                            employeesQuery.OrderByDescending(u => u.ID_user);
                        break;
                    case "Stazh":
                        employeesQuery = isAscending ?
                            employeesQuery.OrderBy(u => u.Stazh) :
                            employeesQuery.OrderByDescending(u => u.Stazh);
                        break;
                }

                // Пагинация
                var employees = employeesQuery
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                SotrudnikiList.ItemsSource = employees;
                UpdateSortIndicators();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStatusText(string status)
        {
            if (string.IsNullOrEmpty(status))
                return "Работает";

            return status == "OnVacation" ? "В отпуске" : "Работает";
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header)
            {
                string newSortColumn;
                switch (header.Content.ToString())
                {
                    case "ID":
                        newSortColumn = "ID_user";
                        break;
                    case "Стаж":
                        newSortColumn = "Stazh";
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

                LoadEmployees();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(SotrudnikiList))
            {
                if (header.Template.FindName("SortArrow", header) is Path sortArrow)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_user";
                            break;
                        case "Стаж":
                            isCurrentSortColumn = currentSortColumn == "Stazh";
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
            var addEditWindow = new AddEdit_Sotrudnik();
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Users.Count(u => u.Rol == 2);
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadEmployees();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = SotrudnikiList.SelectedItem as Entities.dm_Users;

            if (selectedEmployee == null)
            {
                MessageBox.Show("Пожалуйста, выберите сотрудника для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var employeeToDelete = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == selectedEmployee.ID_user);

            if (employeeToDelete == null)
            {
                MessageBox.Show("Сотрудник не найден в базе данных.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isUsedInOrders = App.Context.dm_Zakazi.Any(z => z.Ispolnitel == employeeToDelete.ID_user);

            if (isUsedInOrders)
            {
                MessageBox.Show($"Невозможно удалить сотрудника {employeeToDelete.Familiya} {employeeToDelete.Imya} (ID: {employeeToDelete.ID_user}), " +
                               "так как он назначен исполнителем в одном или нескольких заказах.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить сотрудника {employeeToDelete.Familiya} {employeeToDelete.Imya} (ID: {employeeToDelete.ID_user})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    App.Context.dm_Users.Remove(employeeToDelete);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Users.Count(u => u.Rol == 2);
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadEmployees();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Сотрудник успешно удален!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении сотрудника:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = (sender as Button).DataContext as Entities.dm_Users;

            if (selectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника для редактирования.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var employeeToEdit = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == selectedEmployee.ID_user);

            if (employeeToEdit != null)
            {
                var addEditWindow = new AddEdit_Sotrudnik(employeeToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadEmployees();
                }
            }
            else
            {
                MessageBox.Show("Сотрудник не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadEmployees();
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
                int start = (currentPage - 1) * itemsPerPage + 1;
                int end = Math.Min(currentPage * itemsPerPage, totalItems);

                PaginationInfo.Text = $"{start}-{end} из {totalItems}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения информации о пагинации: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Проверка клика вне панели фильтров
            bool clickInsideFilterPanel = false;
            while (originalSource != null)
            {
                if (originalSource == FilterPanel || originalSource == BtnFilter)
                {
                    clickInsideFilterPanel = true;
                    break;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (!clickInsideFilterPanel && FilterPanel.Visibility == Visibility.Visible)
            {
                FilterPanel.Visibility = Visibility.Collapsed;
            }

            // Остальная логика обработки клика
            bool clickInsideSearchBox = false;
            originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null)
            {
                if (originalSource == SearchTextBox)
                {
                    clickInsideSearchBox = true;
                    break;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

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

            var clickedElement = e.OriginalSource as DependencyObject;
            bool isClickInside = false;

            while (clickedElement != null)
            {
                if (clickedElement == currentButton || clickedElement == itemsPerPagePopup)
                {
                    isClickInside = true;
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            if (!isClickInside && itemsPerPagePopup != null && itemsPerPagePopup.IsOpen)
            {
                itemsPerPagePopup.IsOpen = false;
            }
        }

        private void UpdatePaginationButtons()
        {
            if (currentPage > 1)
            {
                BtnPrevPage.Style = (Style)FindResource("PaginationButtonStyle");
                BtnPrevPage.IsEnabled = true;
            }
            else
            {
                BtnPrevPage.Style = (Style)FindResource("PaginationButtonDisabledStyle");
                BtnPrevPage.IsEnabled = false;
            }

            if (currentPage < (int)Math.Ceiling((double)totalItems / itemsPerPage))
            {
                BtnNextPage.Style = (Style)FindResource("PaginationButtonStyle");
                BtnNextPage.IsEnabled = true;
            }
            else
            {
                BtnNextPage.Style = (Style)FindResource("PaginationButtonDisabledStyle");
                BtnNextPage.IsEnabled = false;
            }
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadEmployees();
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
                LoadEmployees();
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
            LoadEmployees();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void SotrudnikiList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedEmployee = SotrudnikiList.SelectedItem as Entities.dm_Users;
            if (selectedEmployee != null)
            {
                // Переход на страницу заказов с фильтрацией по выбранному сотруднику
                var adminWindow = Window.GetWindow(this) as Admin;
                if (adminWindow != null)
                {
                    adminWindow.FrameMain.Navigate(new Admin_Zakazi(selectedEmployee.ID_user));
                }
            }
        }
    }
}
