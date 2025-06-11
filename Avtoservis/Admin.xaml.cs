using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Логика взаимодействия для Admin.xaml
    /// </summary>
    public partial class Admin : Window
    {
        public Admin()
        {
            InitializeComponent();
            LoadNameAndRol();

            var API = new API();
            FrameMain.Navigate(API);
        }

        private void ProfilPage_PhotoUpdated(object sender, EventArgs e)
        {
            // Обновляем фото в Admin окне
            LoadNameAndRol();

            // Если нужно, можно также обновить текущую страницу ProfilPage
            if (FrameMain.Content is ProfilPage page)
            {
                page.LoadUserData();
            }
        }

        public void LoadNameAndRol()
        {
            try
            {
                var userData = App.Context.dm_Users.Include("dm_Roli").FirstOrDefault(u => u.ID_user == App.CurrentUser.ID_user);
                TextBlockName.Text = $"{userData.Imya}";
                TextBlockRol.Text = $"{userData.dm_Roli.Rol}";

                // Устанавливаем инициалы
                InitialsText.Text = GetUserInitials(userData.Imya);

                // Загружаем фото
                var image = LoadImage(userData.Foto);
                if (image != null)
                {
                    FotoUser.Source = image;
                    InitialsText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    FotoUser.Source = null;
                    InitialsText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                InitialsText.Text = "?";
                InitialsText.Visibility = Visibility.Visible;
                FotoUser.Source = null;
            }
        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Вы точно хотите выйти из аккаунта?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    App.CurrentUser = null;
                    Window MainWindow = new MainWindow();
                    MainWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = GetImagePath(imageName);

            try
            {
                if (System.IO.File.Exists(imagePath))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(imagePath);
                    image.EndInit();
                    return image;
                }
            }
            catch
            {
                // В случае ошибки возвращаем null
            }

            return null;
        }

        private string GetImagePath(string imageName)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "Users", imageName);
        }

        private BitmapImage CreateDefaultImage(string userName = null)
        {
            int width = 50;
            int height = 50;

            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Рисуем круг с цветом #FF567BFF (как в вашем дизайне)
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF567BFF"));
                drawingContext.DrawEllipse(brush, null, new Point(width / 2, height / 2), width / 2, height / 2);

                // Добавляем инициалы пользователя (если имя указано)
                if (!string.IsNullOrEmpty(userName))
                {
                    var text = new FormattedText(
                        GetUserInitials(userName),
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        16,
                        Brushes.White,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    drawingContext.DrawText(text, new Point(width / 2 - text.Width / 2, height / 2 - text.Height / 2));
                }
            }

            renderTarget.Render(drawingVisual);
            var bitmapImage = new BitmapImage();
            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTarget));

            using (var stream = new MemoryStream())
            {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }

            return bitmapImage;
        }

        private string GetUserInitials(string userName)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                return userName.Substring(0, 1).ToUpper();
            }
            return "?";
        }

        private void ButtonProfil_Click(object sender, RoutedEventArgs e)
        {
            var profilPage = new ProfilPage(App.CurrentUser);
            profilPage.PhotoUpdated += ProfilPage_PhotoUpdated; // Подписываемся на событие
            FrameMain.Navigate(profilPage);
        }


        private Border currentPopup;
        private Button currentButton;

        private void ButtonSpravochniki_Click(object sender, RoutedEventArgs e)
        {
            ShowMenu(ButtonSpravochniki, new string[] { "Работы", "Детали", "Рабочие места", "Марки автомобилей", "Модели автомобилей" });
        }

        private void ShowMenu(Button button, string[] items)
        {
            // Если меню уже открыто для этой кнопки - закрываем
            if (currentPopup != null && currentButton == button)
            {
                CloseMenu();
                return;
            }

            CloseMenu(); // Закрываем предыдущее меню

            currentButton = button; // Запоминаем текущую кнопку

            // Создаем меню (ваш существующий код)
            currentPopup = new Border
            {
                Style = (Style)FindResource("PopupBlockStyle"),
                Padding = new Thickness(0,5,0,5)
            };

            var stackPanel = new StackPanel();

            foreach (var item in items)
            {
                var menuButton = new Button
                {
                    Style = (Style)FindResource("SubMenuButtonStyle"),
                    Content = new TextBlock
                    {
                        Text = item,
                        Margin = new Thickness(10, 0, 0, 0) // Отступ слева
                    },
                    MinWidth = 250,
                    MinHeight = 40,
                    FontSize = 15,
                    Tag = item
                };
                menuButton.Click += MenuItem_Click;
                stackPanel.Children.Add(menuButton);
            }

            currentPopup.Child = stackPanel;
            PopupCanvas.Children.Add(currentPopup);

            // Позиционирование
            Point buttonPos = button.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(currentPopup, buttonPos.X + button.ActualWidth - 260);
            Canvas.SetTop(currentPopup, buttonPos.Y - 20); // Подправлено смещение

            // Подписываемся на глобальное событие клика
            this.PreviewMouseDown += Window_PreviewMouseDown;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            string item = (string)button.Tag;

            // Обрабатываем выбор пункта меню
            switch (item)
            {
                case "Работы":
                    FrameMain.Navigate(new Admin_Raboti());
                    break;

                case "Детали":
                    FrameMain.Navigate(new Admin_Detali());
                    break;

                case "Рабочие места":
                    FrameMain.Navigate(new Admin_Rabochie_mesta());
                    break;

                case "Марки автомобилей":
                    FrameMain.Navigate(new Admin_Marki_avto());
                    break;

                case "Модели автомобилей":
                    FrameMain.Navigate(new Admin_Modeli_avto());
                    break;

                case "Сотрудники":
                    FrameMain.Navigate(new Admin_Sotrudniki());
                    break;

                case "Клиенты":
                    FrameMain.Navigate(new Admin_Klienti());
                    break;

                case "Автомобили":
                    FrameMain.Navigate(new Admin_Avtomobili());
                    break;

                case "Заказ-наряды":
                    FrameMain.Navigate(new Admin_Zakazi());
                    break;
                case "Акт выполненных работ":
                    FrameMain.Navigate(new Akt_vipolnennih_rabot());
                    break;
                case "Счёт на оплату":
                    FrameMain.Navigate(new Schet_na_oplatu());
                    break;
                case "Дефектная ведомость":
                    FrameMain.Navigate(new Defektnaya_vedomost());
                    break;

                    

                default:
                    break;
            }

            // Закрываем меню
            CloseMenu();
        }

        private void CloseMenu()
        {
            if (currentPopup != null)
            {
                PopupCanvas.Children.Remove(currentPopup);
                currentPopup = null;
                this.PreviewMouseDown -= Window_PreviewMouseDown; // Отписываемся от события
            }
            currentButton = null;
        }

        private void ButtonServis_Click(object sender, RoutedEventArgs e)
        {
            ShowMenu(ButtonServis, new string[] { "Сотрудники", "Клиенты", "Автомобили", "Заказ-наряды" });
        }

        private void ButtonOtcheti_Click(object sender, RoutedEventArgs e)
        {
            FrameMain.Navigate(new Otcheti());
        }

        private void ButtonDocumenti_Click(object sender, RoutedEventArgs e)
        {
            ShowMenu(ButtonDocumenti, new string[] { "Акт выполненных работ", "Счёт на оплату", "Дефектная ведомость" });
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, был ли клик вне меню и вне кнопки
            if (currentPopup != null &&
                !currentPopup.IsMouseOver &&
                !currentButton.IsMouseOver)
            {
                CloseMenu();
            }
        }

        private void GlavnayaStranica_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FrameMain.Navigate(new API());
        }
    }
}
