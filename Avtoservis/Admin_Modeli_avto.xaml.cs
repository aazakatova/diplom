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
    /// Логика взаимодействия для Admin_Modeli_avto.xaml
    /// </summary>
    public partial class Admin_Modeli_avto : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_modeli_avto";
        private bool isAscending = true;

        private List<int> selectedMarks = new List<int>();
        private List<int> selectedStartYears = new List<int>();
        private List<int> selectedEndYears = new List<int>();
        private bool includeStillProduced = false;

        // Признак «сотрудника»
        private readonly bool isEmployee;

        // Пропорции для «полезных» колонок
        // Для администратора: 6 колонок (ID, Название, Марка, Год выпуска, Год окончания, Редактировать)
        // Для сотрудника: мы удалим колонку «Редактировать» и оставим 5 «полезных», затем добавим «спейсер»
        private double[] columnRatios;

        // «Спэйсер»-колонка, чтобы справа всегда оставалось ~10px
        private const double SpacerWidth = 10;

        public Admin_Modeli_avto()
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

                // После того, как вы скрыли Add/Delete и настроили Refresh, добавьте:
                BtnFilter.Visibility = Visibility.Visible;
                BtnFilter.Margin = new Thickness(0, 0, 0, 0);

                // И у себя задайте тот же самый отступ у FilterPanel, что и в Admin_Detali:
                FilterPanel.Margin = new Thickness(85, 10, 0, 0);

                // 3) Удаляем колонку «Редактировать» и добавляем справа «спэйсер»
                if (ModeliList.View is GridView gridView)
                {
                    // Найдём колонку EditColumn по имени
                    var editCol = gridView.Columns.FirstOrDefault(c => c == EditColumn);
                    if (editCol != null)
                    {
                        gridView.Columns.Remove(editCol);
                    }

                    // Создаём пустой CellTemplate для «спэйсера»
                    var factory = new FrameworkElementFactory(typeof(TextBlock));
                    factory.SetValue(TextBlock.TextProperty, "");
                    var emptyTemplate = new DataTemplate { VisualTree = factory };

                    // Добавляем «спэйсер»-колонку шириной SpacerWidth
                    var spacer = new GridViewColumn
                    {
                        Header = string.Empty,
                        Width = SpacerWidth,
                        CellTemplate = emptyTemplate
                    };
                    gridView.Columns.Add(spacer);
                }

                // 4) Пропорции для «полезных» колонок (ID, Название, Марка, Год выпуска, Год окончания)
                columnRatios = new double[] { 0.1, 0.2, 0.2, 0.15, 0.2 };
            }
            else
            {
                // Администратор — оставляем 6 колонок, включая «Редактировать»
                // Распределяем пропорции: ID, Название, Марка, Год выпуска, Год окончания, Редактировать
                columnRatios = new double[] { 0.1, 0.2, 0.2, 0.15, 0.2, 0.15 };
            }

            LoadModels();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
            LoadFilters();

            ModeliList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();

            // Добавляем обработчик двойного клика
            ModeliList.MouseDoubleClick += ModeliList_MouseDoubleClick;
        }

        private void UpdateGridViewColumnWidths()
        {
            if (!(ModeliList.View is GridView gridView))
                return;

            double totalWidth = ModeliList.ActualWidth;
            // «Резерв» в 10 пикселей
            double availableWidth = totalWidth - 10;
            if (availableWidth <= 0)
                return;

            if (isEmployee)
            {
                // У сотрудника: gridView.Columns.Count == 6 (5 «реальных» + 1 «спэйсер»)
                int realColumnsCount = gridView.Columns.Count - 1;
                if (realColumnsCount <= 0)
                    return;

                // Если колонок больше, чем элементов в columnRatios, создаём равные доли
                if (columnRatios.Length < realColumnsCount)
                {
                    columnRatios = Enumerable.Repeat(1.0 / realColumnsCount, realColumnsCount).ToArray();
                }

                // Вычитаем ширину «спэйсера» из доступного пространства
                double widthForReal = availableWidth - SpacerWidth;
                if (widthForReal < 0)
                    widthForReal = 0;

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
                // Администратор: все колонок «полезные»
                int columnCount = gridView.Columns.Count;
                if (columnCount <= 0)
                    return;

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

        // Обработчик двойного клика
        private void ModeliList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedModel = ModeliList.SelectedItem as Entities.dm_Modeli_avto;
            if (selectedModel != null)
            {
                var komplektaciiPage = new Admin_Komplektacii_modelei(selectedModel.ID_modeli_avto);
                NavigationService.Navigate(komplektaciiPage);
            }
        }

        private void LoadFilters()
        {
            // Загрузка марок автомобилей в алфавитном порядке
            var marks = App.Context.dm_Marki_avto
                .OrderBy(m => m.Nazvanie_marki)  // Сортируем по названию марки
                .ToList();

            foreach (var mark in marks)
            {
                var checkBox = new CheckBox
                {
                    Content = mark.Nazvanie_marki,
                    Tag = mark.ID_marki,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += MarkCheckBox_Checked;
                checkBox.Unchecked += MarkCheckBox_Unchecked;
                ManufacturerFilterPanel.Children.Add(checkBox);
            }

            // Загрузка годов выпуска (от текущего года до 1950)
            int currentYear = DateTime.Now.Year;
            for (int year = currentYear; year >= 1950; year--)
            {
                var checkBox = new CheckBox
                {
                    Content = year.ToString(),
                    Tag = year,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += StartYearCheckBox_Checked;
                checkBox.Unchecked += StartYearCheckBox_Unchecked;
                StartYearFilterPanel.Children.Add(checkBox);
            }


            // Добавляем чекбокс "Ещё выпускается" в панель годов окончания выпуска
            var stillProducedCheckBox = new CheckBox
            {
                Content = "Ещё выпускается",
                Margin = new Thickness(0, 5, 0, 3),
            };
            stillProducedCheckBox.Checked += StillProducedCheckBox_Checked;
            stillProducedCheckBox.Unchecked += StillProducedCheckBox_Unchecked;
            EndYearFilterPanel.Children.Add(stillProducedCheckBox);


            // Затем добавляем годы в обратном порядке (от новых к старым)
            for (int year = currentYear; year >= 1950; year--)
            {
                var checkBox = new CheckBox
                {
                    Content = year.ToString(),
                    Tag = year,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += EndYearCheckBox_Checked;
                checkBox.Unchecked += EndYearCheckBox_Unchecked;
                EndYearFilterPanel.Children.Add(checkBox);
            }
        }

        private void MarkCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedMarks.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                BtnResetFilters.Cursor = Cursors.Hand;
                ApplyFilters();
            }
        }

        private void MarkCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedMarks.Remove((int)checkBox.Tag);
                if (selectedMarks.Count == 0 && selectedStartYears.Count == 0 && selectedEndYears.Count == 0 && !includeStillProduced)
                {
                    BtnResetFilters.IsEnabled = false;
                    BtnResetFilters.Cursor = Cursors.No;
                }
                ApplyFilters();
            }
        }

        private void StartYearCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag is int year)
            {
                selectedStartYears.Add(year);
                BtnResetFilters.IsEnabled = true;
                BtnResetFilters.Cursor = Cursors.Hand;
                ApplyFilters();
            }
        }

        private void StartYearCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag is int year)
            {
                selectedStartYears.Remove(year);
                if (selectedMarks.Count == 0 && selectedStartYears.Count == 0 && selectedEndYears.Count == 0 && !includeStillProduced)
                {
                    BtnResetFilters.IsEnabled = false;
                    BtnResetFilters.Cursor = Cursors.No;
                }
                ApplyFilters();
            }
        }

        private void EndYearCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag is int year)
            {
                selectedEndYears.Add(year);
                BtnResetFilters.IsEnabled = true;
                BtnResetFilters.Cursor = Cursors.Hand;
                ApplyFilters();
            }
        }

        private void EndYearCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.Tag is int year)
            {
                selectedEndYears.Remove(year);
                if (selectedMarks.Count == 0 && selectedStartYears.Count == 0 && selectedEndYears.Count == 0 && !includeStillProduced)
                {
                    BtnResetFilters.IsEnabled = false;
                    BtnResetFilters.Cursor = Cursors.No;
                }
                ApplyFilters();
            }
        }

        private void StillProducedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            includeStillProduced = true;
            BtnResetFilters.IsEnabled = true;
            BtnResetFilters.Cursor = Cursors.Hand;
            ApplyFilters();
        }

        private void StillProducedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            includeStillProduced = false;
            if (selectedMarks.Count == 0 && selectedStartYears.Count == 0 && selectedEndYears.Count == 0)
            {
                BtnResetFilters.IsEnabled = false;
                BtnResetFilters.Cursor = Cursors.No;
            }
            ApplyFilters();
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
            // Сброс фильтров по маркам
            foreach (CheckBox checkBox in ManufacturerFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedMarks.Clear();

            // Сброс фильтров по годам выпуска
            foreach (CheckBox checkBox in StartYearFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedStartYears.Clear();

            // Сброс фильтров по годам окончания выпуска
            foreach (CheckBox checkBox in EndYearFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            selectedEndYears.Clear();
            includeStillProduced = false;

            BtnResetFilters.IsEnabled = false;
            BtnResetFilters.Cursor = Cursors.No;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            LoadModels();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void LoadModels()
        {
            try
            {
                var query = App.Context.dm_Modeli_avto.AsQueryable();

                // Применение поиска
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    query = query.Where(m =>
                        m.ID_modeli_avto.ToString().Contains(currentSearchText) ||
                        m.Model.Contains(currentSearchText) ||
                        m.dm_Marki_avto.Nazvanie_marki.Contains(currentSearchText) ||
                        m.God_vipuska.ToString().Contains(currentSearchText) ||
                        (m.God_okonchaniya_vipuska != null && m.God_okonchaniya_vipuska.ToString().Contains(currentSearchText)));
                }

                // Применение фильтров по маркам
                if (selectedMarks.Count > 0)
                {
                    query = query.Where(m => selectedMarks.Contains(m.Marka));
                }

                // Применение фильтров по годам выпуска
                if (selectedStartYears.Count > 0)
                {
                    query = query.Where(m => selectedStartYears.Contains(m.God_vipuska));
                }

                // Применение фильтров по годам окончания выпуска
                if (selectedEndYears.Count > 0)
                {
                    query = query.Where(m => m.God_okonchaniya_vipuska != null && selectedEndYears.Contains(m.God_okonchaniya_vipuska.Value));
                }

                // Применение фильтра "Ещё выпускается"
                if (includeStillProduced)
                {
                    query = query.Where(m => m.God_okonchaniya_vipuska == null);
                }

                totalItems = query.Count();

                var models = query
                    .OrderBy(m => m.ID_modeli_avto)
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .AsEnumerable();

                switch (currentSortColumn)
                {
                    case "ID_modeli_avto":
                        models = isAscending ?
                            models.OrderBy(m => m.ID_modeli_avto) :
                            models.OrderByDescending(m => m.ID_modeli_avto);
                        break;
                    case "Model":
                        models = isAscending ?
                            models.OrderBy(m => m.Model) :
                            models.OrderByDescending(m => m.Model);
                        break;
                    case "God_vipuska":
                        models = isAscending ?
                            models.OrderBy(m => m.God_vipuska) :
                            models.OrderByDescending(m => m.God_vipuska);
                        break;
                    case "God_okonchaniya_vipuska":
                        if (isAscending)
                        {
                            // По возрастанию: сначала годы по возрастанию, затем NULL (Ещё выпускается)
                            models = models.OrderBy(m => m.God_okonchaniya_vipuska ?? int.MaxValue);
                        }
                        else
                        {
                            // По убыванию: сначала NULL (Ещё выпускается), затем годы по убыванию
                            models = models.OrderByDescending(m =>
                                m.God_okonchaniya_vipuska == null ? int.MaxValue : m.God_okonchaniya_vipuska.Value);
                        }
                        break;
                }

                ModeliList.ItemsSource = models.ToList();
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
                        newSortColumn = "ID_modeli_avto";
                        break;
                    case "Год выпуска":
                        newSortColumn = "God_vipuska";
                        break;
                    case "Год окончания выпуска":
                        newSortColumn = "God_okonchaniya_vipuska";
                        break;
                    case "Название модели":
                        newSortColumn = "Model";
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

                LoadModels();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(ModeliList))
            {
                if (header.Template.FindName("SortArrow", header) is Path sortArrow)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_modeli_avto";
                            break;
                        case "Год выпуска":
                            isCurrentSortColumn = currentSortColumn == "God_vipuska";
                            break;
                        case "Год окончания выпуска":
                            isCurrentSortColumn = currentSortColumn == "God_okonchaniya_vipuska";
                            break;
                        case "Название модели":
                            isCurrentSortColumn = currentSortColumn == "Model";
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
            var addEditWindow = new AddEdit_Modeli_avto();
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Modeli_avto.Count();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadModels();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentModel = ModeliList.SelectedItem as Entities.dm_Modeli_avto;

            if (currentModel == null)
            {
                MessageBox.Show("Пожалуйста, выберите модель для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isUsedInCars = App.Context.dm_Avtomobili
                .Any(a => a.Model == currentModel.ID_modeli_avto);

            if (isUsedInCars)
            {
                MessageBox.Show($"Невозможно удалить модель {currentModel.Model} (ID: {currentModel.ID_modeli_avto}), " +
                               "так как она относится к одному или нескольким автомобилям.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить модель {currentModel.Model} (ID: {currentModel.ID_modeli_avto})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    App.Context.dm_Modeli_avto.Remove(currentModel);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Modeli_avto.Count();
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadModels();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Модель успешно удалена!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении модели:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadModels();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedModel = (sender as Button).DataContext as Entities.dm_Modeli_avto;

            if (selectedModel == null)
            {
                MessageBox.Show("Выберите модель для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var modelToEdit = App.Context.dm_Modeli_avto.FirstOrDefault(m => m.ID_modeli_avto == selectedModel.ID_modeli_avto);

            if (modelToEdit != null)
            {
                var addEditWindow = new AddEdit_Modeli_avto(modelToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadModels();
                }
            }
            else
            {
                MessageBox.Show("Модель не найдена в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadModels();
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
                LoadModels();
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
                LoadModels();
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
            LoadModels();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }
    }
}
