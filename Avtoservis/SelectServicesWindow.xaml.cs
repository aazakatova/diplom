using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для SelectServicesWindow.xaml
    /// </summary>
    public partial class SelectServicesWindow : Window
    {
        public List<Entities.dm_Raboti> SelectedServices { get; private set; }
        private string currentSearchText = "";
        private string currentSortColumn = "Naimenovanie";
        private bool isAscending = true;

        // Пропорции колонок (10%, 50%, 20%, 20%)
        private double[] columnRatios = new double[] { 0.1, 0.4, 0.2, 0.2 };

        public SelectServicesWindow()
        {
            InitializeComponent();
            LoadServices();
            ServicesList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }
        private void UpdateGridViewColumnWidths()
        {
            if (ServicesList.View is GridView gridView)
            {
                double totalWidth = ServicesList.ActualWidth;
                double availableWidth = totalWidth - 20; // отступы

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

        private void LoadServices()
        {
            try
            {
                var query = App.Context.dm_Raboti.AsQueryable();

                // Применение поиска
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    query = query.Where(s =>
                        s.Naimenovanie.Contains(currentSearchText) ||
                        s.Stoimost.ToString().Contains(currentSearchText) ||
                        s.Dlitelnost.ToString().Contains(currentSearchText));
                }

                // Применение сортировки
                switch (currentSortColumn)
                {
                    case "Naimenovanie":
                        query = isAscending ?
                            query.OrderBy(s => s.Naimenovanie) :
                            query.OrderByDescending(s => s.Naimenovanie);
                        break;
                    case "Stoimost":
                        query = isAscending ?
                            query.OrderBy(s => s.Stoimost) :
                            query.OrderByDescending(s => s.Stoimost);
                        break;
                    case "Dlitelnost":
                        query = isAscending ?
                            query.OrderBy(s => s.Dlitelnost) :
                            query.OrderByDescending(s => s.Dlitelnost);
                        break;
                }

                var services = query.ToList();
                var viewModels = services.Select(s => new ServiceViewModel { Service = s }).ToList();
                ServicesList.ItemsSource = viewModels;
                UpdateSortIndicators();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(ServicesList))
            {
                if (header.Template.FindName("SortArrow", header) is Path sortArrow)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "Услуга":
                            isCurrentSortColumn = currentSortColumn == "Naimenovanie";
                            break;
                        case "Стоимость":
                            isCurrentSortColumn = currentSortColumn == "Stoimost";
                            break;
                        case "Длительность, мин.":
                            isCurrentSortColumn = currentSortColumn == "Dlitelnost";
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

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header)
            {
                string newSortColumn;
                switch (header.Content.ToString())
                {
                    case "Услуга":
                        newSortColumn = "Naimenovanie";
                        break;
                    case "Стоимость":
                        newSortColumn = "Stoimost";
                        break;
                    case "Длительность, мин.":
                        newSortColumn = "Dlitelnost";
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

                LoadServices();
            }
        }

        private void ServicesList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGridViewColumnWidths();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            SelectedServices = ((IEnumerable<ServiceViewModel>)ServicesList.ItemsSource)
                .Where(vm => vm.IsSelected)
                .Select(vm => vm.Service)
                .ToList();

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
            LoadServices();
        }

        private void ServicesList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element &&
                element.DataContext is ServiceViewModel item)
            {
                item.IsSelected = !item.IsSelected;
                ServicesList.Items.Refresh();
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Получаем элемент, по которому кликнули
            var originalSource = e.OriginalSource as DependencyObject;

            // Проверяем, является ли клик внутри SearchTextBox или его дочерних элементов
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
                // Если клик вне SearchTextBox — снимаем фокус с SearchTextBox
                if (SearchTextBox.IsFocused)
                {
                    // Переводим фокус на саму страницу или другой элемент,
                    // чтобы SearchTextBox потерял фокус и сработал LostFocus
                    Keyboard.ClearFocus();

                    // Можно дополнительно вызвать LostFocus-логику вручную, если нужно
                    // Например, если LostFocus не срабатывает автоматически:
                    if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                    {
                        SearchTextBox.Text = "Поиск...";
                        SearchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                        SearchTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // рамка серая
                    }
                }
            }
            else
            {
                // Клик по SearchTextBox — если текст "Поиск...", очищаем и меняем рамку
                if (SearchTextBox.Text == "Поиск...")
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
                    SearchTextBox.BorderBrush = new SolidColorBrush(Colors.Blue); // рамка синяя
                }
            }
        }
    }

    public class ServiceViewModel
    {
        public Entities.dm_Raboti Service { get; set; }
        public bool IsSelected { get; set; }
    }
}

