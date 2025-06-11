using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для API.xaml
    /// </summary>
    public partial class API : Page
    {
        private const string ApiKey = "2f8e7d7c3b5711f0a6aa0242ac120002";
        private static readonly HttpClient client = new HttpClient();

        public API()
        {
            InitializeComponent();
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string vin = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(vin) || InputBox.Text == "Введите VIN")
            {
                MessageBox.Show("Введите VIN", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadingIndicator.Visibility = Visibility.Visible;
            ResultsScrollViewer.Visibility = Visibility.Collapsed;
            ErrorMessage.Visibility = Visibility.Collapsed;

            try
            {
                var info = await GetCarInfoAsync(vin);
                if (info?.Data?.Basic != null)
                {
                    DisplayCarInfo(info);

                    // Сдвигаем элементы вверх
                    TitleText.Margin = new Thickness(0, 20, 0, 10); // Поднимаем заголовок
                    InputContainer.Margin = new Thickness(0, 0, 0, 10); // Поднимаем контейнер с полем и кнопкой

                    ResultsScrollViewer.Visibility = Visibility.Visible;
                }
                else
                {
                    ErrorMessage.Text = "Информация по данному VIN не найдена";
                    ErrorMessage.Visibility = Visibility.Visible;
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage.Text = $"Ошибка сети: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Ошибка: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<VdResponse> GetCarInfoAsync(string vin)
        {
            string url = $"https://api.vehicledatabases.com/vin-decode/{vin}";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("x-AuthKey", ApiKey);

            var resp = await client.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            string json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<VdResponse>(json);
        }

        private void DisplayCarInfo(VdResponse r)
        {
            var basic = r.Data.Basic;

            TextBlockMake.Text = SafeValue(basic.Make);
            TextBlockModel.Text = SafeValue(basic.Model);
            TextBlockYear.Text = SafeValue(basic.Year);
            TextBlockTrim.Text = SafeValue(basic.Trim);
            TextBlockBody.Text = Translate("body_type", basic.BodyType);
            TextBlockEngine.Text = SafeValue(basic.Engine?.EngineDescription, "Нет данных");
            TextBlockFuel.Text = Translate("fuel", basic.Fuel?.FuelType);
            TextBlockTransmission.Text = Translate("transmission", basic.Transmission?.TransmissionStyle);
            TextBlockCountry.Text = Translate("country", basic.Manufacturer?.Country);
        }

        private string Translate(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Нет данных";

            var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["body_type"] = new Dictionary<string, string>
                {
                    ["Coupe"] = "Купе",
                    ["Sedan"] = "Седан",
                    ["Hatchback"] = "Хэтчбек",
                    ["Liftback"] = "Лифтбэк",
                    ["Notchback"] = "Нотчбэк",
                    ["SUV"] = "Внедорожник",
                    ["Wagon"] = "Универсал"
                },
                ["fuel"] = new Dictionary<string, string>
                {
                    ["Gasoline"] = "Бензин",
                    ["Diesel"] = "Дизель",
                    ["Electric"] = "Электричество",
                    ["Hybrid"] = "Гибрид"
                },
                ["transmission"] = new Dictionary<string, string>
                {
                    ["Automatic"] = "Автоматическая",
                    ["Manual"] = "Механическая",
                    ["CVT"] = "Вариатор"
                },
                ["country"] = new Dictionary<string, string>
                {
                    ["South Korea"] = "Южная Корея",
                    ["United States (USA)"] = "США",
                    ["Japan"] = "Япония",
                    ["Germany"] = "Германия"
                }
            };

            if (value.Contains("/"))
            {
                var parts = value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    if (dict.TryGetValue(key, out var options) && options.TryGetValue(part, out var translated))
                        parts[i] = translated;
                }
                return string.Join(" / ", parts);
            }
            else
            {
                if (dict.TryGetValue(key, out var options) && options.TryGetValue(value, out var translated))
                    return translated;
            }

            return value;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "Введите VIN")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введите VIN";
                textBox.Foreground = Brushes.Gray;
                InputBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            }
        }

        private bool IsChildOf(DependencyObject child, DependencyObject parent)
        {
            while (child != null)
            {
                if (child == parent)
                    return true;
                child = VisualTreeHelper.GetParent(child);
            }
            return false;
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;

            // Остальная логика обработки клика (поиск и пагинация)
            bool clickInsideSearchBox = IsChildOf(originalSource, InputBox);

            if (!clickInsideSearchBox)
            {
                if (InputBox.IsFocused)
                {
                    Keyboard.ClearFocus();
                    if (string.IsNullOrWhiteSpace(InputBox.Text))
                    {
                        InputBox.Text = "Введите VIN";
                        InputBox.Foreground = new SolidColorBrush(Colors.Gray);
                        InputBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    }
                }
            }
            else
            {
                if (InputBox.Text == "Введите VIN")
                {
                    InputBox.Text = "";
                    InputBox.Foreground = new SolidColorBrush(Colors.Black);
                    InputBox.BorderBrush = new SolidColorBrush(Colors.Blue);
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private string SafeValue(string value, string fallback = "—")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }

    public class VdResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("data")]
        public VdData Data { get; set; }
    }

    public class VdData
    {
        [JsonProperty("intro")]
        public Intro Intro { get; set; }

        [JsonProperty("basic")]
        public Basic Basic { get; set; }
    }

    public class Intro
    {
        [JsonProperty("vin")]
        public string Vin { get; set; }
    }

    public class Basic
    {
        [JsonProperty("make")]
        public string Make { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("year")]
        public string Year { get; set; }

        [JsonProperty("trim")]
        public string Trim { get; set; }

        [JsonProperty("body_type")]
        public string BodyType { get; set; }

        [JsonProperty("engine")]
        public ApiEngine Engine { get; set; }

        [JsonProperty("fuel")]
        public ApiFuel Fuel { get; set; }

        [JsonProperty("transmission")]
        public ApiTransmission Transmission { get; set; }

        [JsonProperty("manufacturer")]
        public ApiManufacturer Manufacturer { get; set; }
    }

    public class ApiEngine
    {
        [JsonProperty("engine_description")]
        public string EngineDescription { get; set; }
    }

    public class ApiFuel
    {
        [JsonProperty("fuel_type")]
        public string FuelType { get; set; }
    }

    public class ApiTransmission
    {
        [JsonProperty("transmission_style")]
        public string TransmissionStyle { get; set; }
    }

    public class ApiManufacturer
    {
        [JsonProperty("country")]
        public string Country { get; set; }
    }
}
