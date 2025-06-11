using Avtoservis.Entities;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => UpdateUI();
            timer.Start();
        }

        private void ButtonVhod_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsBlocked())  // Проверка, заблокирован ли вход из-за слишком большого количества неудачных попыток
            {
                MessageBox.Show($"Доступ заблокирован на {App.GetRemainingBlockTime().Seconds} секунд", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Warning); 
                return;
            }
            if (TextBoxLogin.Text == "" || PasswordBox.Password == "")   // Проверка, заполнены ли поля логина и пароля
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            bool isUserExists = App.Context.dm_Users.Any(p => p.Login == TextBoxLogin.Text); // Проверяем, существует ли пользователь с таким логином в базе данных
            if (!isUserExists)
            {
                MessageBox.Show("Пользователь с таким логином не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            var currentUser = App.Context.dm_Users.FirstOrDefault(p => p.Login == TextBoxLogin.Text);  // Получаем пользователя по логину
            int attempts = App.GetLoginAttempts(TextBoxLogin.Text);  // Получаем количество неудачных попыток входа для данного логина 
            if (attempts >= 3)  // Если количество неудачных попыток >= 3, то включаем капчу
            {
                ShowCaptchaAndVerify(currentUser, sender, e);
                return;
            }
            if (currentUser.Password == PasswordBox.Password)  // Проверка корректности пароля
            {
                // Успешный вход
                App.CurrentUser = currentUser;
                App.ResetCaptcha(currentUser.Login); // Сброс счетчика попыток
                MessageBox.Show("Вы успешно авторизованы!");
                if (App.CurrentUser.Rol == 1)    // Открытие соответствующего окна в зависимости от роли пользователя
                {
                    Window Admin = new Admin();
                    Admin.Show();
                    this.Close();
                }
                else if (App.CurrentUser.Rol == 2)
                {
                    Window Sotrudnik = new Sotrudnik();
                    Sotrudnik.Show();
                    this.Close();
                }
            }
            else
            {
                App.AddLoginAttempt(TextBoxLogin.Text, isUserExists);  // При неправильном пароле — увеличиваем счетчик попыток
                attempts = App.GetLoginAttempts(TextBoxLogin.Text);

                MessageBox.Show($"Неверный пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowCaptchaAndVerify(dm_Users user, object sender, RoutedEventArgs e)
        {
            var captchaWindow = new CaptchaWindow(TextBoxLogin.Text);
            captchaWindow.CaptchaValidated += (s, args) =>
            {
                // После успешной капчи проверяем пароль
                if (user.Password == PasswordBox.Password)
                {
                    App.CurrentUser = user;
                    App.ResetCaptcha(user.Login);
                    MessageBox.Show("Вы успешно авторизованы!");

                    if (App.CurrentUser.Rol == 1)
                    {
                        Window Admin = new Admin();
                        Admin.Show();
                        this.Close();
                    }
                    else if (App.CurrentUser.Rol == 2)
                    {
                        Window Sotrudnik = new Sotrudnik();
                        Sotrudnik.Show();
                        this.Close();
                    }

                }
                else
                {
                    App.AddLoginAttempt(TextBoxLogin.Text, true);
                    MessageBox.Show("Неверный пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            captchaWindow.CaptchaFailed += (s, args) =>
            {
                UpdateUI();
            };

            App.ShowCaptcha();
            captchaWindow.ShowDialog();
        }

        private void TextBlockZaregistrirovaca_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void UpdateUI()
        {
            bool isBlocked = App.IsBlocked();
            ButtonVhod.IsEnabled = !isBlocked;

            if (isBlocked)
            {
                ButtonVhod.Content = $"Заблокировано ({App.GetRemainingBlockTime().Seconds} сек)";
                ButtonVhod.Background = Brushes.LightGray;
            }
            else
            {
                ButtonVhod.Content = "Войти";
                ButtonVhod.Background = new SolidColorBrush(Color.FromRgb(0x56, 0x7B, 0xFF));
            }
        }
    }
}
