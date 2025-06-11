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
    /// Логика взаимодействия для AddEdit_Komplektacii_modelei.xaml
    /// </summary>
    public partial class AddEdit_Komplektacii_modelei : Window
    {
        private Entities.dm_Komplektacii_avto _currentKomplekt;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;
        private string _selectedImagePath;
        private string _originalPhotoPath;
        private int _modelId;

        // Для хранения выбранных значений
        private Entities.dm_Tipi_korobki_peredach _selectedKorobka;
        private Entities.dm_Tipi_privoda _selectedPrivod;
        private Entities.dm_Tipi_dvigatelya _selectedDvigatel;
        private Entities.dm_Tipi_kuzova _selectedKuzov;

        public AddEdit_Komplektacii_modelei(Entities.dm_Komplektacii_avto komplekt) : this(komplekt.Model_avto)
        {
            _currentKomplekt = komplekt;
            _isEditing = true;
            _originalPhotoPath = komplekt.Foto;
            FillForm(_currentKomplekt);
            TextBlockZagolovok.Text = $"Редактирование комплектации №{komplekt.ID_komplektacii_avto}";
        }

        public AddEdit_Komplektacii_modelei(int modelId)
        {
            InitializeComponent();
            _isEditing = false;
            _modelId = modelId;
            TextBlockZagolovok.Text = "Добавление комплектации";
        }

        public AddEdit_Komplektacii_modelei(Entities.dm_Komplektacii_avto komplekt, bool isReadOnly)
            : this(komplekt.Model_avto) // вызываем базовый конструктор, чтобы проинициализировать XAML
        {
            // Заполняем форму из переданного объекта
            _currentKomplekt = komplekt;
            _isEditing = true; // уже редактирование, но мы блокируем все поля
            _originalPhotoPath = komplekt.Foto;
            FillForm(_currentKomplekt);

            // Заголовок «Комплектация [ID]»
            TextBlockZagolovok.Text = $"Комплектация {komplekt.dm_Modeli_avto.Model} №{komplekt.ID_komplektacii_avto}";

            // Скрываем кнопки «Выбрать фотографию» и «Сохранить»
            BtnSelectImage.Visibility = Visibility.Collapsed;
            BtnSave.Visibility = Visibility.Collapsed;

            // Блокируем все поля ввода (чтобы было только «просмотр»):
            TextBoxMoshnost.IsEnabled = false;
            BtnTipDvigatelya.IsEnabled = false;
            BtnTipKuzova.IsEnabled = false;
            BtnTipKorobkiPeredach.IsEnabled = false;
            BtnTipPrivoda.IsEnabled = false;
            ImageService.IsEnabled = false;
        }

        private void BtnTipi_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (button == BtnTipKorobkiPeredach)
            {
                ShowSelectionPopup(
                    button,
                    App.Context.dm_Tipi_korobki_peredach.ToList(),
                    item => {
                        _selectedKorobka = item;
                        BtnTipKorobkiPeredach.Content = item.Tip_korobki_peredach;
                    },
                    "Tip_korobki_peredach"
                );
            }
            else if (button == BtnTipPrivoda)
            {
                ShowSelectionPopup(
                    button,
                    App.Context.dm_Tipi_privoda.ToList(),
                    item => {
                        _selectedPrivod = item;
                        BtnTipPrivoda.Content = item.Tip_privoda;
                    },
                    "Tip_privoda"
                );
            }
            else if (button == BtnTipDvigatelya)
            {
                ShowSelectionPopup(
                    button,
                    App.Context.dm_Tipi_dvigatelya.ToList(),
                    item => {
                        _selectedDvigatel = item;
                        BtnTipDvigatelya.Content = item.Tip_dvigatelya;
                    },
                    "Tip_dvigatelya"
                );
            }
            else if (button == BtnTipKuzova)
            {
                ShowSelectionPopup(
                    button,
                    App.Context.dm_Tipi_kuzova.ToList(),
                    item => {
                        _selectedKuzov = item;
                        BtnTipKuzova.Content = item.Tip_kuzova;
                    },
                    "Tip_kuzova"
                );
            }
        }

        private void ShowSelectionPopup<T>(Button button, System.Collections.Generic.List<T> items, Action<T> onSelected, string displayProperty)
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
            var newKomplekt = new Entities.dm_Komplektacii_avto
            {
                Moshnost = int.Parse(TextBoxMoshnost.Text),
                Tip_korobki_peredach = _selectedKorobka.ID_tipa_korobki_peredach,
                Tip_privoda = _selectedPrivod.ID_tipa_privoda,
                Tip_dvigatelya = _selectedDvigatel.ID_tipa_dvigatelya,
                Tip_kuzova = _selectedKuzov.ID_tipa_kuzova,
                Model_avto = _modelId,
                Foto = SaveImage(_selectedImagePath)
            };

            App.Context.dm_Komplektacii_avto.Add(newKomplekt);
            App.Context.SaveChanges();
        }

        private void Update()
        {
            _currentKomplekt.Moshnost = int.Parse(TextBoxMoshnost.Text);
            _currentKomplekt.Tip_korobki_peredach = _selectedKorobka.ID_tipa_korobki_peredach;
            _currentKomplekt.Tip_privoda = _selectedPrivod.ID_tipa_privoda;
            _currentKomplekt.Tip_dvigatelya = _selectedDvigatel.ID_tipa_dvigatelya;
            _currentKomplekt.Tip_kuzova = _selectedKuzov.ID_tipa_kuzova;

            // Обновляем фото только если оно было выбрано
            if (!string.IsNullOrEmpty(_selectedImagePath))
            {
                _currentKomplekt.Foto = SaveImage(_selectedImagePath);
            }

            App.Context.SaveChanges();
        }

        private string SaveImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            string komplektFolder = GetKomplektFolderPath();
            bool isAlreadyInKomplektFolder = imagePath.StartsWith(komplektFolder, StringComparison.OrdinalIgnoreCase);

            if (isAlreadyInKomplektFolder)
            {
                // Фото уже в папке Komplektacii - просто сохраняем имя файла
                return System.IO.Path.GetFileName(imagePath);
            }
            else
            {
                // Фото не в папке Komplektacii - копируем с оригинальным именем
                if (!Directory.Exists(komplektFolder))
                    Directory.CreateDirectory(komplektFolder);

                string fileName = System.IO.Path.GetFileName(imagePath);
                string newPhotoPath = System.IO.Path.Combine(komplektFolder, fileName);

                // Проверяем, не существует ли файл с таким именем
                if (File.Exists(newPhotoPath))
                {
                    // Если файл существует, добавляем к имени временную метку
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string fileExt = System.IO.Path.GetExtension(fileName);
                    fileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                    newPhotoPath = System.IO.Path.Combine(komplektFolder, fileName);
                }

                File.Copy(imagePath, newPhotoPath);
                return fileName;
            }
        }

        private string GetKomplektFolderPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "Komplektacii");
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = System.IO.Path.Combine(GetKomplektFolderPath(), imageName);

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

        private void FillForm(Entities.dm_Komplektacii_avto komplekt)
        {
            TextBoxMoshnost.Text = komplekt.Moshnost.ToString();

            // Загружаем выбранную коробку передач
            var korobka = App.Context.dm_Tipi_korobki_peredach.FirstOrDefault(k => k.ID_tipa_korobki_peredach == komplekt.Tip_korobki_peredach);
            if (korobka != null)
            {
                _selectedKorobka = korobka;
                BtnTipKorobkiPeredach.Content = korobka.Tip_korobki_peredach;
            }

            // Загружаем выбранный привод
            var privod = App.Context.dm_Tipi_privoda.FirstOrDefault(p => p.ID_tipa_privoda == komplekt.Tip_privoda);
            if (privod != null)
            {
                _selectedPrivod = privod;
                BtnTipPrivoda.Content = privod.Tip_privoda;
            }

            // Загружаем выбранный двигатель
            var dvigatel = App.Context.dm_Tipi_dvigatelya.FirstOrDefault(d => d.ID_tipa_dvigatelya == komplekt.Tip_dvigatelya);
            if (dvigatel != null)
            {
                _selectedDvigatel = dvigatel;
                BtnTipDvigatelya.Content = dvigatel.Tip_dvigatelya;
            }

            // Загружаем выбранный кузов
            var kuzov = App.Context.dm_Tipi_kuzova.FirstOrDefault(k => k.ID_tipa_kuzova == komplekt.Tip_kuzova);
            if (kuzov != null)
            {
                _selectedKuzov = kuzov;
                BtnTipKuzova.Content = kuzov.Tip_kuzova;
            }

            // Загрузка фотографии
            if (!string.IsNullOrEmpty(komplekt.Foto))
            {
                ImageService.Source = LoadImage(komplekt.Foto);
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
                    MessageBox.Show($"Ошибка загрузки фотографии: {ex.Message}");
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
            // Проверка выбранных значений
            if (_selectedKorobka == null || _selectedPrivod == null || string.IsNullOrWhiteSpace(TextBoxMoshnost.Text) ||
                _selectedDvigatel == null || _selectedKuzov == null)
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка мощности
            if (!int.TryParse(TextBoxMoshnost.Text, out _))
            {
                MessageBox.Show("Укажите корректную мощность (целое число).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
