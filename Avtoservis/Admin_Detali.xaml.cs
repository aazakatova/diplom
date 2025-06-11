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
    /// Логика взаимодействия для Admin_Detali.xaml
    /// </summary>
    public partial class Admin_Detali : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_detali";
        private bool isAscending = true;

        private List<int> selectedTypes = new List<int>();
        private List<int> selectedManufacturers = new List<int>();

        // Признак «сотрудника»
        private readonly bool isEmployee;

        // Пропорции для «полезных» колонок (ID, Название, Тип, Производитель, Стоимость)
        // Зададим массив длины 5, но для сотрудника будем использовать только первые 5−1=4 значения
        private double[] columnRatios;

        // «Спэйсер»-колонка, чтобы справа всегда оставалось ~10px
        private const double SpacerWidth = 10;

        public Admin_Detali()
        {
            InitializeComponent();

            // Определяем роль: Rol == 1 → Админ, Rol == 2 → Сотрудник
            isEmployee = App.CurrentUser != null && App.CurrentUser.Rol == 2;

            if (isEmployee)
            {
                // 1) Скрываем «Добавить» и «Удалить»
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnDelete.Visibility = Visibility.Collapsed;

                // 2) Сдвигаем кнопку «Обновить» на место «Добавить»
                BtnRefresh.Margin = new Thickness(40, 0, 10, 0);

                BtnFilter.Visibility = Visibility.Visible;
                BtnFilter.Margin = new Thickness(0);

                FilterPanel.Margin = new Thickness(85, 10, 0, 0);

                // 5) Удаляем колонку «Редактировать» и добавляем справа «спэйсер» (как было):
                if (DetailsList.View is GridView gridView)
                {
                    var editCol = gridView.Columns.FirstOrDefault(c => c == EditColumn);
                    if (editCol != null)
                        gridView.Columns.Remove(editCol);

                    // Создаём пустой CellTemplate для «спэйсера»
                    var factory = new FrameworkElementFactory(typeof(TextBlock));
                    factory.SetValue(TextBlock.TextProperty, "");
                    var emptyTemplate = new DataTemplate { VisualTree = factory };

                    // Добавляем «спэйсер»-колонку шириной SpacerWidth (10px)
                    var spacer = new GridViewColumn
                    {
                        Header = string.Empty,
                        Width = SpacerWidth,
                        CellTemplate = emptyTemplate
                    };
                    gridView.Columns.Add(spacer);
                }

                // 6) Пропорции для «полезных» колонок
                columnRatios = new double[] { 0.1, 0.3, 0.2, 0.2, 0.2 };
            }
            else
            {
                // Администратор — оставляем всё как есть (6 колонок, включая «Редактировать»)
                columnRatios = new double[] { 0.1, 0.3, 0.2, 0.2, 0.15, 0.05 };
            }

            // Загрузим данные + фильтры + пагинацию
            LoadFilters();            // заполняем список фильтров (типы + производители)
            LoadDetails();
            UpdatePaginationInfo();
            UpdatePaginationButtons();

            // Подписываемся, чтобы при изменении размера ListView пересчитывать ширины колонок
            DetailsList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }

        private void UpdateGridViewColumnWidths()
        {
            if (!(DetailsList.View is GridView gridView))
                return;

            double totalWidth = DetailsList.ActualWidth;
            // «Резерв» в 10 пикселей (как раньше)
            double availableWidth = totalWidth - 10;

            if (availableWidth <= 0)
                return;

            if (isEmployee)
            {
                // У сотрудника: gridView.Columns.Count == 6 (5 «справочные» + 1 «спэйсер»)
                //    реальные: первые 5−1 = 4 (ID, Название, Тип, Производитель, Стоимость)
                //    + спэйсер: последняя колонка (индекс realCount)
                int realColumnsCount = gridView.Columns.Count - 1; // здесь 5 («полезных») − 1 = 4 реальных
                if (realColumnsCount <= 0)
                    return;

                // Вычитаем ширину «спэйсера» из доступного пространства
                double widthForReal = availableWidth - SpacerWidth;
                if (widthForReal < 0) widthForReal = 0;

                double sumRatios = columnRatios.Take(realColumnsCount).Sum(); // сумма первых 4 элементов

                for (int i = 0; i < realColumnsCount; i++)
                {
                    gridView.Columns[i].Width = widthForReal * (columnRatios[i] / sumRatios);
                }

                // Последняя (6-я) колонка — «спэйсер»
                gridView.Columns[realColumnsCount].Width = SpacerWidth;
            }
            else
            {
                // Админ: все 6 колонок, ratio.Length == 6
                int columnCount = gridView.Columns.Count;
                if (columnCount <= 0)
                    return;

                double sumRatios = columnRatios.Sum();
                for (int i = 0; i < columnCount; i++)
                {
                    gridView.Columns[i].Width = availableWidth * (columnRatios[i] / sumRatios);
                }
            }
        }

        private void LoadFilters()
        {
            // Загрузка типов деталей
            var types = App.Context.dm_Tipi_detalei
                .OrderBy(m => m.Nazvanie_tipa)
                .ToList();

            foreach (var type in types)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Nazvanie_tipa,
                    Tag = type.ID_tipa,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += TypeCheckBox_Checked;
                checkBox.Unchecked += TypeCheckBox_Unchecked;
                TypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка производителей
            var manufacturers = App.Context.dm_Proizvoditeli
                .OrderBy(m => m.Nazvanie_proizvoditelya)
                .ToList();

            foreach (var manufacturer in manufacturers)
            {
                var checkBox = new CheckBox
                {
                    Content = manufacturer.Nazvanie_proizvoditelya,
                    Tag = manufacturer.ID_proizvoditelya,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += ManufacturerCheckBox_Checked;
                checkBox.Unchecked += ManufacturerCheckBox_Unchecked;
                ManufacturerFilterPanel.Children.Add(checkBox);
            }
        }

        private void TypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                BtnResetFilters.Cursor = Cursors.Hand;
                ApplyFilters();
            }
        }

        private void TypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedTypes.Remove((int)checkBox.Tag);
                if (selectedTypes.Count == 0 && selectedManufacturers.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
                    BtnResetFilters.Cursor = Cursors.No;
                }
                ApplyFilters();
            }
        }

        private void ManufacturerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedManufacturers.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                BtnResetFilters.Cursor = Cursors.Hand;
                ApplyFilters();
            }
        }

        private void ManufacturerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedManufacturers.Remove((int)checkBox.Tag);
                if (selectedTypes.Count == 0 && selectedManufacturers.Count == 0)
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
            // Сброс фильтров по типам
            foreach (CheckBox checkBox in TypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedTypes.Clear();

            // Сброс фильтров по производителям
            foreach (CheckBox checkBox in ManufacturerFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedManufacturers.Clear();

            BtnResetFilters.IsEnabled = false;
            BtnResetFilters.Cursor = Cursors.No;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            LoadDetails();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void LoadDetails()
        {
            try
            {
                var query = App.Context.dm_Detali.AsQueryable();

                // Применение поиска
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    query = query.Where(d =>
                        d.ID_detali.ToString().Contains(currentSearchText) ||
                        d.Nazvanie.Contains(currentSearchText) ||
                        d.dm_Tipi_detalei.Nazvanie_tipa.Contains(currentSearchText) ||
                        d.dm_Proizvoditeli.Nazvanie_proizvoditelya.Contains(currentSearchText) ||
                        d.Cena.ToString().Contains(currentSearchText));
                }

                // Применение фильтров по типам
                if (selectedTypes.Count > 0)
                {
                    query = query.Where(d => selectedTypes.Contains(d.Tip));
                }

                // Применение фильтров по производителям
                if (selectedManufacturers.Count > 0)
                {
                    query = query.Where(d => selectedManufacturers.Contains(d.Proizvoditel));
                }

                totalItems = query.Count();

                var details = query
                    .OrderBy(d => d.ID_detali)
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .AsEnumerable();

                switch (currentSortColumn)
                {
                    case "ID_detali":
                        details = isAscending ?
                            details.OrderBy(d => d.ID_detali) :
                            details.OrderByDescending(d => d.ID_detali);
                        break;
                    case "Nazvanie":
                        details = isAscending ?
                            details.OrderBy(d => d.Nazvanie) :
                            details.OrderByDescending(d => d.Nazvanie);
                        break;
                    case "Cena":
                        details = isAscending ?
                            details.OrderBy(d => d.Cena) :
                            details.OrderByDescending(d => d.Cena);
                        break;
                }

                DetailsList.ItemsSource = details.ToList();
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
                        newSortColumn = "ID_detali";
                        break;
                    case "Стоимость":
                        newSortColumn = "Cena";
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

                LoadDetails();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(DetailsList))
            {
                if (header.Template.FindName("SortArrow", header) is Path sortArrow)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_detali";
                            break;
                        case "Стоимость":
                            isCurrentSortColumn = currentSortColumn == "Cena";
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
            var addEditWindow = new AddEdit_Detali();
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Detali.Count();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadDetails();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentDetail = DetailsList.SelectedItem as Entities.dm_Detali;

            if (currentDetail == null)
            {
                MessageBox.Show("Пожалуйста, выберите деталь для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isUsedInOrders = App.Context.dm_Detali_v_zakaze
                .Any(dvz => dvz.ID_detali == currentDetail.ID_detali);

            if (isUsedInOrders)
            {
                MessageBox.Show($"Невозможно удалить деталь {currentDetail.Nazvanie} (ID: {currentDetail.ID_detali}), " +
                               "так как она используется в одном или нескольких заказах.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить деталь {currentDetail.Nazvanie} (ID: {currentDetail.ID_detali})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    App.Context.dm_Detali.Remove(currentDetail);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Detali.Count();
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadDetails();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Деталь успешно удалена!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении детали:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDetails();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedDetail = (sender as Button).DataContext as Entities.dm_Detali;

            if (selectedDetail == null)
            {
                MessageBox.Show("Выберите деталь для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var detailToEdit = App.Context.dm_Detali.FirstOrDefault(d => d.ID_detali == selectedDetail.ID_detali);

            if (detailToEdit != null)
            {
                var addEditWindow = new AddEdit_Detali(detailToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadDetails();
                }
            }
            else
            {
                MessageBox.Show("Деталь не найдена в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadDetails();
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

            // Остальная логика обработки клика (из вашего кода)
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
                LoadDetails();
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
                LoadDetails();
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
            LoadDetails();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void DetailsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Если это не сотрудник, двойной клик ничего не делает:
            if (!isEmployee)
                return;

            // Получаем выделенный элемент из ListView:
            var selectedDetail = DetailsList.SelectedItem as Entities.dm_Detali;
            if (selectedDetail == null)
                return;

            // Открываем окно «AddEdit_Detali» в режиме «только просмотр»:
            var viewOnlyWindow = new AddEdit_Detali(selectedDetail, isReadOnly: true);
            viewOnlyWindow.ShowDialog();
        }
    }
}
