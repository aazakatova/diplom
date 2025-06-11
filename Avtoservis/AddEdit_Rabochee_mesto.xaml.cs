using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Логика взаимодействия для AddEdit_Rabochee_mesto.xaml
    /// </summary>
    public partial class AddEdit_Rabochee_mesto : Window
    {
        private Entities.dm_Rabochie_mesta _currentRabocheeMesto;
        private bool _isEditing;
        private string _selectedImagePath;
        private string _originalIconPath;

        public AddEdit_Rabochee_mesto(Entities.dm_Rabochie_mesta rabocheeMesto) : this()
        {
            _currentRabocheeMesto = rabocheeMesto;
            _isEditing = true;
            _originalIconPath = rabocheeMesto.Icon;
            FillForm(_currentRabocheeMesto);
            TextBlockZagolovok.Text = $"Редактирование рабочего места {rabocheeMesto.ID_rabochego_mesta}";
        }

        public AddEdit_Rabochee_mesto()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление рабочего места";
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
            var newRabocheeMesto = new Entities.dm_Rabochie_mesta
            {
                Rabochee_mesto = TextBoxNazvanie.Text,
                Icon = SaveImage(_selectedImagePath)
            };

            App.Context.dm_Rabochie_mesta.Add(newRabocheeMesto);
            App.Context.SaveChanges();
        }

        private void Update()
        {
            _currentRabocheeMesto.Rabochee_mesto = TextBoxNazvanie.Text;

            // Обновляем иконку только если она была выбрана
            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                _currentRabocheeMesto.Icon = SaveImage(_selectedImagePath);
            }
            // Если иконка не выбрана, оставляем текущее значение (не изменяем его на null)

            App.Context.SaveChanges();
        }

        private string SaveImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            string rabochieMestaFolder = GetRabocheeMestaFolderPath();
            bool isAlreadyInRabocheeMestaFolder = imagePath.StartsWith(rabochieMestaFolder, StringComparison.OrdinalIgnoreCase);

            if (isAlreadyInRabocheeMestaFolder)
            {
                // Иконка уже в папке RabocheeMesta - просто сохраняем имя файла
                return System.IO.Path.GetFileName(imagePath);
            }
            else
            {
                // Иконка не в папке RabocheeMesta - копируем с оригинальным именем
                if (!Directory.Exists(rabochieMestaFolder))
                    Directory.CreateDirectory(rabochieMestaFolder);

                string fileName = System.IO.Path.GetFileName(imagePath);
                string newIconPath = System.IO.Path.Combine(rabochieMestaFolder, fileName);

                // Проверяем, не существует ли файл с таким именем
                if (File.Exists(newIconPath))
                {
                    // Если файл существует, добавляем к имени временную метку
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string fileExt = System.IO.Path.GetExtension(fileName);
                    fileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                    newIconPath = System.IO.Path.Combine(rabochieMestaFolder, fileName);
                }

                File.Copy(imagePath, newIconPath);
                return fileName;
            }
        }

        private string GetRabocheeMestaFolderPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "Rabochie_mesta");
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = System.IO.Path.Combine(GetRabocheeMestaFolderPath(), imageName);

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

        private void FillForm(Entities.dm_Rabochie_mesta rabocheeMesto)
        {
            TextBoxNazvanie.Text = rabocheeMesto.Rabochee_mesto;

            if (!string.IsNullOrEmpty(rabocheeMesto.Icon))
            {
                ImageService.Source = LoadImage(rabocheeMesto.Icon);
                ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImageService.Source = null;
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
                    ImageService.Source = new BitmapImage(new Uri(_selectedImagePath));
                    ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки иконки: {ex.Message}");
                }
            }
            else
            {
                _selectedImagePath = null;
                if (string.IsNullOrEmpty(_originalIconPath))
                {
                    ImageService.Source = null;
                    ImagePlaceholderBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    ImageService.Source = LoadImage(_originalIconPath);
                    ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TextBoxNazvanie.Text))
            {
                MessageBox.Show("Название рабочего места должно быть заполнено.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (TextBoxNazvanie.Text.Length > 100)
            {
                MessageBox.Show("Название рабочего места не должно превышать 100 символов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
