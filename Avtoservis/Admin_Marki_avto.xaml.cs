using System;
using System.Collections.Generic;
using System.Data.Entity;
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

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для Admin_Marki_avto.xaml
    /// </summary>
    public partial class Admin_Marki_avto : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;

        private string currentSearchText = "";

        private string currentSortColumn = "ID_marki";
        private bool isAscending = true;

        public Admin_Marki_avto()
        {
            InitializeComponent();
            LoadMarki();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
            LoadFilters(); 

            Marki_List.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }

        private double[] columnRatios = new double[] { 0.1, 0.15, 0.25, 0.25, 0.15, 0.1 };

        private void UpdateGridViewColumnWidths()
        {
            if (Marki_List.View is GridView gridView)
            {
                double totalWidth = Marki_List.ActualWidth;
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

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            string imagePath = System.IO.Path.Combine(projectDirectory, "Marki_avto", imageName);

            try
            {
                if (File.Exists(imagePath))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(imagePath);
                    image.EndInit();
                    return image;
                }
            }
            catch { }

            return null;
        }

        private void LoadMarki()
        {
            try
            {
                var query = App.Context.dm_Marki_avto
                    .Include("dm_Gruppi_avto")
                    .Include("dm_Strani")
                    .AsQueryable();

                // Применение поиска
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    query = query.Where(m => m.Nazvanie_marki.Contains(currentSearchText) ||
                                            m.dm_Gruppi_avto.Nazvanie_gruppi.Contains(currentSearchText) ||
                                            m.dm_Strani.Strana.Contains(currentSearchText));
                }

                // Применение фильтров по группам
                if (selectedGroups.Count > 0)
                {
                    query = query.Where(m => selectedGroups.Contains(m.Gruppa));
                }

                // Применение фильтров по странам
                if (selectedCountries.Count > 0)
                {
                    query = query.Where(m => selectedCountries.Contains(m.Strana_proizvoditel));
                }

                totalItems = query.Count();

                var marki = query
                    .OrderBy(m => m.ID_marki)
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                // Загрузка изображений
                foreach (var marka in marki)
                {
                    if (!string.IsNullOrEmpty(marka.Logotip))
                    {
                        marka.LogotipImage = LoadImage(marka.Logotip);
                    }
                }

                // Сортировка
                switch (currentSortColumn)
                {
                    case "ID_marki":
                        marki = isAscending ?
                            marki.OrderBy(m => m.ID_marki).ToList() :
                            marki.OrderByDescending(m => m.ID_marki).ToList();
                        break;
                    case "Nazvanie_marki":
                        marki = isAscending ?
                            marki.OrderBy(m => m.Nazvanie_marki).ToList() :
                            marki.OrderByDescending(m => m.Nazvanie_marki).ToList();
                        break;
                    case "Gruppa":
                        marki = isAscending ?
                            marki.OrderBy(m => m.dm_Gruppi_avto.Nazvanie_gruppi).ToList() :
                            marki.OrderByDescending(m => m.dm_Gruppi_avto.Nazvanie_gruppi).ToList();
                        break;
                    case "Strana":
                        marki = isAscending ?
                            marki.OrderBy(m => m.dm_Strani.Strana).ToList() :
                            marki.OrderByDescending(m => m.dm_Strani.Strana).ToList();
                        break;
                }

                Marki_List.ItemsSource = marki;
                UpdateSortIndicators();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header)
            {
                string newSortColumn;
                switch (header.Content.ToString())
                {
                    case "ID":
                        newSortColumn = "ID_marki";
                        break;
                    case "Название":
                        newSortColumn = "Nazvanie_marki";
                        break;
                    case "Производственная принадлежность":
                        newSortColumn = "Gruppa";
                        break;
                    case "Страна-производитель":
                        newSortColumn = "Strana";
                        break;
                    case "Логотип":
                        newSortColumn = "Logotip";
                        break;
                    default:
                        newSortColumn = currentSortColumn;
                        break;
                }

                // Если кликнули по той же колонке - меняем направление
                if (currentSortColumn == newSortColumn)
                {
                    isAscending = !isAscending;
                }
                else
                {
                    currentSortColumn = newSortColumn;
                    isAscending = true;
                }

                LoadMarki();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(Marki_List))
            {
                var sortArrow = header.Template.FindName("SortArrow", header) as System.Windows.Shapes.Path;
                if (sortArrow != null)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_marki";
                            break;
                        case "Название":
                            isCurrentSortColumn = currentSortColumn == "Nazvanie_marki";
                            break;
                        case "Производственная принадлежность":
                            isCurrentSortColumn = currentSortColumn == "Gruppa";
                            break;
                        case "Страна-производитель":
                            isCurrentSortColumn = currentSortColumn == "Strana";
                            break;
                        case "Логотип":
                            isCurrentSortColumn = currentSortColumn == "Logotip";
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
            var addEditWindow = new AddEdit_Marka_avto();
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Marki_avto.Count();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadMarki();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentMarka = Marki_List.SelectedItem as Entities.dm_Marki_avto;

            if (currentMarka == null)
            {
                MessageBox.Show("Пожалуйста, выберите марку автомобиля для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, используется ли марка в таблице моделей автомобилей
            bool isUsedInModels = App.Context.dm_Modeli_avto
                .Any(m => m.Marka == currentMarka.ID_marki);

            if (isUsedInModels)
            {
                MessageBox.Show($"Невозможно удалить марку {currentMarka.Nazvanie_marki} (ID: {currentMarka.ID_marki}), " +
                               "так как она используется в одной или нескольких моделях автомобилей.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить марку {currentMarka.Nazvanie_marki} (ID: {currentMarka.ID_marki})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем логотип, если он существует
                    if (!string.IsNullOrEmpty(currentMarka.Logotip))
                    {
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
                        string imagePath = System.IO.Path.Combine(projectDirectory, "Marki_avto", currentMarka.Logotip);

                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                        }
                    }

                    App.Context.dm_Marki_avto.Remove(currentMarka);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Marki_avto.Count();
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadMarki();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Марка автомобиля успешно удалена!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении марки автомобиля:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadMarki();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedMarka = (sender as Button).DataContext as Entities.dm_Marki_avto;

            if (selectedMarka == null)
            {
                MessageBox.Show("Выберите марку автомобиля для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var markaToEdit = App.Context.dm_Marki_avto.FirstOrDefault(m => m.ID_marki == selectedMarka.ID_marki);

            if (markaToEdit != null)
            {
                var addEditWindow = new AddEdit_Marka_avto(markaToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadMarki();
                }
            }
            else
            {
                MessageBox.Show("Марка автомобиля не найдена в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadMarki();
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
                LoadMarki();
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
                LoadMarki();
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
            LoadMarki();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private List<int> selectedGroups = new List<int>();
        private List<int> selectedCountries = new List<int>();

        private void LoadFilters()
        {
            // Загрузка групп автомобилей
            var groups = App.Context.dm_Gruppi_avto
                 .OrderBy(m => m.Nazvanie_gruppi)
                .ToList();
            foreach (var group in groups)
            {
                var checkBox = new CheckBox
                {
                    Content = group.Nazvanie_gruppi,
                    Tag = group.ID_gruppi,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += GroupCheckBox_Checked;
                checkBox.Unchecked += GroupCheckBox_Unchecked;
                GroupFilterPanel.Children.Add(checkBox);
            }

            // Загрузка стран производителей
            var countries = App.Context.dm_Strani
                .OrderBy(m => m.Strana)
                .ToList();
            foreach (var country in countries)
            {
                var checkBox = new CheckBox
                {
                    Content = country.Strana,
                    Tag = country.ID_strani,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += CountryCheckBox_Checked;
                checkBox.Unchecked += CountryCheckBox_Unchecked;
                CountryFilterPanel.Children.Add(checkBox);
            }
        }

        private void GroupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGroups.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void GroupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGroups.Remove((int)checkBox.Tag);
                if (selectedGroups.Count == 0 && selectedCountries.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
                }
                ApplyFilters();
            }
        }

        private void CountryCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedCountries.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void CountryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedCountries.Remove((int)checkBox.Tag);
                if (selectedGroups.Count == 0 && selectedCountries.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
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
            // Сброс фильтров по группам
            foreach (CheckBox checkBox in GroupFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedGroups.Clear();

            // Сброс фильтров по странам
            foreach (CheckBox checkBox in CountryFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedCountries.Clear();

            BtnResetFilters.IsEnabled = false;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            LoadMarki();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }



    }
}
