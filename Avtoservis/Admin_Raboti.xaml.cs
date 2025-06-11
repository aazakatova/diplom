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
    /// Логика взаимодействия для Admin_Raboti.xaml
    /// </summary>
    public partial class Admin_Raboti : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;

        private string currentSearchText = "";

        private readonly bool isEmployee;

        // Пропорции для «полезных» колонок (ID, Наименование, Стоимость, Длительность)
        private double[] columnRatios;

        // Сколько пикселей должна занимать «спэйсер»-колонка у сотрудника
        private const double SpacerWidth = 10;

        public Admin_Raboti()
        {
            InitializeComponent();

            // Определяем роль
            // Предполагаем, что App.CurrentUser.Rol == 1 => Админ; == 2 => Сотрудник
            isEmployee = App.CurrentUser != null && App.CurrentUser.Rol == 2;

            if (isEmployee)
            {
                // 1) Скрываем «Добавить» и «Удалить»
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnDelete.Visibility = Visibility.Collapsed;

                // 2) Сдвигаем «Обновить» на место «Добавить»
                BtnRefresh.Margin = new Thickness(40, 0, 10, 0);

                // 3) Удаляем колонку «Редактировать»
                if (WorksList.View is GridView gridView)
                {
                    var editCol = gridView.Columns.FirstOrDefault(c => c == EditColumn);
                    if (editCol != null)
                        gridView.Columns.Remove(editCol);

                    // 4) Добавляем последней «пустую» колонку (заглушку) шириной SpacerWidth
                    //    Её CellTemplate будет пустым, чтобы не отображать никакие данные.
                    var factory = new FrameworkElementFactory(typeof(TextBlock));
                    factory.SetValue(TextBlock.TextProperty, "");
                    var emptyTemplate = new DataTemplate { VisualTree = factory };

                    var spacer = new GridViewColumn
                    {
                        Header = string.Empty,
                        Width = SpacerWidth,
                        CellTemplate = emptyTemplate,
                    };
                    gridView.Columns.Add(spacer);
                }

                // 5) Пропорции для 4 «настоящих» колонок
                columnRatios = new double[] { 0.1, 0.5, 0.2, 0.2 };
            }
            else
            {
                // Админ – оставляем 5 колонок, исходная пропорция
                columnRatios = new double[] { 0.1, 0.4, 0.1, 0.2, 0.2 };
            }

            LoadWorks();
            UpdatePaginationInfo();
            UpdatePaginationButtons();

            WorksList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }

        private void UpdateGridViewColumnWidths()
        {
            if (!(WorksList.View is GridView gridView))
                return;

            double totalWidth = WorksList.ActualWidth;
            // «Резерв» в 10 пикселей, как раньше
            double availableWidth = totalWidth - 10;

            if (availableWidth <= 0)
                return;

            if (isEmployee)
            {
                // У сотрудника в gridView.Columns: 4 реальных + 1 «спэйсер»
                int realColumnsCount = gridView.Columns.Count - 1; // здесь будет 4
                if (realColumnsCount <= 0)
                    return;

                // Сколько места остаётся после «спэйсера»
                double widthForReal = availableWidth - SpacerWidth;
                if (widthForReal <= 0)
                    widthForReal = 0;

                double sumRatios = columnRatios.Sum(); // сумма элементов массива columnRatios (4 элемента)

                // Распределяем widthForReal между первыми realColumnsCount колонками
                for (int i = 0; i < realColumnsCount; i++)
                {
                    gridView.Columns[i].Width = widthForReal * (columnRatios[i] / sumRatios);
                }

                // Последняя колонка – «спэйсер»
                gridView.Columns[realColumnsCount].Width = SpacerWidth;
            }
            else
            {
                // Для администратора: gridView.Columns.Count == columnRatios.Length (==5)
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

        private void LoadWorks()
        {
            try
            {
                // Получаем базовый запрос
                var query = App.Context.dm_Raboti.AsQueryable();

                // Применяем поиск, если есть текст (исключая placeholder)
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    query = query.Where(w =>
                    w.ID_raboti.ToString().Contains(currentSearchText) ||
                    w.Dlitelnost.ToString().Contains(currentSearchText) ||
                        w.Naimenovanie.Contains(currentSearchText) ||
                        w.Stoimost.ToString().Contains(currentSearchText));
                }

                // Получаем общее количество с учетом поиска
                totalItems = query.Count();

                // Получаем данные для текущей страницы
                var works = query
                    .OrderBy(w => w.ID_raboti) // базовая сортировка
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .AsEnumerable();

                // Применяем сортировку к текущей странице
                switch (currentSortColumn)
                {
                    case "ID_raboti":
                        works = isAscending ?
                            works.OrderBy(w => w.ID_raboti) :
                            works.OrderByDescending(w => w.ID_raboti);
                        break;
                    case "Naimenovanie":
                        works = isAscending ?
                            works.OrderBy(w => w.Naimenovanie) :
                            works.OrderByDescending(w => w.Naimenovanie);
                        break;
                    case "Stoimost":
                        works = isAscending ?
                            works.OrderBy(w => w.Stoimost) :
                            works.OrderByDescending(w => w.Stoimost);
                        break;
                    case "Dlitelnost":
                        works = isAscending ?
                            works.OrderBy(w => w.Dlitelnost) :
                            works.OrderByDescending(w => w.Dlitelnost);
                        break;
                }

                WorksList.ItemsSource = works.ToList();
                UpdateSortIndicators();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string currentSortColumn = "ID_raboti";
        private bool isAscending = true;

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header)
            {
                string newSortColumn;
                switch (header.Content.ToString())
                {
                    case "ID":
                        newSortColumn = "ID_raboti";
                        break;
                    case "Наименование":
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
                LoadWorks();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(WorksList))
            {
                if (header.Template.FindName("SortArrow", header) is Path sortArrow)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_raboti";
                            break;
                        case "Наименование":
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

        // Вспомогательный метод для поиска элементов в визуальном дереве
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
            var addEditWindow = new AddEdit_Raboti();
            addEditWindow.Closed += (s, args) =>
            {
                // Обновляем общее количество работ
                totalItems = App.Context.dm_Raboti.Count();

                // Пересчитываем пагинацию
                UpdatePaginationInfo();
                UpdatePaginationButtons();

                // Обновляем список работ (чтобы новая работа отобразилась)
                LoadWorks();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentWork = WorksList.SelectedItem as Entities.dm_Raboti;

            if (currentWork == null)
            {
                MessageBox.Show("Пожалуйста, выберите работу для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, есть ли связанные записи в dm_Raboti_v_zakaze
            bool isUsedInOrders = App.Context.dm_Raboti_v_zakaze
                .Any(rvz => rvz.ID_raboti == currentWork.ID_raboti);

            if (isUsedInOrders)
            {
                MessageBox.Show($"Невозможно удалить работу {currentWork.Naimenovanie} (ID: {currentWork.ID_raboti}), " +
                               "так как она используется в одном или нескольких заказах.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Подтверждение удаления
            if (MessageBox.Show($"Вы уверены, что хотите удалить работу {currentWork.Naimenovanie} (ID: {currentWork.ID_raboti})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    App.Context.dm_Raboti.Remove(currentWork);
                    App.Context.SaveChanges();

                    // Проверяем, не осталось ли пустой страницы после удаления
                    int totalItemsAfterDelete = App.Context.dm_Raboti.Count();
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage; // Переходим на предыдущую страницу, если текущая стала пустой
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1; // Если удалили все записи
                    }

                    // Обновляем данные и пагинацию
                    LoadWorks();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Работа успешно удалена!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении работы:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadWorks();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedWork = (sender as Button).DataContext as Entities.dm_Raboti;

            if (selectedWork == null)
            {
                MessageBox.Show("Выберите работу для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var workToEdit = App.Context.dm_Raboti.FirstOrDefault(s => s.ID_raboti == selectedWork.ID_raboti);

            if (workToEdit != null)
            {
                var addEditWindow = new AddEdit_Raboti(workToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadWorks();
                }
            }
            else
            {
                MessageBox.Show("Работа не найдена в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime lastPopupInteractionTime = DateTime.MinValue;

        private void ItemsPerPageButton_Click(object sender, RoutedEventArgs e)
        {
            // Защита от быстрых повторных кликов
            if ((DateTime.Now - lastPopupInteractionTime).TotalMilliseconds < 200)
                return;

            lastPopupInteractionTime = DateTime.Now;

            var button = sender as Button;

            // Если попап уже открыт для этой кнопки - закрываем его
            if (itemsPerPagePopup != null && itemsPerPagePopup.IsOpen && currentButton == button)
            {
                itemsPerPagePopup.IsOpen = false;
                return;
            }

            // Закрываем предыдущий попап, если он был открыт
            if (itemsPerPagePopup != null)
            {
                itemsPerPagePopup.IsOpen = false;
            }

            // Запоминаем текущую кнопку
            currentButton = button;

            // Создаем новый попап
            itemsPerPagePopup = new Popup
            {
                PlacementTarget = currentButton,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                Width = currentButton.ActualWidth,
                IsOpen = true
            };

            // Обработчик закрытия попапа
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
                    LoadWorks();
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


            // Проверяем, был ли клик внутри кнопки или попапа
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

            // Если клик был снаружи - закрываем попап
            if (!isClickInside && itemsPerPagePopup != null && itemsPerPagePopup.IsOpen)
            {
                itemsPerPagePopup.IsOpen = false;
            }
        }

        private void UpdatePaginationButtons()
        {
            // Кнопка "Назад"
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

            // Кнопка "Вперед"
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
                LoadWorks();
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
                LoadWorks();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            // Игнорируем placeholder
            if (textBox.Text == "Поиск...")
            {
                currentSearchText = "";
                return;
            }

            currentSearchText = textBox.Text;
            currentPage = 1; // Сбрасываем на первую страницу при поиске
            LoadWorks();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }
    }
}
