using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для AddEdit_Detali.xaml
    /// </summary>
    public partial class AddEdit_Detali : Window
    {
        private Entities.dm_Detali _currentDetal;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;
        private string _selectedImagePath;
        private string _originalPhotoPath;

        // Для хранения выбранных значений
        private Entities.dm_Tipi_detalei _selectedTip;
        private Entities.dm_Proizvoditeli _selectedProizvoditel;
        private int? _selectedTipId;
        private int? _selectedProizvoditelId;

        public AddEdit_Detali(Entities.dm_Detali detal) : this()
        {
            _currentDetal = detal;
            _isEditing = true;
            _originalPhotoPath = detal.Foto;
            FillForm(_currentDetal);
            TextBlockZagolovok.Text = $"Редактирование детали №{detal.ID_detali}";
        }

        public AddEdit_Detali()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление детали";
        }

        public AddEdit_Detali(Entities.dm_Detali detal, bool isReadOnly)
        : this()   // вызываем базовый конструктор, чтобы проинициализировать XAML
        {
            // Заполняем форму из переданного объекта
            _currentDetal = detal;
            _isEditing = true; // уже редактирование, но мы блокируем все поля
            _originalPhotoPath = detal.Foto;
            FillForm(_currentDetal);

            // Заголовок «Деталь [Название]»
            TextBlockZagolovok.Text = $"Деталь «{detal.Nazvanie}»";

            // Скрываем кнопки «Выбрать фотографию» и «Сохранить»
            BtnSelectImage.Visibility = Visibility.Collapsed;
            BtnSave.Visibility = Visibility.Collapsed;

            // Блокируем все поля ввода (чтобы было только «просмотр»):
            TextBoxNazvanie.IsEnabled = false;
            BtnTipi.IsEnabled = false;
            BtnProizvoditeli.IsEnabled = false;
            TextBoxCena.IsEnabled = false;
            TextBoxOpisanie.IsEnabled = false;
            // Само изображение уже загрузилось в FillForm, но загрузка новой блокируется:
            ImageService.IsEnabled = false;
            // Popup-кнопки выбора типа/производителя тоже недоступны:
            BtnTipi.IsEnabled = false;
            BtnProizvoditeli.IsEnabled = false;
        }

        private void BtnTipi_Click(object sender, RoutedEventArgs e)
        {
            ShowSelectionPopup(
                (Button)sender,
                App.Context.dm_Tipi_detalei.ToList(),
                item => {
                    _selectedTip = item;
                    _selectedTipId = item.ID_tipa;
                    BtnTipi.Content = item.Nazvanie_tipa;
                },
                "Nazvanie_tipa"
            );
        }

        private void BtnProizvoditeli_Click(object sender, RoutedEventArgs e)
        {
            ShowSelectionPopup(
                (Button)sender,
                App.Context.dm_Proizvoditeli.ToList(),
                item => {
                    _selectedProizvoditel = item;
                    _selectedProizvoditelId = item.ID_proizvoditelya;
                    BtnProizvoditeli.Content = item.Nazvanie_proizvoditelya;
                },
                "Nazvanie_proizvoditelya"
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

            // Точное позиционирование относительно экрана
            Point buttonPos = button.PointToScreen(new Point(0, 0));
            Point windowPos = this.PointToScreen(new Point(0, 0));
            double relativeX = buttonPos.X - windowPos.X;
            double relativeY = buttonPos.Y - windowPos.Y + button.ActualHeight + 5;

            // Округление координат для четкого рендеринга
            Canvas.SetLeft(_currentPopup, Math.Round(relativeX));
            Canvas.SetTop(_currentPopup, Math.Round(relativeY));

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
            var newDetal = new Entities.dm_Detali
            {
                Nazvanie = TextBoxNazvanie.Text,
                Tip = _selectedTipId.Value,
                Proizvoditel = _selectedProizvoditelId.Value,
                Cena = decimal.Parse(TextBoxCena.Text),
                Foto = SaveImage(_selectedImagePath),
                Opisanie = string.IsNullOrWhiteSpace(TextBoxOpisanie.Text) ? null : TextBoxOpisanie.Text
            };

            App.Context.dm_Detali.Add(newDetal);
            App.Context.SaveChanges();
        }

        private void Update()
        {
            _currentDetal.Nazvanie = TextBoxNazvanie.Text;
            _currentDetal.Tip = _selectedTipId.Value;
            _currentDetal.Proizvoditel = _selectedProizvoditelId.Value;
            _currentDetal.Cena = decimal.Parse(TextBoxCena.Text);
            _currentDetal.Opisanie = string.IsNullOrWhiteSpace(TextBoxOpisanie.Text) ? null : TextBoxOpisanie.Text;

            // Обновляем фотографию только если она была выбрана
            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                _currentDetal.Foto = SaveImage(_selectedImagePath);
            }
            // Если фотография не выбрана, оставляем текущее значение (не изменяем его на null)

            App.Context.SaveChanges();
        }

        private string SaveImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            string detaliFolder = GetDetaliFolderPath();
            bool isAlreadyInDetaliFolder = imagePath.StartsWith(detaliFolder, StringComparison.OrdinalIgnoreCase);

            if (isAlreadyInDetaliFolder)
            {
                // Фото уже в папке Detali - просто сохраняем имя файла
                return System.IO.Path.GetFileName(imagePath);
            }
            else
            {
                // Фото не в папке Detali - копируем с оригинальным именем
                if (!Directory.Exists(detaliFolder))
                    Directory.CreateDirectory(detaliFolder);

                string fileName = System.IO.Path.GetFileName(imagePath);
                string newPhotoPath = System.IO.Path.Combine(detaliFolder, fileName);

                // Проверяем, не существует ли файл с таким именем
                if (File.Exists(newPhotoPath))
                {
                    // Если файл существует, добавляем к имени временную метку
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string fileExt = System.IO.Path.GetExtension(fileName);
                    fileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                    newPhotoPath = System.IO.Path.Combine(detaliFolder, fileName);
                }

                File.Copy(imagePath, newPhotoPath);
                return fileName;
            }
        }

        private string GetDetaliFolderPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "Detali");
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = System.IO.Path.Combine(GetDetaliFolderPath(), imageName);

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

        private void FillForm(Entities.dm_Detali detali)
        {
            TextBoxNazvanie.Text = detali.Nazvanie;
            TextBoxCena.Text = detali.Cena.ToString("N2");
            TextBoxOpisanie.Text = detali.Opisanie;

            // Загружаем выбранный тип
            var tip = App.Context.dm_Tipi_detalei.FirstOrDefault(t => t.ID_tipa == detali.Tip);
            if (tip != null)
            {
                _selectedTip = tip;
                _selectedTipId = tip.ID_tipa;
                BtnTipi.Content = tip.Nazvanie_tipa;
            }

            // Загружаем выбранного производителя
            var proizvoditel = App.Context.dm_Proizvoditeli.FirstOrDefault(p => p.ID_proizvoditelya == detali.Proizvoditel);
            if (proizvoditel != null)
            {
                _selectedProizvoditel = proizvoditel;
                _selectedProizvoditelId = proizvoditel.ID_proizvoditelya;
                BtnProizvoditeli.Content = proizvoditel.Nazvanie_proizvoditelya;
            }

            // Загрузка фото
            if (!string.IsNullOrEmpty(detali.Foto))
            {
                ImageService.Source = LoadImage(detali.Foto);
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
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
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
                _selectedTipId == null ||
                _selectedProizvoditelId == null ||
                string.IsNullOrWhiteSpace(TextBoxCena.Text))
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (TextBoxNazvanie.Text.Length > 100)
            {
                MessageBox.Show("Название детали не должно превышать 100 символов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

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
