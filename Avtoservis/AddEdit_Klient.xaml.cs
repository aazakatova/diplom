using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для AddEdit_Klient.xaml
    /// </summary>
    public partial class AddEdit_Klient : Window
    {
        private Entities.dm_Users _currentKlient;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;
        private DateTime? _selectedDate;

        public AddEdit_Klient(Entities.dm_Users klient)
        {
            InitializeComponent();
            _currentKlient = klient;
            _isEditing = true;
            FillForm(_currentKlient);
            TextBlockZagolovok.Text = $"Редактирование клиента {klient.ID_user}";
        }

        public AddEdit_Klient()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление клиента";
        }

        private void BtnData_rojdeniya_Click(object sender, RoutedEventArgs e)
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
            if (!ValidateFields()) return; // Проверка корректности всех полей формы. Если есть ошибки — выход из метода.

            if (_isEditing) // В зависимости от режима (добавление или редактирование) вызывается нужный метод
                Update(); // Редактирование существующего клиента
            else
                AddNew(); // Добавление нового клиента

            // Уведомление об успешном сохранении
            MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true; // Устанавливаем результат диалога и закрываем окно
            this.Close();
        }

        private void AddNew()
        {
            var newKlient = new Entities.dm_Users // Создание нового объекта клиента с данными из формы
            {
                Familiya = TextBoxFamiliya.Text,
                Imya = TextBoxImya.Text,
                Otchestvo = string.IsNullOrWhiteSpace(TextBoxOtchestvo.Text) ? null : TextBoxOtchestvo.Text,
                Data_rojdeniya = _selectedDate.Value, // Выбранная дата рождения
                Nomer_telefona = TextBoxNomer_telefona.Text,
                Rol = 3 // Присваиваем роль "Клиент"
            };

            App.Context.dm_Users.Add(newKlient); // Добавление нового клиента в базу данных и сохранение изменений
            App.Context.SaveChanges();
        }

        private void Update()
        {
            // Обновляем поля текущего клиента на значения из формы
            _currentKlient.Familiya = TextBoxFamiliya.Text;
            _currentKlient.Imya = TextBoxImya.Text;
            _currentKlient.Otchestvo = string.IsNullOrWhiteSpace(TextBoxOtchestvo.Text) ? null : TextBoxOtchestvo.Text;
            _currentKlient.Data_rojdeniya = _selectedDate.Value;
            _currentKlient.Nomer_telefona = TextBoxNomer_telefona.Text;

            App.Context.SaveChanges();   // Сохраняем изменения в базу данных
        }

        private void FillForm(Entities.dm_Users klient)
        {
            TextBoxFamiliya.Text = klient.Familiya;
            TextBoxImya.Text = klient.Imya;
            TextBoxOtchestvo.Text = klient.Otchestvo;
            TextBoxNomer_telefona.Text = klient.Nomer_telefona;

            _selectedDate = klient.Data_rojdeniya;
            BtnData_rojdeniya.Content = _selectedDate?.ToString("dd.MM.yyyy") ?? "Выберите дату рождения";
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TextBoxFamiliya.Text) ||
                string.IsNullOrWhiteSpace(TextBoxImya.Text) ||
                _selectedDate == null ||
                string.IsNullOrWhiteSpace(TextBoxNomer_telefona.Text))
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

            if (!Regex.IsMatch(TextBoxNomer_telefona.Text, @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$"))
            {
                MessageBox.Show("Номер телефона должен быть в формате: +7 (xxx) xxx-xx-xx", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }


    }
}
