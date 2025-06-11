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
    /// Логика взаимодействия для AddEdit_Raboti.xaml
    /// </summary>
    public partial class AddEdit_Raboti : Window
    {
        private Entities.dm_Raboti _currentWork;
        private bool _isEditing;

        // Конструктор для редактирования существующей работы
        public AddEdit_Raboti(Entities.dm_Raboti work) : this()
        {
            _currentWork = work;
            _isEditing = true;
            FillForm(work);
            TextBlockZagolovok.Text = $"Редактирование работы №{work.ID_raboti}";
        }

        public AddEdit_Raboti()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление работы";
        }

        private void FillForm(Entities.dm_Raboti work)
        {
            TextBoxNazvanie.Text = work.Naimenovanie;
            TextBoxCena.Text = work.Stoimost.ToString("N2");
            TextBoxDlitelnost.Text = work.Dlitelnost.ToString();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
            {
                return;
            }

            if (_isEditing)
            {
                UpdateWork();
            }
            else
            {
                AddNewWork();
            }

            MessageBox.Show("Данные успешно сохранены!", "Успех",
                          MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        // Добавление новой работы
        private void AddNewWork()
        {
            var newWork = new Entities.dm_Raboti
            {
                Naimenovanie = TextBoxNazvanie.Text,
                Stoimost = decimal.Parse(TextBoxCena.Text),
                Dlitelnost = int.Parse(TextBoxDlitelnost.Text)
            };

            App.Context.dm_Raboti.Add(newWork);
            App.Context.SaveChanges();
        }

        // Обновление существующей работы
        private void UpdateWork()
        {
            _currentWork.Naimenovanie = TextBoxNazvanie.Text;
            _currentWork.Stoimost = decimal.Parse(TextBoxCena.Text);
            _currentWork.Dlitelnost = int.Parse(TextBoxDlitelnost.Text);
            App.Context.SaveChanges();
        }

        // Валидация полей
        private bool ValidateFields()
        {
            // Проверка на заполненность полей
            if (string.IsNullOrWhiteSpace(TextBoxNazvanie.Text) || string.IsNullOrWhiteSpace(TextBoxDlitelnost.Text) ||
                string.IsNullOrWhiteSpace(TextBoxCena.Text))
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены.", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка названия (можно добавить дополнительные правила)
            if (TextBoxNazvanie.Text.Length > 100)
            {
                MessageBox.Show("Название работы не должно превышать 100 символов.", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка длительности (минуты)
            if (!int.TryParse(TextBoxDlitelnost.Text, out int durationMinutes) ||
                durationMinutes <= 0 ||
                durationMinutes > 14400) // 1440 мин = 24 часа
            {
                MessageBox.Show("Длительность должна быть целым числом (в минутах), от 1 до 1440.", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка цены
            if (!decimal.TryParse(TextBoxCena.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную положительную стоимость.", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
