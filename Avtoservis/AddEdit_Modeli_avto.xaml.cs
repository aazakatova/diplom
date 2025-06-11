using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для AddEdit_Modeli_avto.xaml
    /// </summary>
    public partial class AddEdit_Modeli_avto : Window
    {
        private Entities.dm_Modeli_avto _currentModel;
        private bool _isEditing;
        private Border _currentPopup;
        private Button _currentButton;

        // Для хранения выбранных значений
        private Entities.dm_Marki_avto _selectedMarka;
        private int? _selectedMarkaId;
        private int? _selectedStartYear;
        private int? _selectedEndYear;

        public AddEdit_Modeli_avto(Entities.dm_Modeli_avto model) : this()
        {
            _currentModel = model;
            _isEditing = true;
            FillForm(_currentModel);
            TextBlockZagolovok.Text = $"Редактирование модели {model.Model}";
        }

        public AddEdit_Modeli_avto()
        {
            InitializeComponent();
            _isEditing = false;
            TextBlockZagolovok.Text = "Добавление модели автомобиля";
        }

        private void BtnTipi_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (button == BtnMarka)
            {
                ShowSelectionPopup(
                    BtnMarka,
                    App.Context.dm_Marki_avto.OrderBy(m => m.Nazvanie_marki).ToList(),
                    item => {
                        _selectedMarka = item;
                        _selectedMarkaId = item.ID_marki;
                        BtnMarka.Content = item.Nazvanie_marki;
                    },
                    "Nazvanie_marki"
                );
            }
            else if (button == BtnGodVipuska)
            {
                var years = new List<YearItem>();
                int currentYear = DateTime.Now.Year;
                for (int year = currentYear; year >= 1950; year--)
                {
                    years.Add(new YearItem { Year = year });
                }

                ShowSelectionPopup(
                    BtnGodVipuska,
                    years,
                    item => {
                        _selectedStartYear = item.Year;
                        BtnGodVipuska.Content = item.Year.ToString();
                    },
                    "Year"
                );
            }
            else if (button == BtnGodOkonchaniyaVipuska)
            {
                var years = new List<YearItem>();
                int currentYear = DateTime.Now.Year;
                years.Add(new YearItem { Year = null }); // "Ещё выпускается"
                for (int year = currentYear; year >= 1950; year--)
                {
                    years.Add(new YearItem { Year = year });
                }

                ShowSelectionPopup(
                    BtnGodOkonchaniyaVipuska,
                    years,
                    item => {
                        _selectedEndYear = item.IsStillProduced ? (int?)null : item.Year;
                        BtnGodOkonchaniyaVipuska.Content = item.DisplayText;
                    },
                    "DisplayText"
                );
            }
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
                MaxHeight = 160
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

            // Получаем абсолютные координаты кнопки на экране
            Point buttonPos = button.PointToScreen(new Point(0, 0));
            // Преобразуем в координаты относительно окна
            buttonPos = this.PointFromScreen(buttonPos);

            // Рассчитываем доступное пространство
            double popupHeight = Math.Min(items.Count * 40, 160); // Примерная высота

            // Для кнопки года окончания - всегда открываем вверх
            bool isEndYearButton = button == BtnGodOkonchaniyaVipuska;

            if (isEndYearButton)
            {
                // Всегда открываем вверх для года окончания
                Canvas.SetLeft(_currentPopup, buttonPos.X);
                Canvas.SetTop(_currentPopup, buttonPos.Y - popupHeight - 19); // Уменьшенный отступ
            }
            else
            {
                // Для остальных кнопок - стандартная логика
                double spaceBelow = this.ActualHeight - (buttonPos.Y + button.ActualHeight);
                double spaceAbove = buttonPos.Y;

                bool openUpwards = spaceBelow < popupHeight && spaceAbove >= popupHeight;

                if (openUpwards)
                {
                    // Открываем вверх
                    Canvas.SetLeft(_currentPopup, buttonPos.X);
                    Canvas.SetTop(_currentPopup, buttonPos.Y - popupHeight - 19);
                }
                else
                {
                    // Открываем вниз
                    Canvas.SetLeft(_currentPopup, buttonPos.X);
                    Canvas.SetTop(_currentPopup, buttonPos.Y + button.ActualHeight + 5);

                    // Если места мало, ограничиваем высоту
                    if (spaceBelow < popupHeight)
                    {
                        scrollViewer.MaxHeight = spaceBelow - 10;
                    }
                }
            }

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
            var newModel = new Entities.dm_Modeli_avto
            {
                Model = TextBoxNazvanie.Text,
                Marka = _selectedMarkaId.Value,
                God_vipuska = _selectedStartYear.Value,
                God_okonchaniya_vipuska = _selectedEndYear
            };

            App.Context.dm_Modeli_avto.Add(newModel);
            App.Context.SaveChanges();
        }

        private void Update()
        {
            _currentModel.Model = TextBoxNazvanie.Text;
            _currentModel.Marka = _selectedMarkaId.Value;
            _currentModel.God_vipuska = _selectedStartYear.Value;
            _currentModel.God_okonchaniya_vipuska = _selectedEndYear;

            App.Context.SaveChanges();
        }

        private void FillForm(Entities.dm_Modeli_avto model)
        {
            TextBoxNazvanie.Text = model.Model;

            // Загружаем выбранную марку
            var marka = App.Context.dm_Marki_avto.FirstOrDefault(m => m.ID_marki == model.Marka);
            if (marka != null)
            {
                _selectedMarka = marka;
                _selectedMarkaId = marka.ID_marki;
                BtnMarka.Content = marka.Nazvanie_marki;
            }

            // Загружаем год выпуска
            _selectedStartYear = model.God_vipuska;
            BtnGodVipuska.Content = model.God_vipuska.ToString();

            // Загружаем год окончания выпуска
            _selectedEndYear = model.God_okonchaniya_vipuska;
            BtnGodOkonchaniyaVipuska.Content = model.God_okonchaniya_vipuska?.ToString() ?? "Ещё выпускается";
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(TextBoxNazvanie.Text) || _selectedMarkaId == null || _selectedStartYear == null ||
        BtnGodOkonchaniyaVipuska.Content == null || BtnGodOkonchaniyaVipuska.Content.ToString() == "Выберите год окончания выпуска")
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (TextBoxNazvanie.Text.Length > 100)
            {
                MessageBox.Show("Название модели не должно превышать 100 символов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка, что год окончания не раньше года начала выпуска
            if (_selectedEndYear != null && _selectedEndYear < _selectedStartYear)
            {
                MessageBox.Show("Год окончания выпуска не может быть раньше года начала выпуска.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }

    public class YearItem
    {
        public int? Year { get; set; }
        public bool IsStillProduced => Year == null; // Новое свойство
        public string DisplayText => IsStillProduced ? "Ещё выпускается" : Year?.ToString();
    }
}
