using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для Admin_Rabochie_mesta.xaml
    /// </summary>
    public partial class Admin_Rabochie_mesta : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;

        private string currentSearchText = "";

        private string currentSortColumn = "ID_rabochego_mesta";
        private bool isAscending = true;

        public Admin_Rabochie_mesta()
        {
            InitializeComponent();
            LoadRabochieMesta();
            UpdatePaginationInfo();
            UpdatePaginationButtons();

            Rabochie_mesta_List.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }

        private double[] columnRatios = new double[] { 0.1, 0.2, 0.5, 0.2 };

        private void UpdateGridViewColumnWidths()
        {
            if (Rabochie_mesta_List.View is GridView gridView)
            {
                double totalWidth = Rabochie_mesta_List.ActualWidth;
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
            string imagePath = System.IO.Path.Combine(projectDirectory, "Rabochie_mesta", imageName);

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

        private void LoadRabochieMesta()
        {
            try
            {
                var query = App.Context.dm_Rabochie_mesta.AsQueryable();

                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    query = query.Where(w => w.Rabochee_mesto.Contains(currentSearchText));
                }

                totalItems = query.Count();

                var works = query
                    .OrderBy(w => w.ID_rabochego_mesta)
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                // Загружаем изображения для каждого рабочего места
                foreach (var work in works)
                {
                    if (!string.IsNullOrEmpty(work.Icon))
                    {
                        work.IconImage = LoadImage(work.Icon);
                    }
                }

                // Применяем сортировку
                switch (currentSortColumn)
                {
                    case "ID_rabochego_mesta":
                        works = isAscending ?
                            works.OrderBy(w => w.ID_rabochego_mesta).ToList() :
                            works.OrderByDescending(w => w.ID_rabochego_mesta).ToList();
                        break;
                    case "Rabochee_mesto":
                        works = isAscending ?
                            works.OrderBy(w => w.Rabochee_mesto).ToList() :
                            works.OrderByDescending(w => w.Rabochee_mesto).ToList();
                        break;
                    case "Icon":
                        works = isAscending ?
                            works.OrderBy(w => w.Icon).ToList() :
                            works.OrderByDescending(w => w.Icon).ToList();
                        break;
                }

                Rabochie_mesta_List.ItemsSource = works;
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
                        newSortColumn = "ID_rabochego_mesta";
                        break;
                    case "Наименование":
                        newSortColumn = "Rabochee_mesto";
                        break;
                    case "Иконка":
                        newSortColumn = "Icon";
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

                // Не сбрасываем страницу, сортируем текущую
                LoadRabochieMesta();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(Rabochie_mesta_List))
            {
                var sortArrow = header.Template.FindName("SortArrow", header) as System.Windows.Shapes.Path;
                if (sortArrow != null)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_rabochego_mesta";
                            break;
                        case "Наименование":
                            isCurrentSortColumn = currentSortColumn == "Rabochee_mesto";
                            break;
                        case "Иконка":
                            isCurrentSortColumn = currentSortColumn == "Icon";
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
            var addEditWindow = new AddEdit_Rabochee_mesto();
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Rabochie_mesta.Count();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadRabochieMesta();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentWork = Rabochie_mesta_List.SelectedItem as Entities.dm_Rabochie_mesta;

            if (currentWork == null)
            {
                MessageBox.Show("Пожалуйста, выберите рабочее место для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isUsedInOrders = App.Context.dm_Zakazi
                .Any(z => z.Rabochee_mesto == currentWork.ID_rabochego_mesta);

            if (isUsedInOrders)
            {
                MessageBox.Show($"Невозможно удалить рабочее место {currentWork.Rabochee_mesto} (ID: {currentWork.ID_rabochego_mesta}), " +
                               "так как оно используется в одном или нескольких заказах.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить рабочее место {currentWork.Rabochee_mesto} (ID: {currentWork.ID_rabochego_mesta})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    App.Context.dm_Rabochie_mesta.Remove(currentWork);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Rabochie_mesta.Count();
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadRabochieMesta();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Рабочее место успешно удалено!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении рабочего места:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRabochieMesta();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedWork = (sender as Button).DataContext as Entities.dm_Rabochie_mesta;

            if (selectedWork == null)
            {
                MessageBox.Show("Выберите рабочее место для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var workToEdit = App.Context.dm_Rabochie_mesta.FirstOrDefault(s => s.ID_rabochego_mesta == selectedWork.ID_rabochego_mesta);

            if (workToEdit != null)
            {
                var addEditWindow = new AddEdit_Rabochee_mesto(workToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadRabochieMesta();
                }
            }
            else
            {
                MessageBox.Show("Рабочее место не найдено в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadRabochieMesta();
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

            bool clickInsideSearchBox = false;
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
                LoadRabochieMesta();
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
                LoadRabochieMesta();
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
            LoadRabochieMesta();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }
    }
}
