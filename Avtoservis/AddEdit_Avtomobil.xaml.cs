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
    /// Логика взаимодействия для AddEdit_Avtomobil.xaml
    /// </summary>
    public partial class AddEdit_Avtomobil : Window
    {
        private Entities.dm_Avtomobili _currentAuto;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;

        // Выбранные значения
        private Entities.dm_Users _selectedClient;
        private Entities.dm_Marki_avto _selectedMarka;
        private Entities.dm_Modeli_avto _selectedModel;
        private Entities.dm_Komplektacii_avto _selectedKomplektaciya;

        private int? _fixedClientId;

        public AddEdit_Avtomobil(Entities.dm_Avtomobili auto, int? fixedClientId = null)
    : this(fixedClientId)
        {
            _currentAuto = auto;
            _isEditing = true;
            FillForm(_currentAuto);
            TextBlockZagolovok.Text = $"Редактирование автомобиля {auto.Gos_nomer ?? "без номера"}";
        }

        public AddEdit_Avtomobil(int? fixedClientId = null)
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление автомобиля";

            // Если передан ID клиента, загружаем его данные
            if (fixedClientId.HasValue)
            {
                _selectedClient = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == fixedClientId.Value);
                if (_selectedClient != null)
                {
                    // Устанавливаем текст кнопки
                    BtnVladelec.Content = $"{_selectedClient.Familiya} {_selectedClient.Imya} {_selectedClient.Otchestvo}";
                    // Блокируем выбор
                    BtnVladelec.IsEnabled = false;
                    BtnVladelec.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fafafa"));
                    BtnVladelec.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));
                    BtnVladelec.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCACACA"));

                    // Показываем контактную информацию
                    ClientContactInfo.Text = $"Тел: {_selectedClient.Nomer_telefona}";
                    ClientContactPanel.Visibility = Visibility.Visible;
                }
            }
        }

        public AddEdit_Avtomobil()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление автомобиля";
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (button == BtnVladelec && _fixedClientId.HasValue)
            {
                MessageBox.Show("Нельзя изменить владельца при создании автомобиля из профиля клиента",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_currentPopup != null && _currentButton == button)
            {
                ClosePopup();
                return;
            }

            if (button == BtnVladelec)
            {
                ShowClientSelectionPopup();
            }
            else if (button == BtnMarka)
            {
                ShowMarkaSelectionPopup();
            }
            else if (button == BtnModel && _selectedMarka != null)
            {
                ShowModelSelectionPopup();
            }
            else if (button == BtnKomplektaciya && _selectedModel != null)
            {
                ShowKomplektaciyaSelectionPopup();
            }
        }

        private void ShowClientSelectionPopup()
        {
            var clients = App.Context.dm_Users
                .Where(u => u.dm_Roli.Rol == "Клиент")
                .OrderBy(u => u.Familiya)
                .ThenBy(u => u.Imya)
                .ToList();

            var popupContent = new StackPanel();

            foreach (var client in clients)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = $"{client.Familiya} {client.Imya} {client.Otchestvo}",
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = client,
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += (s, e) =>
                {
                    _selectedClient = (Entities.dm_Users)((Button)s).Tag;
                    BtnVladelec.Content = $"{_selectedClient.Familiya} {_selectedClient.Imya} {_selectedClient.Otchestvo}";
                    ClientContactInfo.Text = $"Тел: {_selectedClient.Nomer_telefona}";
                    ClientContactPanel.Visibility = Visibility.Visible;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnVladelec, popupContent);
        }

        private void ShowMarkaSelectionPopup()
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
                btn.Click += (s, e) =>
                {
                    _selectedMarka = (Entities.dm_Marki_avto)((Button)s).Tag;
                    _selectedModel = null;
                    _selectedKomplektaciya = null;
                    BtnMarka.Content = _selectedMarka.Nazvanie_marki;
                    BtnModel.Content = "Выберите модель";
                    BtnKomplektaciya.Content = "Выберите комплектацию";
                    KomplektaciyaInfo.Text = "";

                    ModelPanel.Visibility = Visibility.Visible;
                    KomplektaciyaPanel.Visibility = Visibility.Collapsed;
                    KomplektaciyaInfoPanel.Visibility = Visibility.Collapsed;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnMarka, popupContent);
        }

        private void ShowModelSelectionPopup()
        {
            var modeli = App.Context.dm_Modeli_avto
                .Where(m => m.Marka == _selectedMarka.ID_marki)
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
                btn.Click += (s, e) =>
                {
                    _selectedModel = (Entities.dm_Modeli_avto)((Button)s).Tag;
                    _selectedKomplektaciya = null;
                    BtnModel.Content = $"{_selectedModel.Model}";
                    BtnKomplektaciya.Content = "Выберите комплектацию";
                    KomplektaciyaInfo.Text = "";

                    KomplektaciyaPanel.Visibility = Visibility.Visible;
                    KomplektaciyaInfoPanel.Visibility = Visibility.Collapsed;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnModel, popupContent);
        }

        private void ShowKomplektaciyaSelectionPopup()
        {
            var komplektacii = App.Context.dm_Komplektacii_avto
                .Where(k => k.Model_avto == _selectedModel.ID_modeli_avto)
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
                btn.Click += (s, e) =>
                {
                    _selectedKomplektaciya = (Entities.dm_Komplektacii_avto)((Button)s).Tag;
                    BtnKomplektaciya.Content = $"{_selectedKomplektaciya.dm_Tipi_dvigatelya.Tip_dvigatelya}, {_selectedKomplektaciya.Moshnost} л.с.";
                    KomplektaciyaInfo.Text = $"{_selectedKomplektaciya.dm_Tipi_kuzova.Tip_kuzova}, " +
                                             $"{_selectedKomplektaciya.dm_Tipi_korobki_peredach.Tip_korobki_peredach}, " +
                                             $"{_selectedKomplektaciya.dm_Tipi_privoda.Tip_privoda}";
                    KomplektaciyaInfoPanel.Visibility = Visibility.Visible;
                    ClosePopup();
                };
                popupContent.Children.Add(btn);
            }

            ShowPopup(BtnKomplektaciya, popupContent);
        }

        private void ShowPopup(Button button, StackPanel content)
        {
            ClosePopup();
            _currentButton = button;

            var scrollViewer = new ScrollViewer
            {
                Content = content,
                MaxHeight = 200, // Ограничение высоты
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

        private void FillForm(Entities.dm_Avtomobili auto)
        {
            TextBoxWIN_nomer.Text = auto.WIN_nomer?.ToString() ?? "";
            TextBoxGos_nomer.Text = auto.Gos_nomer;

            // Заполняем клиента
            if (auto.Vladelec != 0)
            {
                var client = App.Context.dm_Users.Find(auto.Vladelec);
                if (client != null)
                {
                    _selectedClient = client;
                    BtnVladelec.Content = $"{client.Familiya} {client.Imya} {client.Otchestvo}";
                    ClientContactInfo.Text = $"Тел: {client.Nomer_telefona}";
                    ClientContactPanel.Visibility = Visibility.Visible;
                }
            }

            // Заполняем комплектацию
            if (auto.Model != 0)
            {
                var komplekt = App.Context.dm_Komplektacii_avto
                    .Include("dm_Modeli_avto")
                    .Include("dm_Modeli_avto.dm_Marki_avto")
                    .FirstOrDefault(k => k.ID_komplektacii_avto == auto.Model);

                if (komplekt != null)
                {
                    _selectedKomplektaciya = komplekt;
                    _selectedModel = komplekt.dm_Modeli_avto;
                    _selectedMarka = komplekt.dm_Modeli_avto.dm_Marki_avto;

                    BtnMarka.Content = _selectedMarka.Nazvanie_marki;
                    BtnModel.Content = _selectedModel.Model;
                    BtnKomplektaciya.Content = $"{_selectedKomplektaciya.dm_Tipi_dvigatelya.Tip_dvigatelya}, {_selectedKomplektaciya.Moshnost} л.с.";
                    KomplektaciyaInfo.Text = $"{_selectedKomplektaciya.dm_Tipi_kuzova.Tip_kuzova}, " +
                                             $"{_selectedKomplektaciya.dm_Tipi_korobki_peredach.Tip_korobki_peredach}, " +
                                             $"{_selectedKomplektaciya.dm_Tipi_privoda.Tip_privoda}";

                    ModelPanel.Visibility = Visibility.Visible;
                    KomplektaciyaPanel.Visibility = Visibility.Visible;
                    KomplektaciyaInfoPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields()) return;

            // Проверка уникальности VIN-номера
            string vin = TextBoxWIN_nomer.Text.Trim().ToUpper();
            if (!string.IsNullOrEmpty(vin))
            {
                var existingVin = App.Context.dm_Avtomobili
                    .FirstOrDefault(a => a.WIN_nomer == vin && (_isEditing ? a.ID_avtomobilya != _currentAuto.ID_avtomobilya : true));

                if (existingVin != null)
                {
                    MessageBox.Show($"Автомобиль с таким VIN-номером уже существует (гос. номер: {existingVin.Gos_nomer ?? "не указан"})",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    TextBoxWIN_nomer.Focus();
                    return;
                }
            }

            // Проверка уникальности госномера
            string gosNomer = TextBoxGos_nomer.Text.Trim().ToUpper();
            if (!string.IsNullOrEmpty(gosNomer))
            {
                var existingGosNomer = App.Context.dm_Avtomobili
                    .FirstOrDefault(a => a.Gos_nomer == gosNomer && (_isEditing ? a.ID_avtomobilya != _currentAuto.ID_avtomobilya : true));

                if (existingGosNomer != null)
                {
                    MessageBox.Show($"Автомобиль с таким гос. номером уже существует (VIN: {existingGosNomer.WIN_nomer ?? "не указан"})",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    TextBoxGos_nomer.Focus();
                    return;
                }
            }

            if (_isEditing)
                UpdateAuto();
            else
                AddNewAuto();

            MessageBox.Show("Данные автомобиля успешно сохранены!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void AddNewAuto()
        {
            var newAuto = new Entities.dm_Avtomobili
            {
                Model = _selectedKomplektaciya.ID_komplektacii_avto,
                WIN_nomer = string.IsNullOrEmpty(TextBoxWIN_nomer.Text) ? null : TextBoxWIN_nomer.Text,
                Vladelec = _selectedClient.ID_user,
                Gos_nomer = string.IsNullOrEmpty(TextBoxGos_nomer.Text) ? null : TextBoxGos_nomer.Text
            };

            App.Context.dm_Avtomobili.Add(newAuto);
            App.Context.SaveChanges();
        }

        private void UpdateAuto()
        {
            _currentAuto.Model = _selectedKomplektaciya.ID_komplektacii_avto;
            _currentAuto.WIN_nomer = string.IsNullOrEmpty(TextBoxWIN_nomer.Text) ? null : TextBoxWIN_nomer.Text;
            _currentAuto.Vladelec = _selectedClient.ID_user;
            _currentAuto.Gos_nomer = string.IsNullOrEmpty(TextBoxGos_nomer.Text) ? null : TextBoxGos_nomer.Text;

            App.Context.SaveChanges();
        }

        private bool IsValidVin(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin))
                return true; // Разрешаем пустое значение (так как поле необязательное)

            if (vin.Length != 17)
                return false;

            // Разрешаем только английские буквы (кроме I, O, Q) и цифры
            foreach (char c in vin.ToUpper())
            {
                if (!((c >= 'A' && c <= 'H') ||
                      (c >= 'J' && c <= 'N') ||
                      (c >= 'P' && c <= 'R') ||
                      (c >= 'S' && c <= 'Z') ||
                      (c >= '0' && c <= '9')))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateFields()
        {
            if (_selectedClient == null)
            {
                MessageBox.Show("Необходимо выбрать владельца автомобиля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (_selectedKomplektaciya == null)
            {
                MessageBox.Show("Необходимо выбрать комплектацию автомобиля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка VIN-номера
            string vin = TextBoxWIN_nomer.Text.Trim();
            if (!string.IsNullOrEmpty(vin))
    {
                if (!IsValidVin(vin))
                {
                    MessageBox.Show("VIN-номер должен содержать ровно 17 символов (английские буквы и цифры, кроме I, O, Q)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    TextBoxWIN_nomer.Focus();
                    return false;
                }
            }

            return true;
        }
    }
}
