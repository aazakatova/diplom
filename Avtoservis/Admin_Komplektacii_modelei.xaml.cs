using Avtoservis.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.Entity;
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
    /// Логика взаимодействия для Admin_Komplektacii_modelei.xaml
    /// </summary>
    public partial class Admin_Komplektacii_modelei : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_komplektacii_avto";
        private bool isAscending = true;
        private int modelId;
        private string modelName;

        private List<int> selectedBodyTypes = new List<int>();
        private List<int> selectedGearboxTypes = new List<int>();
        private List<int> selectedDriveTypes = new List<int>();
        private List<int> selectedEngineTypes = new List<int>();

        private DispatcherTimer powerFilterTimer;
        private const int PowerFilterDelay = 500; // Задержка в миллисекундах

        // --- 1) Флаг «сотрудника» ---
        private readonly bool isEmployee;

        // --- 2) Пропорции колонок ---
        // Для администратора у нас 8 колонок (ID, Фото, Мощность, Тип кузова, Тип коробки, Тип привода, Тип двигателя, Edit).
        // Для сотрудника удалим «Edit», добавим «спэйсер» и получим 8 колонок тоже, но 7 «полезных» + 1 «спэйсер».
        private double[] columnRatios;

        // «Спэйсер»-колонка
        private const double SpacerWidth = 10;

        public Admin_Komplektacii_modelei(int modelId)
        {
            InitializeComponent();
            this.modelId = modelId;

            // --- 1.1) Определяем роль: Rol == 1 → Админ, Rol == 2 → Сотрудник. ---
            isEmployee = App.CurrentUser != null && App.CurrentUser.Rol == 2;

            // --- 1.2) Если это сотрудник, дублируем логику из Admin_Detali: hide Add/Delete, shift Refresh/Filter, удалить Edit и т.д. ---
            if (isEmployee)
            {
                // Скрываем «Добавить» и «Удалить»
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnDelete.Visibility = Visibility.Collapsed;

                // Сдвигаем кнопку «Обновить» на место «Добавить»
                BtnRefresh.Margin = new Thickness(40, 0, 10, 0);

                // Делаем кнопку «Фильтры» видимой и без маргина слева (так же, как в Admin_Detali)
                BtnFilter.Visibility = Visibility.Visible;
                BtnFilter.Margin = new Thickness(0);

                // Сдвигаем сам FilterPanel так же, как в Admin_Detali: 
                FilterPanel.Margin = new Thickness(85, 10, 0, 0);

                // Удаляем колонку «Редактировать» и добавляем справа «спэйсер»-колонку
                if (Komplektacii_List.View is GridView gridView)
                {
                    // Находим колонку «Edit» (предпоследнюю в исходном GridView) и удаляем
                    var editCol = gridView.Columns.FirstOrDefault(c => c.CellTemplate != null
                                            && c.CellTemplate.LoadContent() is Button);
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

                // Поскольку у сотрудника реальных «полезных» колонок стало 7 (ID, Фото, Мощность, Тип кузова, Тип коробки, Тип привода, Тип двигателя),
                // задаём columnRatios длиной 7 (каждую подгоняем по своему вкусу). Например:
                columnRatios = new double[] { 0.05, 0.2, 0.1, 0.15, 0.15, 0.1, 0.15 };
            }
            else
            {
                // Администратор — оставляем все 8 колонок, включая «Редактировать»
                columnRatios = new double[] { 0.05, 0.2, 0.1, 0.15, 0.15, 0.1, 0.15, 0.1 };
            }

            // --- Дальше обычная инициализация: заголовок, загрузка и т.д. ---
            var model = App.Context.dm_Modeli_avto
                .Include(m => m.dm_Marki_avto)
                .FirstOrDefault(m => m.ID_modeli_avto == modelId);
            modelName = model != null
                ? $"{model.dm_Marki_avto.Nazvanie_marki} {model.Model}"
                : "Неизвестная модель";
            TextBlockZagolovok.Text = $"Комплектации {model.Model}";

            LoadKomplektacii();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
            LoadFilters();

            Komplektacii_List.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();

            // --- 1.3) Добавляем обработчик двойного клика: если сотрудник — открываем «только для просмотра» (AddEdit) ---
            Komplektacii_List.MouseDoubleClick += Komplektacii_List_MouseDoubleClick;

            // Инициализация таймера для фильтрации по мощности
            powerFilterTimer = new DispatcherTimer();
            powerFilterTimer.Interval = TimeSpan.FromMilliseconds(PowerFilterDelay);
            powerFilterTimer.Tick += PowerFilterTimer_Tick;
        }

        private void Komplektacii_List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!isEmployee)
                return;

            var selectedKomplekt = Komplektacii_List.SelectedItem as dm_Komplektacii_avto;
            if (selectedKomplekt == null)
                return;

            // Открываем AddEdit_Komplektacii_modelei в режиме «только чтение». 
            // Предположим, что у вас есть конструктор, принимающий (dm_Komplektacii_avto, isReadOnly).
            var viewOnlyWindow = new AddEdit_Komplektacii_modelei(selectedKomplekt, isReadOnly: true);
            viewOnlyWindow.ShowDialog();
        }

        private void UpdateGridViewColumnWidths()
        {
            if (!(Komplektacii_List.View is GridView gridView))
                return;

            double totalWidth = Komplektacii_List.ActualWidth;
            // «Резерв» в 10 пикселей (как раньше)
            double availableWidth = totalWidth - 10;

            if (availableWidth <= 0)
                return;

            if (isEmployee)
            {
                // У сотрудника: gridView.Columns.Count == 8 (7 «полезных» + 1 «спэйсер»)
                //    реальные: первые 7 (ID, Фото, Мощность, Тип кузова, Тип коробки, Тип привода, Тип двигателя)
                //    + спэйсер: последняя колонка (индекс realCount)
                int realColumnsCount = gridView.Columns.Count - 1;
                if (realColumnsCount <= 0)
                    return;

                // Вычитаем ширину «спэйсера» из доступного пространства
                double widthForReal = availableWidth - SpacerWidth;
                if (widthForReal < 0) widthForReal = 0;

                double sumRatios = columnRatios.Take(realColumnsCount).Sum();

                for (int i = 0; i < realColumnsCount; i++)
                {
                    gridView.Columns[i].Width = widthForReal * (columnRatios[i] / sumRatios);
                }

                // Последняя колонка — «спэйсер»
                gridView.Columns[realColumnsCount].Width = SpacerWidth;
            }
            else
            {
                // Админ: все 8 колонок, columnRatios.Length == 8
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

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            string imagePath = System.IO.Path.Combine(projectDirectory, "Komplektacii", imageName);

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

        private void LoadKomplektacii()
        {
            try
            {
                var query = App.Context.dm_Komplektacii_avto
                    .Include(k => k.dm_Tipi_kuzova)
                    .Include(k => k.dm_Tipi_korobki_peredach)
                    .Include(k => k.dm_Tipi_privoda)
                    .Include(k => k.dm_Tipi_dvigatelya)
                    .Where(k => k.Model_avto == modelId)
                    .AsQueryable();

                // Применение поиска
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    query = query.Where(k =>
                        k.ID_komplektacii_avto.ToString().Contains(currentSearchText) ||
                        k.Moshnost.ToString().Contains(currentSearchText) ||
                        (k.dm_Tipi_kuzova != null && k.dm_Tipi_kuzova.Tip_kuzova.Contains(currentSearchText)) ||
                        (k.dm_Tipi_korobki_peredach != null && k.dm_Tipi_korobki_peredach.Tip_korobki_peredach.Contains(currentSearchText)) ||
                        (k.dm_Tipi_privoda != null && k.dm_Tipi_privoda.Tip_privoda.Contains(currentSearchText)) ||
                        (k.dm_Tipi_dvigatelya != null && k.dm_Tipi_dvigatelya.Tip_dvigatelya.Contains(currentSearchText))
                    );
                }

                // Применение фильтров по типам
                if (selectedBodyTypes.Count > 0)
                {
                    query = query.Where(k => selectedBodyTypes.Contains(k.Tip_kuzova));
                }
                if (selectedGearboxTypes.Count > 0)
                {
                    query = query.Where(k => selectedGearboxTypes.Contains(k.Tip_korobki_peredach));
                }
                if (selectedDriveTypes.Count > 0)
                {
                    query = query.Where(k => selectedDriveTypes.Contains(k.Tip_privoda));
                }
                if (selectedEngineTypes.Count > 0)
                {
                    query = query.Where(k => selectedEngineTypes.Contains(k.Tip_dvigatelya));
                }

                // Применение фильтра по мощности
                if (minPower.HasValue)
                {
                    query = query.Where(k => k.Moshnost >= minPower.Value);
                }
                if (maxPower.HasValue)
                {
                    query = query.Where(k => k.Moshnost <= maxPower.Value);
                }

                // Остальной код метода остается без изменений
                totalItems = query.Count();

                IQueryable<dm_Komplektacii_avto> sortedQuery;
                switch (currentSortColumn)
                {
                    case "Moshnost":
                        sortedQuery = isAscending ?
                            query.OrderBy(k => k.Moshnost) :
                            query.OrderByDescending(k => k.Moshnost);
                        break;
                    default:
                        sortedQuery = isAscending ?
                            query.OrderBy(k => k.ID_komplektacii_avto) :
                            query.OrderByDescending(k => k.ID_komplektacii_avto);
                        break;
                }

                var komplektacii = sortedQuery
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                foreach (var komplekt in komplektacii)
                {
                    if (!string.IsNullOrEmpty(komplekt.Foto))
                    {
                        komplekt.FotoImage = LoadImage(komplekt.Foto);
                    }
                }

                Komplektacii_List.ItemsSource = komplektacii;
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
                        newSortColumn = "ID_komplektacii_avto";
                        break;
                    case "Мощность, л.с.":
                        newSortColumn = "Moshnost";
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

                LoadKomplektacii();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(Komplektacii_List))
            {
                var sortArrow = header.Template.FindName("SortArrow", header) as System.Windows.Shapes.Path;
                if (sortArrow != null)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_komplektacii_avto";
                            break;
                        case "Мощность, л.с.":
                            isCurrentSortColumn = currentSortColumn == "Moshnost";
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
            var addEditWindow = new AddEdit_Komplektacii_modelei(modelId);
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Komplektacii_avto.Count(k => k.Model_avto == modelId);
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadKomplektacii();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentKomplekt = Komplektacii_List.SelectedItem as Entities.dm_Komplektacii_avto;

            if (currentKomplekt == null)
            {
                MessageBox.Show("Пожалуйста, выберите комплектацию для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, используется ли комплектация в таблице автомобилей
            bool isUsedInCars = App.Context.dm_Avtomobili
                .Any(a => a.Model == currentKomplekt.ID_komplektacii_avto);

            if (isUsedInCars)
            {
                MessageBox.Show($"Невозможно удалить комплектацию (ID: {currentKomplekt.ID_komplektacii_avto}), " +
                               "так как она используется в одном или нескольких автомобилях.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить выбранную комплектацию (ID: {currentKomplekt.ID_komplektacii_avto})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Удаляем только запись из базы данных, не удаляя файл фотографии
                    App.Context.dm_Komplektacii_avto.Remove(currentKomplekt);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Komplektacii_avto.Count(k => k.Model_avto == modelId);
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadKomplektacii();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Комплектация успешно удалена!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении комплектации:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadKomplektacii();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedKomplekt = (sender as Button).DataContext as Entities.dm_Komplektacii_avto;

            if (selectedKomplekt == null)
            {
                MessageBox.Show("Выберите комплектацию для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var komplektToEdit = App.Context.dm_Komplektacii_avto
                .FirstOrDefault(k => k.ID_komplektacii_avto == selectedKomplekt.ID_komplektacii_avto);

            if (komplektToEdit != null)
            {
                var addEditWindow = new AddEdit_Komplektacii_modelei(komplektToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadKomplektacii();
                }
            }
            else
            {
                MessageBox.Show("Комплектация не найдена в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadKomplektacii();
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
                LoadKomplektacii();
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
                LoadKomplektacii();
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
            LoadKomplektacii();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private int? minPower = null;
        private int? maxPower = null;

        private void LoadFilters()
        {
            // Очищаем панели фильтров
            BodyTypeFilterPanel.Children.Clear();
            GearboxTypeFilterPanel.Children.Clear();
            DriveTypeFilterPanel.Children.Clear();
            EngineTypeFilterPanel.Children.Clear();

            // Загрузка типов кузова
            var bodyTypes = App.Context.dm_Tipi_kuzova
                .OrderBy(t => t.Tip_kuzova)
                .ToList();
            foreach (var type in bodyTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_kuzova,
                    Tag = type.ID_tipa_kuzova,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += BodyTypeCheckBox_Checked;
                checkBox.Unchecked += BodyTypeCheckBox_Unchecked;
                BodyTypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка типов коробки передач
            var gearboxTypes = App.Context.dm_Tipi_korobki_peredach
                .OrderBy(t => t.Tip_korobki_peredach)
                .ToList();
            foreach (var type in gearboxTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_korobki_peredach,
                    Tag = type.ID_tipa_korobki_peredach,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += GearboxTypeCheckBox_Checked;
                checkBox.Unchecked += GearboxTypeCheckBox_Unchecked;
                GearboxTypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка типов привода
            var driveTypes = App.Context.dm_Tipi_privoda
                .OrderBy(t => t.Tip_privoda)
                .ToList();
            foreach (var type in driveTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_privoda,
                    Tag = type.ID_tipa_privoda,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += DriveTypeCheckBox_Checked;
                checkBox.Unchecked += DriveTypeCheckBox_Unchecked;
                DriveTypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка типов двигателя
            var engineTypes = App.Context.dm_Tipi_dvigatelya
                .OrderBy(t => t.Tip_dvigatelya)
                .ToList();
            foreach (var type in engineTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_dvigatelya,
                    Tag = type.ID_tipa_dvigatelya,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += EngineTypeCheckBox_Checked;
                checkBox.Unchecked += EngineTypeCheckBox_Unchecked;
                EngineTypeFilterPanel.Children.Add(checkBox);
            }
        }

        // Добавим обработчики для текстовых полей мощности
        private void PowerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (powerFilterTimer != null)
            {
                powerFilterTimer.Stop();
                powerFilterTimer.Start();
            }
        }

        private void UpdatePowerFilterValues()
        {
            var fromText = PowerFromTextBox.Text;
            var toText = PowerToTextBox.Text;

            if (PowerFromTextBox != null && fromText != "от" && !string.IsNullOrWhiteSpace(fromText))
            {
                if (int.TryParse(fromText, out int value))
                    minPower = value;
                else
                    minPower = null;
            }
            else
            {
                minPower = null;
            }

            if (PowerToTextBox != null && toText != "до" && !string.IsNullOrWhiteSpace(toText))
            {
                if (int.TryParse(toText, out int value))
                    maxPower = value;
                else
                    maxPower = null;
            }
            else
            {
                maxPower = null;
            }
        }

        private void PowerFilterTimer_Tick(object sender, EventArgs e)
        {
            if (powerFilterTimer != null)
            {
                powerFilterTimer.Stop();
            }

            // Обновляем значения minPower и maxPower
            UpdatePowerFilterValues();
            ApplyFilters();
        }

        private void PowerTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void PowerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = textBox.Tag.ToString();
                    textBox.Foreground = Brushes.Gray;
                }
                UpdatePowerFilterValues();
                ApplyFilters();
            }
        }



        private void PowerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void BodyTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedBodyTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void BodyTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedBodyTypes.Remove((int)checkBox.Tag);
                if (selectedBodyTypes.Count == 0 && selectedGearboxTypes.Count == 0 &&
                    selectedDriveTypes.Count == 0 && selectedEngineTypes.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
                }
                ApplyFilters();
            }
        }

        private void GearboxTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGearboxTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void GearboxTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGearboxTypes.Remove((int)checkBox.Tag);
                if (selectedBodyTypes.Count == 0 && selectedGearboxTypes.Count == 0 &&
                    selectedDriveTypes.Count == 0 && selectedEngineTypes.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
                }
                ApplyFilters();
            }
        }

        private void DriveTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedDriveTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void DriveTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedDriveTypes.Remove((int)checkBox.Tag);
                if (selectedBodyTypes.Count == 0 && selectedGearboxTypes.Count == 0 &&
                    selectedDriveTypes.Count == 0 && selectedEngineTypes.Count == 0)
                {
                    BtnResetFilters.IsEnabled = false;
                }
                ApplyFilters();
            }
        }

        private void EngineTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedEngineTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void EngineTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedEngineTypes.Remove((int)checkBox.Tag);
                if (selectedBodyTypes.Count == 0 && selectedGearboxTypes.Count == 0 &&
                    selectedDriveTypes.Count == 0 && selectedEngineTypes.Count == 0)
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
            // Сброс всех фильтров
            foreach (CheckBox checkBox in BodyTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in GearboxTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in DriveTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in EngineTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }

            selectedBodyTypes.Clear();
            selectedGearboxTypes.Clear();
            selectedDriveTypes.Clear();
            selectedEngineTypes.Clear();

            // Сброс фильтра по мощности
            PowerFromTextBox.Text = "от";
            PowerFromTextBox.Foreground = Brushes.Gray;
            PowerToTextBox.Text = "до";
            PowerToTextBox.Foreground = Brushes.Gray;
            minPower = null;
            maxPower = null;

            BtnResetFilters.IsEnabled = false;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            LoadKomplektacii();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
            CheckFiltersActive();
        }

        private void CheckFiltersActive()
        {
            bool hasActiveFilters = selectedBodyTypes.Count > 0 ||
                                  selectedGearboxTypes.Count > 0 ||
                                  selectedDriveTypes.Count > 0 ||
                                  selectedEngineTypes.Count > 0 ||
                                  minPower.HasValue ||
                                  maxPower.HasValue ||
                                  (PowerFromTextBox.Text != "от" && !string.IsNullOrWhiteSpace(PowerFromTextBox.Text)) ||
                                  (PowerToTextBox.Text != "до" && !string.IsNullOrWhiteSpace(PowerToTextBox.Text));

            BtnResetFilters.IsEnabled = hasActiveFilters;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
