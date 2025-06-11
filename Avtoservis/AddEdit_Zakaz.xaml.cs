using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для AddEdit_Zakaz.xaml
    /// </summary>
    public partial class AddEdit_Zakaz : Window
    {
        private Entities.dm_Zakazi _currentOrder;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;
        private bool _isEmployeeEditable = true;
        private int? _fixedEmployeeId;
        private int? _fixedClientId;

        // Выбранные значения
        private Entities.dm_Users _selectedClient;
        private Entities.dm_Avtomobili _selectedAuto;
        private Entities.dm_Rabochie_mesta _selectedWorkPlace;
        private Entities.dm_Users _selectedEmployee;


        // Поля для хранения временных данных
        private DateTime? _newClientBirthDate;
        private Entities.dm_Marki_avto _newAutoSelectedMarka;
        private Entities.dm_Modeli_avto _newAutoSelectedModel;
        private Entities.dm_Komplektacii_avto _newAutoSelectedKomplektaciya;


        // Коллекции для данных
        private ObservableCollection<Entities.dm_Raboti_v_zakaze> _services = new ObservableCollection<Entities.dm_Raboti_v_zakaze>();
        private ObservableCollection<Entities.dm_Detali_v_zakaze> _details = new ObservableCollection<Entities.dm_Detali_v_zakaze>();
        private ObservableCollection<PhotoItem> _photos = new ObservableCollection<PhotoItem>();

        // Время работы сервиса
        private const int StartHour = 9;
        private const int EndHour = 19;

        private readonly string[] _orderStatuses = { "Принят", "В работе", "Завершён", "Отменён" };
        private readonly string[] _paymentStatuses = { "Оплачен", "Не оплачен" };

        public bool IsEmployeeEditable
        {
            get { return _isEmployeeEditable; }
            set
            {
                _isEmployeeEditable = value;
                BtnEmployee.IsEnabled = value;
            }
        }

        public AddEdit_Zakaz(Entities.dm_Zakazi order, int? fixedEmployeeId = null, int? fixedClientId = null)
            : this(fixedEmployeeId, fixedClientId)
        {
            _currentOrder = order;
            _isEditing = true;
            FillForm(_currentOrder);
            TextBlockZagolovok.Text = $"Редактирование заказ-наряда №{order.ID_zakaza} от {order.Data_sozdaniya:dd.MM.yyyy}";

            // Если вошли под сотрудником — блокируем и меняем цвет кнопки «Исполнитель»
            if (App.CurrentUser != null && App.CurrentUser.Rol == 2)
            {
                BtnEmployee.IsEnabled = false;
                BtnEmployee.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#fafafa"));
                BtnEmployee.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFCACACA"));
                BtnEmployee.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFCACACA"));
            }
        }

        public AddEdit_Zakaz(int? fixedEmployeeId = null, int? fixedClientId = null)
        {
            InitializeComponent();
            _isEditing = false;
            _fixedEmployeeId = fixedEmployeeId;

            // Если передан ID клиента, загружаем его данные
            if (fixedClientId.HasValue)
            {
                _selectedClient = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == fixedClientId.Value);
                if (_selectedClient != null)
                {
                    // Устанавливаем текст кнопки
                    BtnClient.Content = $"{_selectedClient.Familiya} {_selectedClient.Imya} {_selectedClient.Otchestvo}";
                    // Блокируем выбор
                    BtnClient.IsEnabled = false;
                    BtnClient.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fafafa"));
                    BtnClient.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
                    BtnClient.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
                    BtnAddClient.IsEnabled = false;

                    // Показываем контактную информацию
                    ClientContactInfo.Text = $"Тел: {_selectedClient.Nomer_telefona}";
                    ClientContactPanel.Visibility = Visibility.Visible;
                    AutoPanel.Visibility = Visibility.Visible;
                }
            }

            // Если передан ID сотрудника, загружаем его данные
            if (_fixedEmployeeId.HasValue)
            {
                _selectedEmployee = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == _fixedEmployeeId.Value);
                if (_selectedEmployee != null)
                {
                    // Устанавливаем текст кнопки
                    BtnEmployee.Content = $"{_selectedEmployee.Familiya} {_selectedEmployee.Imya} {_selectedEmployee.Otchestvo}";
                    // Блокируем выбор
                    BtnEmployee.IsEnabled = false;
                    BtnEmployee.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fafafa"));
                    BtnEmployee.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
                    BtnEmployee.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
                }
            }

            InitializeTimeComboBoxes();
            InitializeDataGrids();

            TextBlockZagolovok.Text = "Добавление заказ-наряда";
        }

        private void InitializeTimeComboBoxes()
        {
            // Заполняем часы (9-19)
            for (int hour = StartHour; hour <= EndHour; hour++)
            {
                ComboHourPriem.Items.Add(hour.ToString("00"));
                ComboHourVidacha.Items.Add(hour.ToString("00"));
            }

            // Заполняем минуты (00-59 с шагом 1 минута)
            for (int minute = 0; minute < 60; minute++)
            {
                ComboMinutePriem.Items.Add(minute.ToString("00"));
                ComboMinuteVidacha.Items.Add(minute.ToString("00"));
            }

            // Устанавливаем текущее время, если оно в пределах рабочего времени
            var now = DateTime.Now;
            if (now.Hour >= StartHour && now.Hour < EndHour)
            {
                BtnDatePriem.Content = now.ToString("dd.MM.yyyy");
                ComboHourPriem.SelectedItem = now.Hour.ToString("00");
                ComboMinutePriem.SelectedItem = now.Minute.ToString("00");

                UpdateIssueDateTime();
            }
            else
            {
                // Иначе устанавливаем начало рабочего дня
                BtnDatePriem.Content = DateTime.Today.ToString("dd.MM.yyyy");
                ComboHourPriem.SelectedItem = StartHour.ToString("00");
                ComboMinutePriem.SelectedItem = "00";

                UpdateIssueDateTime();
            }
        }

        private void BtnDatePriem_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

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
                FontFamily = this.FontFamily, 
                Height = 160,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                FontSize = 25
            };

            // Устанавливаем выбранную дату, если она есть
            DateTime currentDate;
            if (DateTime.TryParse(BtnDatePriem.Content.ToString(), out currentDate))
            {
                calendar.SelectedDate = currentDate;
                calendar.DisplayDate = currentDate;
            }

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    BtnDatePriem.Content = calendar.SelectedDate.Value.ToString("dd.MM.yyyy");
                    UpdateIssueDateTime();
                    ClosePopup();
                }
            };

            _currentPopup = new Border
            {
                Style = (Style)FindResource("PopupBlockStyle"),
                Child = calendar,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray
            };

            PopupCanvas.Children.Add(_currentPopup);
            Point buttonPos = button.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 5);
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void BtnDateVidacha_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

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
                FontFamily = this.FontFamily,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                FontSize = 25
            };

            // Устанавливаем выбранную дату, если она есть
            DateTime currentDate;
            if (DateTime.TryParse(BtnDateVidacha.Content.ToString(), out currentDate))
            {
                calendar.SelectedDate = currentDate;
                calendar.DisplayDate = currentDate;
            }

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    BtnDateVidacha.Content = calendar.SelectedDate.Value.ToString("dd.MM.yyyy");
                    ClosePopup();
                }
            };

            _currentPopup = new Border
            {
                Style = (Style)FindResource("PopupBlockStyle"),
                Child = calendar,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray
            };

            PopupCanvas.Children.Add(_currentPopup);
            Point buttonPos = button.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 5);
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void UpdateIssueDateTime()
        {
            try
            {
                DateTime priemDate;
                if (!DateTime.TryParse(BtnDatePriem.Content.ToString(), out priemDate))
                    return;

                int hour, minute;
                if (!int.TryParse(ComboHourPriem.SelectedItem?.ToString(), out hour) ||
                    !int.TryParse(ComboMinutePriem.SelectedItem?.ToString(), out minute))
                    return;

                DateTime startTime = priemDate.Date
                    .AddHours(hour)
                    .AddMinutes(minute);

                // Суммируем время всех услуг (включая дубликаты)
                int totalMinutes = _services.Sum(s => s.dm_Raboti.Dlitelnost ?? 0);
                DateTime endTime = startTime.AddMinutes(totalMinutes);

                if (endTime.Hour >= EndHour)
                {
                    endTime = endTime.Date.AddHours(EndHour).AddMinutes(0);
                }

                BtnDateVidacha.Content = endTime.ToString("dd.MM.yyyy");
                ComboHourVidacha.SelectedItem = endTime.Hour.ToString("00");
                ComboMinuteVidacha.SelectedItem = endTime.Minute.ToString("00");
            }
            catch
            {
                // В случае ошибки оставляем как есть
            }
        }

        private DateTime GetDateTimeFromButtonAndComboBoxes(Button dateButton, ComboBox hourCombo, ComboBox minuteCombo)
        {
            DateTime date;
            if (!DateTime.TryParse(dateButton.Content.ToString(), out date))
                date = DateTime.Today;

            int hour, minute;
            if (!int.TryParse(hourCombo.SelectedItem?.ToString(), out hour))
                hour = StartHour;
            if (!int.TryParse(minuteCombo.SelectedItem?.ToString(), out minute))
                minute = 0;

            return date.Date
                .AddHours(hour)
                .AddMinutes(minute);
        }


        private void InitializeDataGrids()
        {
            ServicesContainer.ItemsSource = _services;
            DetailsContainer.ItemsSource = _details;


            PhotosContainer.ItemsSource = _photos;

            _services.CollectionChanged += (s, e) => CalculateTotal();
            _details.CollectionChanged += (s, e) => CalculateTotal();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (button == BtnClient && _fixedClientId.HasValue)
            {
                MessageBox.Show("Нельзя изменить клиента при создании заказа из профиля клиента",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_currentPopup != null && _currentButton == button)
            {
                ClosePopup();
                return;
            }

            if (button == BtnClient)
            {
                ShowClientSelectionPopup();
            }
            else if (button == BtnAuto && _selectedClient != null)
            {
                ShowAutoSelectionPopup();
            }
            else if (button == BtnWorkPlace)
            {
                ShowWorkPlaceSelectionPopup();
            }
            else if (button == BtnEmployee)
            {
                ShowEmployeeSelectionPopup();
            }
        }

        private void ShowClientSelectionPopup()
        {
            var clients = App.Context.dm_Users
                .Where(u => u.dm_Roli.Rol == "Клиент")
                .OrderBy(u => u.Familiya)
                .ThenBy(u => u.Imya)
                .ToList();

            var popupContent = new StackPanel { Background = Brushes.White };

            foreach (var client in clients)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = $"{client.Familiya} {client.Imya} {client.Otchestvo}",
                        Margin = new Thickness(10, 0, 0, 0) // отступ слева у текста
                    },
                    Tag = client,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                };
                btn.Click += (s, e) =>
                {
                    _selectedClient = (Entities.dm_Users)((Button)s).Tag;
                    BtnClient.Content = $"{_selectedClient.Familiya} {_selectedClient.Imya} {_selectedClient.Otchestvo}";
                    ClientContactInfo.Text = $"Тел: {_selectedClient.Nomer_telefona}";
                    ClientContactPanel.Visibility = Visibility.Visible;
                    AutoPanel.Visibility = Visibility.Visible;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            // Передаём StackPanel напрямую в ShowPopup — он обернёт в ScrollViewer с MaxHeight
            ShowPopup(BtnClient, popupContent);
        }

        private void ShowAutoSelectionPopup()
        {
            var autos = App.Context.dm_Avtomobili
                .Include("dm_Komplektacii_avto")
                .Include("dm_Komplektacii_avto.dm_Modeli_avto")
                .Include("dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto")
                .Where(a => a.Vladelec == _selectedClient.ID_user)
                .OrderBy(a => a.dm_Komplektacii_avto.dm_Modeli_avto.Model)
                .ToList();

            var popupContent = new StackPanel();

            foreach (var auto in autos)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = $"{auto.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki} " +
                               $"{auto.dm_Komplektacii_avto.dm_Modeli_avto.Model} ({auto.Gos_nomer})",
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = auto,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += (s, e) =>
                {
                    _selectedAuto = (Entities.dm_Avtomobili)((Button)s).Tag;
                    BtnAuto.Content = $"{_selectedAuto.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki} " +
                                     $"{_selectedAuto.dm_Komplektacii_avto.dm_Modeli_avto.Model}";
                    AutoInfo.Text = $"VIN: {_selectedAuto.WIN_nomer ?? "не указан"}, " +
                                   $"Гос. номер: {_selectedAuto.Gos_nomer ?? "не указан"}";
                    AutoInfoPanel.Visibility = Visibility.Visible;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnAuto, popupContent);
        }

        private void ShowWorkPlaceSelectionPopup()
        {
            var workPlaces = App.Context.dm_Rabochie_mesta
                .OrderBy(w => w.Rabochee_mesto)
                .ToList();

            var popupContent = new StackPanel();

            foreach (var wp in workPlaces)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = wp.Rabochee_mesto,
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = wp,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += (s, e) =>
                {
                    _selectedWorkPlace = (Entities.dm_Rabochie_mesta)((Button)s).Tag;
                    BtnWorkPlace.Content = _selectedWorkPlace.Rabochee_mesto;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnWorkPlace, popupContent);
        }

        private void ShowEmployeeSelectionPopup()
        {
            var employees = App.Context.dm_Users
                .Where(u => u.dm_Roli.Rol == "Сотрудник")
                .OrderBy(u => u.Familiya)
                .ThenBy(u => u.Imya)
                .ToList();

            var popupContent = new StackPanel { Background = Brushes.White };

            foreach (var emp in employees)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = $"{emp.Familiya} {emp.Imya} {emp.Otchestvo}",
                        Margin = new Thickness(10, 0, 0, 0) // отступ слева
                    },
                    Tag = emp,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    MinWidth = BtnEmployee.ActualWidth
                };

                // Если это нужный сотрудник, выделяем:
                if (_fixedEmployeeId.HasValue && emp.ID_user == _fixedEmployeeId.Value)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE1F5FE"));
                }

                btn.Click += (s, e) =>
                {
                    _selectedEmployee = (Entities.dm_Users)((Button)s).Tag;
                    BtnEmployee.Content = $"{_selectedEmployee.Familiya} {_selectedEmployee.Imya} {_selectedEmployee.Otchestvo}";
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            // Передаём StackPanel в ShowPopup — ограничение по высоте появится там
            ShowPopup(BtnEmployee, popupContent);
        }

        private void ShowPopup(Button button, StackPanel content)
        {
            ClosePopup();
            _currentButton = button;

            var scrollViewer = new ScrollViewer
            {
                Content = content,
                MaxHeight = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _currentPopup = new Border
            {
                Child = scrollViewer,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5),
                Width = button.ActualWidth
            };

            // Позиционирование относительно экрана
            Point buttonPos = button.PointToScreen(new Point(0, 0));
            Point windowPos = this.PointToScreen(new Point(0, 0));
            double relativeX = buttonPos.X - windowPos.X;
            double relativeY = buttonPos.Y - windowPos.Y + button.ActualHeight + 2;

            Canvas.SetLeft(_currentPopup, relativeX);
            Canvas.SetTop(_currentPopup, relativeY);

            PopupCanvas.Children.Add(_currentPopup);
            this.PreviewMouseDown += Window_PreviewMouseDown;
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
            if (_currentPopup != null && !_currentPopup.IsMouseOver &&
                (_currentButton == null || !_currentButton.IsMouseOver))
            {
                ClosePopup();
            }
        }

        private void FillForm(Entities.dm_Zakazi order)
        {
            // Заполняем клиента
            if (order.Klient != 0)
            {
                var client = App.Context.dm_Users.Find(order.Klient);
                if (client != null)
                {
                    _selectedClient = client;
                    BtnClient.Content = $"{client.Familiya} {client.Imya} {client.Otchestvo}";
                    ClientContactInfo.Text = $"Тел: {client.Nomer_telefona}";
                    ClientContactPanel.Visibility = Visibility.Visible;
                    AutoPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                BtnClient.Content = "Выберите клиента";
            }

            if (App.CurrentUser != null && App.CurrentUser.Rol == 2)
            {
                BtnEmployee.IsEnabled = false;
                BtnEmployee.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#fafafa"));
                BtnEmployee.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFCACACA"));
                BtnEmployee.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFCACACA"));
            }

            // Заполняем автомобиль
            if (order.Avtomobil.HasValue)
            {
                var auto = App.Context.dm_Avtomobili
                    .Include("dm_Komplektacii_avto")
                    .Include("dm_Komplektacii_avto.dm_Modeli_avto")
                    .Include("dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto")
                    .FirstOrDefault(a => a.ID_avtomobilya == order.Avtomobil);

                if (auto != null)
                {
                    _selectedAuto = auto;
                    BtnAuto.Content = $"{auto.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki} " +
                                     $"{auto.dm_Komplektacii_avto.dm_Modeli_avto.Model}";
                    AutoInfo.Text = $"VIN: {auto.WIN_nomer ?? "не указан"}, " +
                                   $"Гос. номер: {auto.Gos_nomer ?? "не указан"}";
                    AutoInfoPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                BtnAuto.Content = "Выберите автомобиль";
            }

            // Заполняем рабочее место
            if (order.Rabochee_mesto != 0)
            {
                var wp = App.Context.dm_Rabochie_mesta.Find(order.Rabochee_mesto);
                if (wp != null)
                {
                    _selectedWorkPlace = wp;
                    BtnWorkPlace.Content = wp.Rabochee_mesto;
                }
            }

            // Заполняем исполнителя
            if (order.Ispolnitel.HasValue)
            {
                var emp = App.Context.dm_Users.Find(order.Ispolnitel);
                if (emp != null)
                {
                    _selectedEmployee = emp;
                    BtnEmployee.Content = $"{emp.Familiya} {emp.Imya} {emp.Otchestvo}";
                }
            }

            // Заполняем даты
            BtnDatePriem.Content = order.Data_i_vremya_priema_avto.ToString("dd.MM.yyyy");
            ComboHourPriem.SelectedItem = order.Data_i_vremya_priema_avto.Hour.ToString("00");
            ComboMinutePriem.SelectedItem = order.Data_i_vremya_priema_avto.Minute.ToString("00");

            BtnDateVidacha.Content = order.Data_i_vremya_vidachi_avto.ToString("dd.MM.yyyy");
            ComboHourVidacha.SelectedItem = order.Data_i_vremya_vidachi_avto.Hour.ToString("00");
            ComboMinuteVidacha.SelectedItem = order.Data_i_vremya_vidachi_avto.Minute.ToString("00");

            BtnOrderStatus.Content = order.Status;
            BtnPaymentStatus.Content = order.Oplata ? "Оплачен" : "Не оплачен";

            // Заполняем услуги
            var services = App.Context.dm_Raboti_v_zakaze
                .Include("dm_Raboti")
                .Where(r => r.ID_zakaza == order.ID_zakaza)
                .ToList();

            foreach (var service in services)
            {
                _services.Add(service);
            }

            // Заполняем детали
            var details = App.Context.dm_Detali_v_zakaze
                .Include("dm_Detali")
                .Where(d => d.ID_zakaza == order.ID_zakaza)
                .ToList();

            foreach (var detail in details)
            {
                _details.Add(detail);
            }

            // Заполняем фотографии
            var photos = App.Context.dm_Foto_v_zakaze
                .Where(f => f.ID_zakaza == order.ID_zakaza)
                .ToList();

            foreach (var photo in photos)
            {
                // Собираем полный путь к фото
                string fullPhotoPath = System.IO.Path.Combine(GetPhotosFolderPath(), photo.Foto);
                _photos.Add(new PhotoItem { ImagePath = fullPhotoPath });
            }

            CalculateTotal();
        }

        private void BtnAddService_Click(object sender, RoutedEventArgs e)
        {
            var selectWindow = new SelectServicesWindow();
            if (selectWindow.ShowDialog() == true)
            {
                foreach (var service in selectWindow.SelectedServices)
                {
                    // Просто добавляем услугу, даже если она уже есть в списке
                    _services.Add(new Entities.dm_Raboti_v_zakaze
                    {
                        ID_raboti = service.ID_raboti,
                        dm_Raboti = service,
                        Zakrep_stoimost = service.Stoimost
                    });
                }

                UpdateIssueDateTime();
            }
        }

        private void BtnRemoveService_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var service = (Entities.dm_Raboti_v_zakaze)button.DataContext;
            _services.Remove(service);
            UpdateIssueDateTime();
        }

        private void BtnAddDetail_Click(object sender, RoutedEventArgs e)
        {
            var selectWindow = new SelectDetailsWindow();
            if (selectWindow.ShowDialog() == true)
            {
                foreach (var detail in selectWindow.SelectedDetails)
                {
                    var existing = _details.FirstOrDefault(d => d.ID_detali == detail.ID_detali);
                    if (existing == null)
                    {
                        _details.Add(new Entities.dm_Detali_v_zakaze
                        {
                            ID_detali = detail.ID_detali,
                            dm_Detali = detail,
                            Zakrep_cena = detail.Cena,
                            Kolichestvo = 1,
                            Detal_klienta = false
                        });
                    }
                }
            }
        }

        private void BtnRemoveDetail_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var detail = (Entities.dm_Detali_v_zakaze)button.DataContext;
            _details.Remove(detail);
        }

        private void BtnAddPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filename in openFileDialog.FileNames)
                {
                    // Проверяем, не находится ли файл уже в папке OrderPhotos
                    string photosFolder = GetPhotosFolderPath();
                    if (filename.StartsWith(photosFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        // Если файл уже в нужной папке, используем его напрямую
                        string relativePath = filename.Substring(photosFolder.Length).TrimStart(System.IO.Path.DirectorySeparatorChar);
                        _photos.Add(new PhotoItem { ImagePath = relativePath });
                    }
                    else
                    {
                        // Иначе добавляем с обычной обработкой
                        _photos.Add(new PhotoItem { ImagePath = filename });
                    }
                }
            }
        }

        private void BtnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var photo = (PhotoItem)button.DataContext;
            _photos.Remove(photo);
        }

        private void RefreshDetailsBindings()
        {
            var temp = _details.ToList();
            _details.Clear();
            foreach (var item in temp) _details.Add(item);
            CalculateTotal();
        }


        private void DetailClient_Checked(object sender, RoutedEventArgs e)
        {
            RefreshDetailsBindings();
        }

        private void DetailClient_Unchecked(object sender, RoutedEventArgs e)
        {
            RefreshDetailsBindings();
        }


        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var detail = (Entities.dm_Detali_v_zakaze)button.DataContext;
            detail.Kolichestvo++;
            RefreshDetailsBindings();
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var detail = (Entities.dm_Detali_v_zakaze)button.DataContext;

            if (detail.Kolichestvo > 1)
            {
                detail.Kolichestvo--;
                RefreshDetailsBindings();
            }
        }

        private void CalculateTotal()
        {
            decimal total = 0;

            // Сумма всех услуг (включая дубликаты)
            total += _services.Sum(s => s.Zakrep_stoimost ?? 0);

            // Сумма деталей
            total += _details.Where(d => !d.Detal_klienta)
                           .Sum(d => (d.Zakrep_cena ?? 0) * d.Kolichestvo);

            TotalSumText.Text = $"{total:N2} ₽";
        }

        private bool ValidateFields()
        {
            // Проверка обязательных полей
            if (_selectedClient == null)
            {
                MessageBox.Show("Необходимо выбрать клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (_selectedAuto == null)
            {
                MessageBox.Show("Необходимо выбрать автомобиль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (_selectedWorkPlace == null)
            {
                MessageBox.Show("Необходимо выбрать рабочее место", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (_selectedEmployee == null)
            {
                MessageBox.Show("Необходимо выбрать исполнителя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка даты и времени приёма
            DateTime priemDate;
            if (!DateTime.TryParse(BtnDatePriem.Content.ToString(), out priemDate))
            {
                MessageBox.Show("Необходимо указать дату приёма", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            int priemHour, priemMinute;
            if (ComboHourPriem.SelectedItem == null || ComboMinutePriem.SelectedItem == null ||
                !int.TryParse(ComboHourPriem.SelectedItem.ToString(), out priemHour) ||
                !int.TryParse(ComboMinutePriem.SelectedItem.ToString(), out priemMinute))
            {
                MessageBox.Show("Необходимо указать время приёма", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка даты и времени выдачи
            DateTime vidachaDate;
            if (!DateTime.TryParse(BtnDateVidacha.Content.ToString(), out vidachaDate))
            {
                MessageBox.Show("Необходимо указать дату выдачи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            int vidachaHour, vidachaMinute;
            if (ComboHourVidacha.SelectedItem == null || ComboMinuteVidacha.SelectedItem == null ||
                !int.TryParse(ComboHourVidacha.SelectedItem.ToString(), out vidachaHour) ||
                !int.TryParse(ComboMinuteVidacha.SelectedItem.ToString(), out vidachaMinute))
            {
                MessageBox.Show("Необходимо указать время выдачи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка времени работы сервиса (с 9:00 до 19:00)
            if (priemHour < StartHour || priemHour >= EndHour || (priemHour == EndHour - 1 && priemMinute > 0))
            {
                MessageBox.Show($"Время приёма должно быть с {StartHour}:00 до {EndHour}:00", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка доступности рабочего места
            DateTime startTime = priemDate.Date
                .AddHours(priemHour)
                .AddMinutes(priemMinute);

            DateTime endTime = vidachaDate.Date
                .AddHours(vidachaHour)
                .AddMinutes(vidachaMinute);

            // Проверяем, не пересекается ли новый заказ с существующими
            int currentOrderId = _currentOrder != null ? _currentOrder.ID_zakaza : 0;

            // Сначала проверяем, что рабочее место выбрано
            if (_selectedWorkPlace == null)
            {
                MessageBox.Show("Необходимо выбрать рабочее место", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var query = App.Context.dm_Zakazi.AsQueryable();

            // Проверяем условие с рабочим местом
            query = query.Where(z => z.Rabochee_mesto == _selectedWorkPlace.ID_rabochego_mesta);

            // Проверяем условие с текущим заказом
            if (_currentOrder != null)
            {
                query = query.Where(z => z.ID_zakaza != _currentOrder.ID_zakaza);
            }

            // Проверяем условие с датами
            query = query.Where(z => z.Data_i_vremya_priema_avto != null &&
z.Data_i_vremya_vidachi_avto != null &&
z.Data_i_vremya_priema_avto < endTime &&
z.Data_i_vremya_vidachi_avto > startTime);

            var conflictingOrders = query.ToList(); // ← Здесь может вылететь ошибка

            if (conflictingOrders.Any())
            {
                MessageBox.Show($"Выбранное рабочее место уже занято в это время. Конфликт с заказом №{conflictingOrders.First().ID_zakaza}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка занятости сотрудника
            var conflictingEmployeeOrders = App.Context.dm_Zakazi
                .Where(z => z.Ispolnitel == _selectedEmployee.ID_user &&
                            z.ID_zakaza != currentOrderId &&
                            z.Data_i_vremya_priema_avto < endTime &&
                            z.Data_i_vremya_vidachi_avto > startTime)
                .ToList();

            if (conflictingEmployeeOrders.Any())
            {
                MessageBox.Show($"Выбранный сотрудник уже занят в это время. Конфликт с заказом №{conflictingEmployeeOrders.First().ID_zakaza}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка наличия хотя бы одной услуги
            if (_services.Count == 0)
            {
                MessageBox.Show("Необходимо добавить хотя бы одну услугу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields()) return;

            try
            {
                if (_isEditing)
                    UpdateOrder();
                else
                    AddNewOrder();

                MessageBox.Show("Заказ успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNewOrder()
        {
            var newOrder = new Entities.dm_Zakazi
            {
                Data_sozdaniya = DateTime.Now.Date,
                Status = BtnOrderStatus.Content.ToString(),
                Klient = _selectedClient.ID_user,
                Ispolnitel = _selectedEmployee.ID_user,
                Data_i_vremya_priema_avto = GetDateTimeFromButtonAndComboBoxes(BtnDatePriem, ComboHourPriem, ComboMinutePriem),
                Data_i_vremya_vidachi_avto = GetDateTimeFromButtonAndComboBoxes(BtnDateVidacha, ComboHourVidacha, ComboMinuteVidacha),
                Rabochee_mesto = _selectedWorkPlace.ID_rabochego_mesta,
                Avtomobil = _selectedAuto.ID_avtomobilya,
                Oplata = BtnPaymentStatus.Content.ToString() == "Оплачен"
            };

            App.Context.dm_Zakazi.Add(newOrder);
            App.Context.SaveChanges();

            // Сохраняем услуги
            foreach (var service in _services)
            {
                service.ID_zakaza = newOrder.ID_zakaza;
                App.Context.dm_Raboti_v_zakaze.Add(service);
            }

            // Сохраняем детали
            foreach (var detail in _details)
            {
                detail.ID_zakaza = newOrder.ID_zakaza;
                App.Context.dm_Detali_v_zakaze.Add(detail);
            }

            // Сохраняем фотографии
            foreach (var photo in _photos)
            {
                string savedPath = SavePhoto(photo.ImagePath, newOrder.ID_zakaza);
                App.Context.dm_Foto_v_zakaze.Add(new Entities.dm_Foto_v_zakaze
                {
                    ID_zakaza = newOrder.ID_zakaza,
                    Foto = savedPath
                });
            }

            App.Context.SaveChanges();
        }

        private void UpdateOrder()
        {
            _currentOrder.Klient = _selectedClient.ID_user;
            _currentOrder.Ispolnitel = _selectedEmployee.ID_user;
            _currentOrder.Data_i_vremya_priema_avto = GetDateTimeFromButtonAndComboBoxes(BtnDatePriem, ComboHourPriem, ComboMinutePriem);
            _currentOrder.Data_i_vremya_vidachi_avto = GetDateTimeFromButtonAndComboBoxes(BtnDateVidacha, ComboHourVidacha, ComboMinuteVidacha);
            _currentOrder.Rabochee_mesto = _selectedWorkPlace.ID_rabochego_mesta;
            _currentOrder.Avtomobil = _selectedAuto.ID_avtomobilya;

            _currentOrder.Status = BtnOrderStatus.Content.ToString();
            _currentOrder.Oplata = BtnPaymentStatus.Content.ToString() == "Оплачен";

            // Обновляем услуги
            var existingServices = App.Context.dm_Raboti_v_zakaze
                .Where(r => r.ID_zakaza == _currentOrder.ID_zakaza)
                .ToList();

            // Удаляем старые услуги
            foreach (var service in existingServices)
            {
                if (!_services.Any(s => s.ID_raboti == service.ID_raboti))
                {
                    App.Context.dm_Raboti_v_zakaze.Remove(service);
                }
            }

            // Добавляем новые услуги
            foreach (var service in _services)
            {
                if (!existingServices.Any(s => s.ID_raboti == service.ID_raboti))
                {
                    service.ID_zakaza = _currentOrder.ID_zakaza;
                    App.Context.dm_Raboti_v_zakaze.Add(service);
                }
            }

            // Обновляем детали
            var existingDetails = App.Context.dm_Detali_v_zakaze
                .Where(d => d.ID_zakaza == _currentOrder.ID_zakaza)
                .ToList();

            // Удаляем старые детали
            foreach (var detail in existingDetails)
            {
                if (!_details.Any(d => d.ID_detali == detail.ID_detali))
                {
                    App.Context.dm_Detali_v_zakaze.Remove(detail);
                }
            }

            // Добавляем новые детали
            foreach (var detail in _details)
            {
                if (!existingDetails.Any(d => d.ID_detali == detail.ID_detali))
                {
                    detail.ID_zakaza = _currentOrder.ID_zakaza;
                    App.Context.dm_Detali_v_zakaze.Add(detail);
                }
            }

            // Обновляем фотографии
            var existingPhotos = App.Context.dm_Foto_v_zakaze
                .Where(f => f.ID_zakaza == _currentOrder.ID_zakaza)
                .ToList();

            // Удаляем старые фотографии
            foreach (var photo in existingPhotos)
            {
                if (!_photos.Any(p => p.ImagePath == photo.Foto))
                {
                    App.Context.dm_Foto_v_zakaze.Remove(photo);
                }
            }

            // Добавляем новые фотографии
            foreach (var photo in _photos)
            {
                if (!existingPhotos.Any(p => p.Foto == photo.ImagePath))
                {
                    string savedPath = SavePhoto(photo.ImagePath, _currentOrder.ID_zakaza);
                    App.Context.dm_Foto_v_zakaze.Add(new Entities.dm_Foto_v_zakaze
                    {
                        ID_zakaza = _currentOrder.ID_zakaza,
                        Foto = savedPath
                    });
                }
            }

            App.Context.SaveChanges();
        }

        private DateTime GetDateTimeFromPicker(DatePicker datePicker, ComboBox timeCombo)
        {
            var timeParts = timeCombo.SelectedItem.ToString().Split(':');
            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);

            return datePicker.SelectedDate.Value
                .AddHours(hour)
                .AddMinutes(minute);
        }

        private string SavePhoto(string imagePath, int orderId)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            string photosFolder = GetPhotosFolderPath();
            string orderFolder = System.IO.Path.Combine(photosFolder, orderId.ToString());
            string fileName = System.IO.Path.GetFileName(imagePath);

            // Если файл уже находится в нужной папке заказа
            string destinationPath = System.IO.Path.Combine(orderFolder, fileName);
            if (imagePath.Equals(destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                return System.IO.Path.Combine(orderId.ToString(), fileName);
            }

            // Если файл уже существует в папке заказа, но пришел из другого места
            if (File.Exists(destinationPath))
            {
                // Генерируем уникальное имя
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string fileExt = System.IO.Path.GetExtension(fileName);
                fileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                destinationPath = System.IO.Path.Combine(orderFolder, fileName);
            }

            // Создаем папку, если ее нет
            if (!Directory.Exists(orderFolder))
            {
                Directory.CreateDirectory(orderFolder);
            }

            // Копируем только если файл еще не в нужной папке
            if (!File.Exists(destinationPath))
            {
                File.Copy(imagePath, destinationPath);
            }

            return System.IO.Path.Combine(orderId.ToString(), fileName);
        }

        private static string GetPhotosFolderPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "OrderPhotos");
        }

        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            // Если клиент фиксированный, не разрешаем добавлять нового
            if (_fixedClientId.HasValue)
            {
                MessageBox.Show("Нельзя добавить нового клиента при создании заказа из профиля клиента",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Сбрасываем выбранного клиента, если он был выбран
            if (_selectedClient != null)
            {
                _selectedClient = null;
                BtnClient.Content = "Выберите клиента";
                ClientContactPanel.Visibility = Visibility.Collapsed;
                AutoPanel.Visibility = Visibility.Collapsed;

                // Также сбрасываем автомобиль, так как он принадлежит клиенту
                _selectedAuto = null;
                BtnAuto.Content = "Выберите автомобиль";
                AutoInfoPanel.Visibility = Visibility.Collapsed;
            }

            NewClientPanel.Visibility = Visibility.Visible;
            BtnClient.IsEnabled = false;
            BtnClient.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fafafa"));
            BtnClient.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
            BtnClient.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
            BtnAddClient.IsEnabled = false;

            // Очищаем поля
            TextBoxNewClientFamiliya.Text = "";
            TextBoxNewClientImya.Text = "";
            TextBoxNewClientOtchestvo.Text = "";
            TextBoxNewClientPhone.Text = "";
            BtnNewClientDataRojdeniya.Content = "Выберите дату рождения";
            _newClientBirthDate = null;
        }

        private void BtnNewClientDataRojdeniya_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

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
                FontFamily = this.FontFamily,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                FontSize = 25
            };

            if (_newClientBirthDate.HasValue)
            {
                calendar.SelectedDate = _newClientBirthDate.Value;
                calendar.DisplayDate = _newClientBirthDate.Value;
            }

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    _newClientBirthDate = calendar.SelectedDate.Value;
                    BtnNewClientDataRojdeniya.Content = _newClientBirthDate.Value.ToString("dd.MM.yyyy");
                    ClosePopup();
                }
            };

            _currentPopup = new Border
            {
                Style = (Style)FindResource("PopupBlockStyle"),
                Child = calendar,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray
            };

            PopupCanvas.Children.Add(_currentPopup);
            Point buttonPos = button.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 5);
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void BtnCancelNewClient_Click(object sender, RoutedEventArgs e)
        {
            NewClientPanel.Visibility = Visibility.Collapsed;
            BtnClient.IsEnabled = true; // Возвращаем активное состояние
            BtnClient.Background = Brushes.White;
            BtnClient.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));
            BtnClient.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));
            BtnAddClient.IsEnabled = true;
        }

        private void BtnSaveNewClient_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(TextBoxNewClientFamiliya.Text) ||
                string.IsNullOrWhiteSpace(TextBoxNewClientImya.Text) ||
                _newClientBirthDate == null ||
                string.IsNullOrWhiteSpace(TextBoxNewClientPhone.Text))
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate phone number format
            if (!Regex.IsMatch(TextBoxNewClientPhone.Text, @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$"))
            {
                MessageBox.Show("Номер телефона должен быть в формате: +7 (xxx) xxx-xx-xx", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Создаем нового клиента
            var newClient = new Entities.dm_Users
            {
                Familiya = TextBoxNewClientFamiliya.Text,
                Imya = TextBoxNewClientImya.Text,
                Otchestvo = string.IsNullOrWhiteSpace(TextBoxNewClientOtchestvo.Text) ? null : TextBoxNewClientOtchestvo.Text,
                Data_rojdeniya = _newClientBirthDate.Value,
                Nomer_telefona = TextBoxNewClientPhone.Text,
                Rol = 3 // Роль "Клиент"
            };

            try
            {
                App.Context.dm_Users.Add(newClient);
                App.Context.SaveChanges();

                // Устанавливаем нового клиента
                _selectedClient = newClient;
                BtnClient.Content = $"{newClient.Familiya} {newClient.Imya} {newClient.Otchestvo}";
                ClientContactInfo.Text = $"Тел: {newClient.Nomer_telefona}";
                ClientContactPanel.Visibility = Visibility.Visible;
                AutoPanel.Visibility = Visibility.Visible;

                // Сбрасываем автомобиль, так как он мог быть привязан к старому клиенту
                _selectedAuto = null;
                BtnAuto.Content = "Выберите автомобиль";
                AutoInfoPanel.Visibility = Visibility.Collapsed;

                // Скрываем панель добавления
                NewClientPanel.Visibility = Visibility.Collapsed;
                BtnClient.IsEnabled = true;
                BtnAddClient.IsEnabled = true;
                BtnClient.Background = Brushes.White;
                BtnClient.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));
                BtnClient.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики для добавления автомобиля
        private void BtnAddAuto_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null)
            {
                MessageBox.Show("Сначала выберите клиента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Сбрасываем выбранный автомобиль, если он был выбран
            if (_selectedAuto != null)
            {
                _selectedAuto = null;
                BtnAuto.Content = "Выберите автомобиль";
                AutoInfoPanel.Visibility = Visibility.Collapsed;
            }

            NewAutoPanel.Visibility = Visibility.Visible;
            BtnAuto.IsEnabled = false;
            BtnAuto.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fafafa"));
            BtnAuto.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
            BtnAuto.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
            BtnAddAuto.IsEnabled = false;

            // Очищаем поля
            BtnNewAutoMarka.Content = "Выберите марку";
            BtnNewAutoModel.Content = "Выберите модель";
            BtnNewAutoKomplektaciya.Content = "Выберите комплектацию";
            NewAutoKomplektaciyaInfo.Text = "";
            TextBoxNewAutoVin.Text = "";
            TextBoxNewAutoGosNumber.Text = "";

            _newAutoSelectedMarka = null;
            _newAutoSelectedModel = null;
            _newAutoSelectedKomplektaciya = null;

            NewAutoModelPanel.Visibility = Visibility.Collapsed;
            NewAutoKomplektaciyaPanel.Visibility = Visibility.Collapsed;
            NewAutoKomplektaciyaInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnNewAutoMarka_Click(object sender, RoutedEventArgs e)
        {
            var marki = App.Context.dm_Marki_avto
                .OrderBy(m => m.Nazvanie_marki)
                .ToList();

            var popupContent = new StackPanel();

            foreach (var marka in marki)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = marka.Nazvanie_marki,
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = marka,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += (s, args) =>
                {
                    _newAutoSelectedMarka = (Entities.dm_Marki_avto)((Button)s).Tag;
                    _newAutoSelectedModel = null;
                    _newAutoSelectedKomplektaciya = null;
                    BtnNewAutoMarka.Content = _newAutoSelectedMarka.Nazvanie_marki;
                    BtnNewAutoModel.Content = "Выберите модель";
                    BtnNewAutoKomplektaciya.Content = "Выберите комплектацию";
                    NewAutoKomplektaciyaInfo.Text = "";

                    NewAutoModelPanel.Visibility = Visibility.Visible;
                    NewAutoKomplektaciyaPanel.Visibility = Visibility.Collapsed;
                    NewAutoKomplektaciyaInfoPanel.Visibility = Visibility.Collapsed;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnNewAutoMarka, popupContent);
        }

        private void BtnNewAutoModel_Click(object sender, RoutedEventArgs e)
        {
            if (_newAutoSelectedMarka == null) return;

            var modeli = App.Context.dm_Modeli_avto
                .Where(m => m.Marka == _newAutoSelectedMarka.ID_marki)
                .OrderBy(m => m.Model)
                .ToList();

            var popupContent = new StackPanel();

            foreach (var model in modeli)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = $"{model.Model} ({model.God_vipuska}" +
                             (model.God_okonchaniya_vipuska.HasValue ?
                              $" - {model.God_okonchaniya_vipuska})" : " - н.в.)"),
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = model,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += (s, args) =>
                {
                    _newAutoSelectedModel = (Entities.dm_Modeli_avto)((Button)s).Tag;
                    _newAutoSelectedKomplektaciya = null;
                    BtnNewAutoModel.Content = $"{_newAutoSelectedModel.Model}";
                    BtnNewAutoKomplektaciya.Content = "Выберите комплектацию";
                    NewAutoKomplektaciyaInfo.Text = "";

                    NewAutoKomplektaciyaPanel.Visibility = Visibility.Visible;
                    NewAutoKomplektaciyaInfoPanel.Visibility = Visibility.Collapsed;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnNewAutoModel, popupContent);
        }

        private void BtnNewAutoKomplektaciya_Click(object sender, RoutedEventArgs e)
        {
            if (_newAutoSelectedModel == null) return;

            var komplektacii = App.Context.dm_Komplektacii_avto
                .Include("dm_Tipi_dvigatelya")
                .Include("dm_Tipi_korobki_peredach")
                .Include("dm_Tipi_kuzova")
                .Include("dm_Tipi_privoda")
                .Where(k => k.Model_avto == _newAutoSelectedModel.ID_modeli_avto)
                .OrderBy(k => k.Moshnost)
                .ToList();

            var popupContent = new StackPanel();

            foreach (var komplekt in komplektacii)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = $"{komplekt.dm_Tipi_dvigatelya.Tip_dvigatelya}, " +
                              $"{komplekt.Moshnost} л.с., " +
                              $"{komplekt.dm_Tipi_korobki_peredach.Tip_korobki_peredach}",
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = komplekt,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    ToolTip = $"{komplekt.dm_Tipi_kuzova.Tip_kuzova}, {komplekt.dm_Tipi_privoda.Tip_privoda}"
                };
                btn.Click += (s, args) =>
                {
                    _newAutoSelectedKomplektaciya = (Entities.dm_Komplektacii_avto)((Button)s).Tag;
                    BtnNewAutoKomplektaciya.Content = $"{_newAutoSelectedKomplektaciya.dm_Tipi_dvigatelya.Tip_dvigatelya}, {_newAutoSelectedKomplektaciya.Moshnost} л.с.";
                    NewAutoKomplektaciyaInfo.Text = $"{_newAutoSelectedKomplektaciya.dm_Tipi_kuzova.Tip_kuzova}, " +
                                                 $"{_newAutoSelectedKomplektaciya.dm_Tipi_korobki_peredach.Tip_korobki_peredach}, " +
                                                 $"{_newAutoSelectedKomplektaciya.dm_Tipi_privoda.Tip_privoda}";
                    NewAutoKomplektaciyaInfoPanel.Visibility = Visibility.Visible;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnNewAutoKomplektaciya, popupContent);
        }

        private void BtnCancelNewAuto_Click(object sender, RoutedEventArgs e)
        {
            NewAutoPanel.Visibility = Visibility.Collapsed;
            BtnAuto.IsEnabled = true; // Возвращаем активное состояние
            BtnAuto.Background = Brushes.White;
            BtnAuto.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));
            BtnAuto.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));
            BtnAddAuto.IsEnabled = true;
        }

        private void BtnSaveNewAuto_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (_newAutoSelectedKomplektaciya == null)
            {
                MessageBox.Show("Необходимо выбрать комплектацию автомобиля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка VIN-номера
            string vin = TextBoxNewAutoVin.Text.Trim().ToUpper();
            if (!string.IsNullOrEmpty(vin))
            {
                if (vin.Length != 17)
                {
                    MessageBox.Show("VIN-номер должен содержать ровно 17 символов", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверка уникальности VIN-номера
                var existingVin = App.Context.dm_Avtomobili
                    .FirstOrDefault(a => a.WIN_nomer == vin);

                if (existingVin != null)
                {
                    MessageBox.Show($"Автомобиль с таким VIN-номером уже существует (гос. номер: {existingVin.Gos_nomer ?? "не указан"})",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    TextBoxNewAutoVin.Focus();
                    return;
                }
            }

            // Проверка уникальности госномера
            string gosNomer = TextBoxNewAutoGosNumber.Text.Trim().ToUpper();
            if (!string.IsNullOrEmpty(gosNomer))
            {
                var existingGosNomer = App.Context.dm_Avtomobili
                    .FirstOrDefault(a => a.Gos_nomer == gosNomer);

                if (existingGosNomer != null)
                {
                    MessageBox.Show($"Автомобиль с таким гос. номером уже существует (VIN: {existingGosNomer.WIN_nomer ?? "не указан"})",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    TextBoxNewAutoGosNumber.Focus();
                    return;
                }
            }

            // Создаем новый автомобиль
            var newAuto = new Entities.dm_Avtomobili
            {
                Model = _newAutoSelectedKomplektaciya.ID_komplektacii_avto,
                WIN_nomer = string.IsNullOrWhiteSpace(vin) ? null : vin,
                Vladelec = _selectedClient.ID_user,
                Gos_nomer = string.IsNullOrWhiteSpace(TextBoxNewAutoGosNumber.Text) ? null : TextBoxNewAutoGosNumber.Text
            };

            try
            {
                App.Context.dm_Avtomobili.Add(newAuto);
                App.Context.SaveChanges();

                // Устанавливаем новый автомобиль
                _selectedAuto = newAuto;
                BtnAuto.Content = $"{_newAutoSelectedKomplektaciya.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki} " +
                                 $"{_newAutoSelectedKomplektaciya.dm_Modeli_avto.Model}";
                AutoInfo.Text = $"VIN: {newAuto.WIN_nomer ?? "не указан"}, " +
                               $"Гос. номер: {newAuto.Gos_nomer ?? "не указан"}";
                AutoInfoPanel.Visibility = Visibility.Visible;

                // Скрываем панель добавления
                NewAutoPanel.Visibility = Visibility.Collapsed;
                BtnAuto.IsEnabled = true;
                BtnAddAuto.IsEnabled = true;
                BtnAuto.Background = Brushes.White;
                BtnAuto.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));
                BtnAuto.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF616161"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении автомобиля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (_currentPopup != null && _currentButton == button)
            {
                ClosePopup();
                return;
            }

            ClosePopup();
            _currentButton = button;

            var popupContent = new StackPanel();
            string[] statuses = button == BtnOrderStatus ? _orderStatuses : _paymentStatuses;

            foreach (var status in statuses)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = status,
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = status,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Width = button.ActualWidth
                };
                btn.Click += (s, args) =>
                {
                    button.Content = ((Button)s).Tag.ToString();
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(button, popupContent);
        }

        public class PhotoItem
        {
            public string ImagePath { get; set; } // Хранит относительный путь (ID_заказа/файл.jpg)

            public BitmapImage Image
            {
                get
                {
                    try
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;

                        string fullPath = System.IO.Path.Combine(GetPhotosFolderPath(), ImagePath);
                        image.UriSource = new Uri(fullPath);

                        image.EndInit();
                        return image;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        private void BtnDefects_Click(object sender, RoutedEventArgs e)
        {
            // Если создаём новый заказ (пока _isEditing == false), запрещаем переход
            if (!_isEditing || _currentOrder == null)
            {
                MessageBox.Show("Сначала сохраните заказ, затем переходите к заполнению дефектов.",
                                "Сохраните заказ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Если же редактируется уже существующий заказ:
            var windowDefects = new AddEdit_Defekti(_currentOrder.ID_zakaza);
            windowDefects.Owner = this; // чтобы окно было “дочерним”
            windowDefects.ShowDialog();
        }
    }


}

