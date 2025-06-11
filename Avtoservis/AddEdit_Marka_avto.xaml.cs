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
    /// Логика взаимодействия для AddEdit_Marka_avto.xaml
    /// </summary>
    public partial class AddEdit_Marka_avto : Window
    {
        private Entities.dm_Marki_avto _currentMarka;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;
        private string _selectedImagePath;
        private string _originalPhotoPath;

        // Для хранения выбранных значений
        private Entities.dm_Gruppi_avto _selectedGruppa;
        private Entities.dm_Strani _selectedStrana;
        private int? _selectedGruppaId;
        private int? _selectedStranaId;

        public AddEdit_Marka_avto(Entities.dm_Marki_avto marka) : this()
        {
            _currentMarka = marka;
            _isEditing = true;
            _originalPhotoPath = marka.Logotip;
            FillForm(_currentMarka);
            TextBlockZagolovok.Text = $"Редактирование марки {marka.Nazvanie_marki}";
        }

        public AddEdit_Marka_avto()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление марки автомобиля";
        }

        private void BtnGruppi_Click(object sender, RoutedEventArgs e)
        {
            ShowSelectionPopup(
                (Button)sender,
                App.Context.dm_Gruppi_avto.ToList(),
                item => {
                    _selectedGruppa = item;
                    _selectedGruppaId = item.ID_gruppi;
                    BtnGruppi.Content = item.Nazvanie_gruppi;
                },
                "Nazvanie_gruppi"
            );
        }

        private void BtnStrani_Click(object sender, RoutedEventArgs e)
        {
            ShowSelectionPopup(
                (Button)sender,
                App.Context.dm_Strani.ToList(),
                item => {
                    _selectedStrana = item;
                    _selectedStranaId = item.ID_strani;
                    BtnStrani.Content = item.Strana;
                },
                "Strana"
            );
        }

        private void ShowSelectionPopup<T>(Button button, List<T> items, Action<T> onSelected, string displayProperty)
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
                var content = item.GetType().GetProperty(displayProperty)?.GetValue(item)?.ToString();
                var btn = new Button
                {
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Content = new TextBlock { Text = content, Margin = new Thickness(10, 0, 0, 0) },
                    MinWidth = button.ActualWidth - 2,
                    MinHeight = 40,
                    FontSize = 15,
                    Tag = item
                };

                btn.Click += (s, args) =>
                {
                    onSelected((T)((Button)s).Tag);
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
            var newMarka = new Entities.dm_Marki_avto
            {
                Nazvanie_marki = TextBoxNazvanie.Text,
                Gruppa = _selectedGruppaId.Value,
                Strana_proizvoditel = _selectedStranaId.Value,
                Logotip = SaveImage(_selectedImagePath)
            };

            App.Context.dm_Marki_avto.Add(newMarka);
            App.Context.SaveChanges();
        }

        private void Update()
        {
            _currentMarka.Nazvanie_marki = TextBoxNazvanie.Text;
            _currentMarka.Gruppa = _selectedGruppaId.Value;
            _currentMarka.Strana_proizvoditel = _selectedStranaId.Value;

            // Обновляем логотип только если он был выбран
            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                _currentMarka.Logotip = SaveImage(_selectedImagePath);
            }

            App.Context.SaveChanges();
        }

        private string SaveImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            string markiFolder = GetMarkiFolderPath();
            bool isAlreadyInMarkiFolder = imagePath.StartsWith(markiFolder, StringComparison.OrdinalIgnoreCase);

            if (isAlreadyInMarkiFolder)
            {
                // Фото уже в папке Marki - просто сохраняем имя файла
                return System.IO.Path.GetFileName(imagePath);
            }
            else
            {
                // Фото не в папке Marki - копируем с оригинальным именем
                if (!Directory.Exists(markiFolder))
                    Directory.CreateDirectory(markiFolder);

                string fileName = System.IO.Path.GetFileName(imagePath);
                string newPhotoPath = System.IO.Path.Combine(markiFolder, fileName);

                // Проверяем, не существует ли файл с таким именем
                if (File.Exists(newPhotoPath))
                {
                    // Если файл существует, добавляем к имени временную метку
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string fileExt = System.IO.Path.GetExtension(fileName);
                    fileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                    newPhotoPath = System.IO.Path.Combine(markiFolder, fileName);
                }

                File.Copy(imagePath, newPhotoPath);
                return fileName;
            }
        }

        private string GetMarkiFolderPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "Marki_avto");
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = System.IO.Path.Combine(GetMarkiFolderPath(), imageName);

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

        private void FillForm(Entities.dm_Marki_avto marka)
        {
            TextBoxNazvanie.Text = marka.Nazvanie_marki;

            // Загружаем выбранную группу
            var gruppa = App.Context.dm_Gruppi_avto.FirstOrDefault(g => g.ID_gruppi == marka.Gruppa);
            if (gruppa != null)
            {
                _selectedGruppa = gruppa;
                _selectedGruppaId = gruppa.ID_gruppi;
                BtnGruppi.Content = gruppa.Nazvanie_gruppi;
            }

            // Загружаем выбранную страну
            var strana = App.Context.dm_Strani.FirstOrDefault(s => s.ID_strani == marka.Strana_proizvoditel);
            if (strana != null)
            {
                _selectedStrana = strana;
                _selectedStranaId = strana.ID_strani;
                BtnStrani.Content = strana.Strana;
            }

            // Загрузка логотипа
            if (!string.IsNullOrEmpty(marka.Logotip))
            {
                ImageService.Source = LoadImage(marka.Logotip);
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
                    MessageBox.Show($"Ошибка загрузки логотипа: {ex.Message}");
                }
            }
            else
            {
                _selectedImagePath = null;
                if (string.IsNullOrEmpty(_originalPhotoPath))
                {
                    ImageService.Source = null;
                    ImagePlaceholderBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    ImageService.Source = LoadImage(_originalPhotoPath);
                    ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TextBoxNazvanie.Text) ||
                _selectedGruppaId == null ||
                _selectedStranaId == null)
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (TextBoxNazvanie.Text.Length > 500)
            {
                MessageBox.Show("Название марки не должно превышать 500 символов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
