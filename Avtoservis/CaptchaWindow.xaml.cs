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
using System.Windows.Threading;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для CaptchaWindow.xaml
    /// </summary>
    public partial class CaptchaWindow : Window
    {
        public event EventHandler CaptchaValidated;
        public event EventHandler CaptchaFailed;

        private int _incorrectAttempts = 0;
        private DispatcherTimer _blockTimer;
        private int _remainingSeconds = 10;
        private Random _random = new Random();
        private string _captchaText;
        private string _targetLogin;

        public CaptchaWindow(string login)
        {
            InitializeComponent();
            _targetLogin = login;
            InitializeTimer();
            UpdateCaptcha();
            this.Owner = Application.Current.MainWindow;
        }

        private void InitializeTimer()
        {
            _blockTimer = new DispatcherTimer();
            _blockTimer.Interval = TimeSpan.FromSeconds(1);
            _blockTimer.Tick += BlockTimer_Tick;
        }

        private void UpdateCaptcha()
        {
            SPanelSymbols.Children.Clear();
            CanvasNoise.Children.Clear();
            _captchaText = GenerateSymbols(4);
            GenerateNoise(30);
        }

        private string GenerateSymbols(int count)
        {
            string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string captcha = "";

            // Очищаем контейнеры
            SPanelSymbols.Children.Clear();
            CanvasNoise.Children.Clear();
            CanvasStrikethrough.Children.Clear();

            for (int i = 0; i < count; i++)
            {
                string symbol = alphabet.ElementAt(_random.Next(0, alphabet.Length)).ToString();
                TextBlock lbl = new TextBlock();
                lbl.Text = symbol;
                lbl.FontSize = _random.Next(40, 50);
                lbl.FontWeight = FontWeights.Bold;
                lbl.Foreground = Brushes.Black;
                lbl.Margin = new Thickness(15, 0, 15, 0);

                // Небольшой наклон (не более 15 градусов)
                lbl.RenderTransform = new RotateTransform(_random.Next(-15, 15));

                SPanelSymbols.Children.Add(lbl);
                captcha += symbol;
            }

            // Добавляем перечёркивающие линии (2 линии)
            AddStrikethroughLines();

            return captcha;
        }

        private void AddStrikethroughLines()
        {
            // Первая перечёркивающая линия
            Line line1 = new Line();
            line1.Stroke = Brushes.Black;
            line1.StrokeThickness = 1.5;
            line1.X1 = 20;
            line1.Y1 = _random.Next(30, 70);
            line1.X2 = 380;
            line1.Y2 = _random.Next(30, 70);
            CanvasStrikethrough.Children.Add(line1);

            // Вторая перечёркивающая линия
            Line line2 = new Line();
            line2.Stroke = Brushes.Black;
            line2.StrokeThickness = 1.5;
            line2.X1 = 20;
            line2.Y1 = _random.Next(30, 70);
            line2.X2 = 380;
            line2.Y2 = _random.Next(30, 70);
            CanvasStrikethrough.Children.Add(line2);
        }


        private void GenerateNoise(int volumeNoise)
        {
            for (int i = 0; i < volumeNoise; i++)
            {
                // Только чёрные линии и точки
                if (_random.Next(0, 2) == 0)
                {
                    // Вертикальные или горизонтальные линии
                    Line noiseLine = new Line();
                    noiseLine.Stroke = Brushes.Black;
                    noiseLine.StrokeThickness = 0.5;

                    if (_random.Next(0, 2) == 0)
                    {
                        // Горизонтальная
                        noiseLine.X1 = _random.Next(0, 400);
                        noiseLine.Y1 = _random.Next(0, 120);
                        noiseLine.X2 = noiseLine.X1 + _random.Next(5, 20);
                        noiseLine.Y2 = noiseLine.Y1;
                    }
                    else
                    {
                        // Вертикальная
                        noiseLine.X1 = _random.Next(0, 400);
                        noiseLine.Y1 = _random.Next(0, 120);
                        noiseLine.X2 = noiseLine.X1;
                        noiseLine.Y2 = noiseLine.Y1 + _random.Next(5, 20);
                    }

                    // Проверяем, чтобы не выходило за границы
                    if (noiseLine.X2 > 400) noiseLine.X2 = 400;
                    if (noiseLine.Y2 > 120) noiseLine.Y2 = 120;

                    CanvasNoise.Children.Add(noiseLine);
                }
                else
                {
                    // Точки
                    Ellipse dot = new Ellipse();
                    dot.Fill = Brushes.Black;
                    dot.Width = dot.Height = _random.Next(1, 3);
                    CanvasNoise.Children.Add(dot);
                    Canvas.SetLeft(dot, _random.Next(0, 400));
                    Canvas.SetTop(dot, _random.Next(0, 120));
                }
            }
        }

        private void BtnUpdateCaptcha_Click(object sender, RoutedEventArgs e)
        {
            if (!_blockTimer.IsEnabled)
            {
                UpdateCaptcha();
            }
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            if (_blockTimer.IsEnabled)
            {
                MessageBox.Show($"Пожалуйста, подождите {_remainingSeconds} секунд перед следующей попыткой.");
                return;
            }

            if (TextBoxCaptcha.Text == _captchaText)
            {
                App.ResetCaptcha(_targetLogin);
                CaptchaValidated?.Invoke(this, EventArgs.Empty);
                this.Close();
            }
            else
            {
                _incorrectAttempts++;
                if (_incorrectAttempts >= 3)
                {
                    _blockTimer.Start();
                    _remainingSeconds = 10;
                    UpdateTimerText();
                    TextBoxCaptcha.Text = "";
                    TextBoxCaptcha.IsEnabled = false;
                    BtnContinue.IsEnabled = false;
                    BtnUpdateCaptcha.IsEnabled = false;
                    TextBlockTimer.Visibility = Visibility.Visible;
                    App.BlockAccess();
                    CaptchaFailed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageBox.Show("Неверная капча. Пожалуйста, попробуйте снова.");
                    UpdateCaptcha();
                    TextBoxCaptcha.Text = "";
                }
            }
        }

        private void BlockTimer_Tick(object sender, EventArgs e)
        {
            _remainingSeconds--;
            UpdateTimerText();

            if (_remainingSeconds <= 0)
            {
                _incorrectAttempts = 0;
                _blockTimer.Stop();
                UpdateCaptcha();
                TextBoxCaptcha.Text = "";
                TextBoxCaptcha.IsEnabled = true;
                BtnContinue.IsEnabled = true;
                BtnUpdateCaptcha.IsEnabled = true;
                TextBlockTimer.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateTimerText()
        {
            TextBlockTimer.Text = $"Доступ запрещён на: {_remainingSeconds} сек";
        }
    }
}
