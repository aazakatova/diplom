using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
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
    /// Логика взаимодействия для Admin_Klienti.xaml
    /// </summary>
    public partial class Admin_Klienti : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_user";
        private bool isAscending = true;

        public Admin_Klienti()
        {
            InitializeComponent();
            LoadClients();
            UpdatePaginationInfo();
            UpdatePaginationButtons();

            KlientiList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();
        }

        private void UpdateGridViewColumnWidths()
        {
            if (KlientiList.View is GridView gridView)
            {
                double totalWidth = KlientiList.ActualWidth;
                double availableWidth = totalWidth - 10;

                if (availableWidth <= 0)
                    return;

                int columnCount = gridView.Columns.Count;
                double[] columnRatios = new double[] { 0.1, 0.15, 0.15, 0.15, 0.15, 0.2, 0.1 };
                double sumRatios = columnRatios.Sum();

                for (int i = 0; i < columnCount; i++)
                {
                    gridView.Columns[i].Width = availableWidth * (columnRatios[i] / sumRatios);
                }
            }
        }

        private void LoadClients()
        {
            try
            {
                // Получаем только клиентов (роль ID = 3)
                var clientsQuery = App.Context.dm_Users.Where(u => u.Rol == 3).AsQueryable();

                // Применяем поиск
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    clientsQuery = clientsQuery.Where(u =>
                        u.Familiya.Contains(currentSearchText) ||
                        u.Imya.Contains(currentSearchText) ||
                        u.Otchestvo.Contains(currentSearchText) ||
                        u.Nomer_telefona.Contains(currentSearchText));
                }

                totalItems = clientsQuery.Count();

                // Сортировка только по ID
                clientsQuery = isAscending ?
                    clientsQuery.OrderBy(u => u.ID_user) :
                    clientsQuery.OrderByDescending(u => u.ID_user);

                // Пагинация
                var clients = clientsQuery
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                KlientiList.ItemsSource = clients;
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
                string newSortColumn = "ID_user"; // Сортировка только по ID

                if (currentSortColumn == newSortColumn)
                {
                    isAscending = !isAscending;
                }
                else
                {
                    currentSortColumn = newSortColumn;
                    isAscending = true;
                }

                LoadClients();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(KlientiList))
            {
                if (header.Template.FindName("SortArrow", header) is Path sortArrow)
                {
                    bool isCurrentSortColumn = header.Content.ToString() == "ID" && currentSortColumn == "ID_user";
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
            var addEditWindow = new AddEdit_Klient();
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Users.Count(u => u.Rol == 3);
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadClients();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = KlientiList.SelectedItem as Entities.dm_Users; // Получаем выбранного клиента из списка
            if (selectedClient == null)
            { // Если пользователь не выбрал клиента — показываем сообщение об ошибке
                MessageBox.Show("Пожалуйста, выберите клиента для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            // Загружаем клиента из базы данных по ID
            var clientToDelete = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == selectedClient.ID_user);
            if (clientToDelete == null)
            { // Если клиент не найден в базе (возможно, уже удалён) — сообщаем об этом
                MessageBox.Show("Клиент не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            // Проверка, связан ли клиент с заказами — если да, удалять нельзя
            bool isUsedInOrders = App.Context.dm_Zakazi.Any(z => z.Klient == clientToDelete.ID_user);
            if (isUsedInOrders)
            {
                MessageBox.Show($"Невозможно удалить клиента {clientToDelete.Familiya} {clientToDelete.Imya} (ID: {clientToDelete.ID_user}), " +
                               "так как он связан с одним или несколькими заказами.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            // Подтверждение удаления через диалоговое окно
            if (MessageBox.Show($"Вы уверены, что хотите удалить клиента {clientToDelete.Familiya} {clientToDelete.Imya} (ID: {clientToDelete.ID_user})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {  // Удаление клиента из базы
                    App.Context.dm_Users.Remove(clientToDelete);
                    App.Context.SaveChanges();
                    int totalItemsAfterDelete = App.Context.dm_Users.Count(u => u.Rol == 3); // После удаления пересчитываем общее число клиентов
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);
                    if (currentPage > maxPage && maxPage > 0) // Обновляем текущую страницу, чтобы избежать пустой страницы после удаления
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }
                    LoadClients();  // Обновляем список клиентов и интерфейс
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();              // Сообщаем об успешном удалении
                    MessageBox.Show("Клиент успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {             // Обработка ошибок при удалении (например, проблемы с базой данных)
                    MessageBox.Show($"Ошибка при удалении клиента:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadClients();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = (sender as Button).DataContext as Entities.dm_Users;

            if (selectedClient == null)
            {
                MessageBox.Show("Выберите клиента для редактирования.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var clientToEdit = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == selectedClient.ID_user);

            if (clientToEdit != null)
            {
                var addEditWindow = new AddEdit_Klient(clientToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadClients();
                }
            }
            else
            {
                MessageBox.Show("Клиент не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    LoadClients();
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
                LoadClients();
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
                LoadClients();
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
            LoadClients();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private Border _currentPopup;
        private Button _currentButton;
        private ListViewItem _rightClickedItem;

        private void KlientiList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rightClickedItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (_rightClickedItem != null)
            {
                e.Handled = true;
                _rightClickedItem.IsSelected = true;

                // Получаем позицию курсора относительно всего окна
                Point cursorScreenPos = e.GetPosition(Application.Current.MainWindow);

                // Создаем контент меню (как в вашем оригинальном стиле)
                var popupContent = new StackPanel();

                var btnAvto = new Button
                {
                    Content = new TextBlock
                    {
                        Text = "Автомобили",
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Width = 150
                };
                btnAvto.Click += (s, args) => { ShowClientCars(); ClosePopup(); };

                var btnOrders = new Button
                {
                    Content = new TextBlock
                    {
                        Text = "Заказы",
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Width = 150
                };
                btnOrders.Click += (s, args) => { ShowClientOrders(); ClosePopup(); };

                popupContent.Children.Add(btnAvto);
                popupContent.Children.Add(btnOrders);

                // Показываем popup с точным позиционированием
                ShowPopupAtExactPosition(cursorScreenPos, popupContent);
            }
        }

        private void ShowPopupAtExactPosition(Point screenPosition, StackPanel content)
        {
            ClosePopup();

            var scrollViewer = new ScrollViewer
            {
                Content = content,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 300
            };

            _currentPopup = new Border
            {
                Child = scrollViewer,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5),
                Width = 150
            };

            // Конвертируем экранные координаты в координаты текущего окна
            Point windowPosition = this.PointFromScreen(screenPosition);

            // Устанавливаем позицию прямо у курсора
            Canvas.SetLeft(_currentPopup, windowPosition.X - 50);
            Canvas.SetTop(_currentPopup, windowPosition.Y - 65);

            // Корректировка, если выходит за границы окна
            if (windowPosition.X + _currentPopup.Width > this.ActualWidth)
            {
                Canvas.SetLeft(_currentPopup, this.ActualWidth - _currentPopup.Width);
            }
            if (windowPosition.Y + _currentPopup.Height > this.ActualHeight)
            {
                Canvas.SetTop(_currentPopup, this.ActualHeight - _currentPopup.Height);
            }

            PopupCanvas.Children.Add(_currentPopup);
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void ShowClientCars()
        {
            var selectedClient = KlientiList.SelectedItem as Entities.dm_Users;
            if (selectedClient == null) return;

            var page = new Admin_Avtomobili(selectedClient.ID_user);

            // если мы внутри Admin (есть FrameMain), перелетим туда,
            // иначе – навигируемся по текущему NavigationService
            if (Window.GetWindow(this) is Admin adminWindow)
            {
                adminWindow.FrameMain.Navigate(page);
            }
            else
            {
                NavigationService.Navigate(page);
            }
        }

        private void ShowClientOrders()
        {
            var selectedClient = KlientiList.SelectedItem as Entities.dm_Users;
            if (selectedClient == null) return;

            // Передаём ID текущего сотрудника и ID клиента
            int currentEmployeeId = App.CurrentUser.ID_user;
            var page = new Admin_Zakazi(currentEmployeeId, selectedClient.ID_user);

            if (Window.GetWindow(this) is Admin adminWindow)
                adminWindow.FrameMain.Navigate(page);
            else
                NavigationService.Navigate(page);
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

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentPopup != null && !_currentPopup.IsMouseOver)
            {
                ClosePopup();
            }
        }

        // Вспомогательный метод для поиска родительского элемента
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
    }
}
