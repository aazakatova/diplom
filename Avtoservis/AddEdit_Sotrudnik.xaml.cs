using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для AddEdit_Sotrudnik.xaml
    /// </summary>
    public partial class AddEdit_Sotrudnik : Window
    {
        private Entities.dm_Users _currentSotrudnik;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;
        private string _selectedImagePath;
        private string _originalPhotoPath;
        private string _selectedStatus;
        private DateTime? _selectedDate;

        public AddEdit_Sotrudnik(Entities.dm_Users sotrudnik)
        {
            InitializeComponent();
            _currentSotrudnik = sotrudnik;
            _isEditing = true;
            _originalPhotoPath = sotrudnik.Foto;
            FillForm(_currentSotrudnik);
            TextBlockZagolovok.Text = $"Редактирование сотрудника {sotrudnik.ID_user}";
        }

        public AddEdit_Sotrudnik()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление сотрудника";
        }

        private bool IsLoginUnique(string login, int? currentUserId = null)
        {
            // Проверяем, существует ли пользователь с таким логином (кроме текущего)
            return !App.Context.dm_Users.Any(u =>
                u.Login == login &&
                (currentUserId == null || u.ID_user != currentUserId));
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            ShowSelectionPopup(
                    (Button)sender,
                    new[] { "Работает", "В отпуске" }, // Только русские варианты
                    item => {
                        _selectedStatus = item; // Сохраняем русский вариант
                        BtnStatus.Content = item;
                    }
                );
        }

        private void BtnData_rojdeniya_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            // Если попап уже открыт - закрываем его
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
                Height = 150,
                FontSize = 25
            };

            if (_selectedDate.HasValue)
            {
                calendar.SelectedDate = _selectedDate.Value;
                calendar.DisplayDate = _selectedDate.Value;
            }

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    _selectedDate = calendar.SelectedDate.Value;
                    BtnData_rojdeniya.Content = _selectedDate.Value.ToString("dd.MM.yyyy");
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

        private void ShowSelectionPopup(Button button, string[] items, Action<string> onSelected)
        {
            if (_currentPopup != null && _currentButton == button)
            {
                ClosePopup();
                return;
            }

            ClosePopup();
            _currentButton = button;

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 200
            };

            var stackPanel = new StackPanel();
            scrollViewer.Content = stackPanel;

            foreach (var item in items)
            {
                var btn = new Button
                {
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Content = new TextBlock { Text = item, Margin = new Thickness(10, 0, 0, 0) },
                    MinWidth = button.ActualWidth - 2,
                    MinHeight = 40,
                    FontSize = 15,
                    Tag = item
                };

                btn.Click += (s, args) =>
                {
                    onSelected((string)((Button)s).Tag);
                    ClosePopup();
                };

                stackPanel.Children.Add(btn);
            }

            _currentPopup = new Border
            {
                Style = (Style)FindResource("PopupBlockStyle"),
                Child = scrollViewer
            };

            PopupCanvas.Children.Add(_currentPopup);
            Point buttonPos = button.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(_currentPopup, buttonPos.X);
            Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 5);
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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields()) return;

            if (_isEditing)
                Update();
            else
                AddNew();

            MessageBox.Show("Данные успешно сохранены!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void AddNew()
        {
            var newSotrudnik = new Entities.dm_Users
            {
                Familiya = TextBoxFamiliya.Text,
                Imya = TextBoxImya.Text,
                Otchestvo = string.IsNullOrWhiteSpace(TextBoxOtchestvo.Text) ? null : TextBoxOtchestvo.Text,
                Data_rojdeniya = _selectedDate.Value,
                Status = _selectedStatus,
                Nomer_telefona = string.IsNullOrWhiteSpace(TextBoxNomer_telefona.Text) ? null : TextBoxNomer_telefona.Text,
                Login = TextBoxLogin.Text,
                Password = TextBoxPassword.Text,
                Stazh = string.IsNullOrWhiteSpace(TextBoxStazh.Text) ? (int?)null : int.Parse(TextBoxStazh.Text),
                Foto = SaveImage(_selectedImagePath),
                Rol = 2 // Роль "Сотрудник"
            };

            App.Context.dm_Users.Add(newSotrudnik);
            App.Context.SaveChanges();
        }

        private void Update()
        {
            _currentSotrudnik.Familiya = TextBoxFamiliya.Text;
            _currentSotrudnik.Imya = TextBoxImya.Text;
            _currentSotrudnik.Otchestvo = string.IsNullOrWhiteSpace(TextBoxOtchestvo.Text) ? null : TextBoxOtchestvo.Text;
            _currentSotrudnik.Data_rojdeniya = _selectedDate.Value;
            _currentSotrudnik.Status = _selectedStatus;
            _currentSotrudnik.Nomer_telefona = string.IsNullOrWhiteSpace(TextBoxNomer_telefona.Text) ? null : TextBoxNomer_telefona.Text;
            _currentSotrudnik.Login = TextBoxLogin.Text;
            _currentSotrudnik.Password = TextBoxPassword.Text;
            _currentSotrudnik.Stazh = string.IsNullOrWhiteSpace(TextBoxStazh.Text) ? (int?)null : int.Parse(TextBoxStazh.Text);

            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                _currentSotrudnik.Foto = SaveImage(_selectedImagePath);
            }

            App.Context.SaveChanges();
        }

        private string SaveImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            string sotrudnikiFolder = GetSotrudnikiFolderPath();
            bool isAlreadyInSotrudnikiFolder = imagePath.StartsWith(sotrudnikiFolder, StringComparison.OrdinalIgnoreCase);

            if (isAlreadyInSotrudnikiFolder)
                return System.IO.Path.GetFileName(imagePath);

            if (!Directory.Exists(sotrudnikiFolder))
                Directory.CreateDirectory(sotrudnikiFolder);

            string fileName = System.IO.Path.GetFileName(imagePath);
            string newPhotoPath = System.IO.Path.Combine(sotrudnikiFolder, fileName);

            if (File.Exists(newPhotoPath))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                string fileExt = System.IO.Path.GetExtension(fileName);
                fileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                newPhotoPath = System.IO.Path.Combine(sotrudnikiFolder, fileName);
            }

            File.Copy(imagePath, newPhotoPath);
            return fileName;
        }

        private string GetSotrudnikiFolderPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "Users");
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = System.IO.Path.Combine(GetSotrudnikiFolderPath(), imageName);

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

        private void FillForm(Entities.dm_Users sotrudnik)
        {
            TextBoxFamiliya.Text = sotrudnik.Familiya;
            TextBoxImya.Text = sotrudnik.Imya;
            TextBoxOtchestvo.Text = sotrudnik.Otchestvo;
            TextBoxNomer_telefona.Text = sotrudnik.Nomer_telefona;
            TextBoxLogin.Text = sotrudnik.Login;
            TextBoxPassword.Text = sotrudnik.Password;
            TextBoxStazh.Text = sotrudnik.Stazh?.ToString();

            _selectedDate = sotrudnik.Data_rojdeniya;
            BtnData_rojdeniya.Content = _selectedDate?.ToString("dd.MM.yyyy") ?? "Выберите дату рождения";

            // Просто используем сохраненное значение статуса (уже на русском)
            _selectedStatus = sotrudnik.Status;
            BtnStatus.Content = _selectedStatus;

            if (!string.IsNullOrEmpty(sotrudnik.Foto))
            {
                ImageSotrudnik.Source = LoadImage(sotrudnik.Foto);
                ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImageSotrudnik.Source = null;
                ImagePlaceholderBorder.Visibility = Visibility.Visible;
            }
        }

        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _selectedImagePath = openFileDialog.FileName;
                    ImageSotrudnik.Source = new BitmapImage(new Uri(_selectedImagePath));
                    ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
                }
            }
            else
            {
                _selectedImagePath = null;
                if (string.IsNullOrEmpty(_originalPhotoPath))
                {
                    ImageSotrudnik.Source = null;
                    ImagePlaceholderBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    ImageSotrudnik.Source = LoadImage(_originalPhotoPath);
                    ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TextBoxFamiliya.Text) ||
                string.IsNullOrWhiteSpace(TextBoxImya.Text) ||
                _selectedDate == null ||
                string.IsNullOrWhiteSpace(_selectedStatus) ||
                string.IsNullOrWhiteSpace(TextBoxLogin.Text) ||
                string.IsNullOrWhiteSpace(TextBoxPassword.Text))
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (TextBoxFamiliya.Text.Length > 100 ||
                TextBoxImya.Text.Length > 100 ||
                (TextBoxOtchestvo.Text != null && TextBoxOtchestvo.Text.Length > 100))
            {
                MessageBox.Show("ФИО не должно превышать 100 символов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TextBoxNomer_telefona.Text) &&
                !Regex.IsMatch(TextBoxNomer_telefona.Text, @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$"))
            {
                MessageBox.Show("Номер телефона должен быть в формате: +7 (xxx) xxx-xx-xx", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TextBoxStazh.Text) &&
                (!int.TryParse(TextBoxStazh.Text, out int stazh) || stazh < 0))
            {
                MessageBox.Show("Введите корректный стаж (целое неотрицательное число).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка уникальности логина
            if (!IsLoginUnique(TextBoxLogin.Text, _isEditing ? _currentSotrudnik.ID_user : (int?)null))
            {
                MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
