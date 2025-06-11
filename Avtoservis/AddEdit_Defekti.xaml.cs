using Avtoservis.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Логика взаимодействия для AddEdit_Defekti.xaml
    /// </summary>
    public partial class AddEdit_Defekti : Window
    {
        private readonly int _orderId;

        /// <summary>
        /// Список всех узлов авто (dm_Uzli_avto) — для построения Popup.
        /// </summary>
        public List<dm_Uzli_avto> UzliList { get; set; }

        /// <summary>
        /// Коллекция дефектов, к которой привязан ItemsControl.
        /// </summary>
        public ObservableCollection<dm_Defekti> Defects { get; set; } = new ObservableCollection<dm_Defekti>();

        // Для работы с «всплывающим» окном
        private Border _currentPopup;
        private Button _currentButton;

        public AddEdit_Defekti(int orderId)
        {
            InitializeComponent();

            _orderId = orderId;

            TextBlockZagolovok.Text = $"Дефекты автомобиля по заказ-наряду №{_orderId}";

            // 1) Загружаем список узлов авто
            UzliList = App.Context.dm_Uzli_avto
                .OrderBy(u => u.Nazvanie_uzla_avto)
                .ToList();

            // 2) Загружаем из БД уже существующие дефекты (включая навигацию по узлам)
            var existing = App.Context.dm_Defekti
                .Include("dm_Uzli_avto")
                .Where(d => d.Zakaz == _orderId)
                .ToList();

            foreach (var d in existing)
            {
                Defects.Add(d);
            }

            // 3) Если дефектов нет, создаём один пустой блок по умолчанию
            if (!Defects.Any())
            {
                var newDefect = new dm_Defekti
                {
                    Uzel_avto = null,
                    Opisanie = string.Empty,
                    Rekomendacii = string.Empty,
                    Primechaniya = string.Empty,
                    Zakaz = _orderId
                };
                Defects.Add(newDefect);
            }

            // Привязываем DataContext к окну, чтобы XAML видел свойства UzliList и Defects
            this.DataContext = this;
        }

        private void BtnSelectUzel_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            // Если этот же Popup уже открыт — закрываем
            if (_currentPopup != null && _currentButton == button)
            {
                ClosePopup();
                return;
            }

            // Закрываем любой ранее открытый Popup
            ClosePopup();

            _currentButton = button;

            // Создаём панель с кнопками — по одной на каждый dm_Uzli_avto
            var popupContent = new StackPanel { Background = Brushes.White };

            foreach (var uzel in UzliList)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = uzel.Nazvanie_uzla_avto,
                        Margin = new Thickness(10, 0, 0, 0)
                    },
                    Tag = uzel, // запомним объект
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Height = 40,
                    FontSize = 15,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                };

                btn.Click += (s, args) =>
                {
                    // Когда пользователь выбрал конкретный узел из списка:
                    var selectedUzel = (dm_Uzli_avto)((Button)s).Tag;

                    // Получаем текущий дефект из DataContext той кнопки, на которой вызвали Popup
                    var defect = (dm_Defekti)button.DataContext;
                    if (defect != null)
                    {
                        defect.Uzel_avto = selectedUzel.ID_uzla_avto;
                        defect.dm_Uzli_avto = selectedUzel;

                        // Обновляем текст самой кнопки (которая в списке дефектов)
                        button.Content = selectedUzel.Nazvanie_uzla_avto;
                    }

                    ClosePopup();
                };

                popupContent.Children.Add(btn);
            }

            // Оборачиваем StackPanel в ScrollViewer, чтобы было прокручивание, если узлов > 4
            var scrollViewer = new ScrollViewer
            {
                Content = popupContent,
                MaxHeight = 160,                          // 5 элементов по 40px + 4*4px отступов = 180px
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            // Оборачиваем в Border, чтобы был видимый контур
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

            // Позиционирование Popup относительно окна
            ShowPopup(button, _currentPopup);

            // Ловим клики вне Popup, чтобы его закрыть
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void ShowPopup(Button button, Border popup)
        {
            // Получаем абсолютные координаты кнопки и окна на экране
            Point buttonPos = button.PointToScreen(new Point(0, 0));
            Point windowPos = this.PointToScreen(new Point(0, 0));

            double relativeX = buttonPos.X - windowPos.X;
            double relativeY = buttonPos.Y - windowPos.Y + button.ActualHeight + 2;

            // Располагаем Border (popup) на Canvas
            Canvas.SetLeft(popup, relativeX);
            Canvas.SetTop(popup, relativeY);

            PopupCanvas.Children.Add(popup);
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

        /// <summary>
        /// Добавляет новый пустой дефект в список
        /// </summary>
        private void BtnAddDefect_Click(object sender, RoutedEventArgs e)
        {
            var newDefect = new dm_Defekti
            {
                Uzel_avto = null,
                Opisanie = string.Empty,
                // Не заполняем необязательные поля сразу пустой строкой, оставляем null
                Rekomendacii = null,
                Primechaniya = null,
                Zakaz = _orderId
            };
            Defects.Add(newDefect);
        }

        /// <summary>
        /// Удаляет дефект из списка
        /// </summary>
        private void BtnRemoveDefect_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var defect = (dm_Defekti)btn.DataContext;
            if (defect != null)
            {
                Defects.Remove(defect);
            }
        }

        /// <summary>
        /// Закрывает окно без сохранения
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Сохраняет все дефекты: валидирует, удаляет старые, добавляет новые
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1) Валидация обязательных полей
            foreach (var d in Defects)
            {
                if (d.Uzel_avto == null)
                {
                    MessageBox.Show("Узел авто обязателен для каждого дефекта.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(d.Opisanie))
                {
                    MessageBox.Show("Описание обязательно для каждого дефекта.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Заменяем пустые или пробельные строки на null
                if (string.IsNullOrWhiteSpace(d.Rekomendacii))
                    d.Rekomendacii = null;
                // И аналогично для Primechaniya
                if (string.IsNullOrWhiteSpace(d.Primechaniya))
                    d.Primechaniya = null;
            }

            try
            {
                // 2) Берём из БД все дефекты для данного заказа
                var existingInDb = App.Context.dm_Defekti
                    .Where(d => d.Zakaz == _orderId)
                    .ToList();

                // 3) Удаляем те, которых нет в списке Defects
                foreach (var fromDb in existingInDb)
                {
                    if (!Defects.Any(x => x.ID_defekta == fromDb.ID_defekta))
                    {
                        App.Context.dm_Defekti.Remove(fromDb);
                    }
                }

                // 4) Добавляем новые и обновляем существующие
                foreach (var defect in Defects)
                {
                    if (defect.ID_defekta == 0)
                    {
                        // Новый дефект
                        App.Context.dm_Defekti.Add(defect);
                    }
                    else
                    {
                        // Существующий: EF уже отслеживает его, а свойства (включая null для пустых)
                        // обновлены из привязки и предыдущей логики выше.
                    }
                }

                // 5) Сохраняем изменения
                App.Context.SaveChanges();
                MessageBox.Show("Дефекты успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении дефектов: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}