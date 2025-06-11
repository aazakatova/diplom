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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для Admin_Avtomobili.xaml
    /// </summary>
    public partial class Admin_Avtomobili : Page
    {
        private Popup itemsPerPagePopup;
        private Button currentButton;
        private int currentPage = 1;
        private int itemsPerPage = 20;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentSortColumn = "ID_avtomobilya";
        private bool isAscending = true;

        // Фильтры
        private List<int> selectedManufacturers = new List<int>();
        private List<int> selectedStartYears = new List<int>();
        private List<int> selectedGroups = new List<int>();
        private List<int> selectedCountries = new List<int>();
        private List<int> selectedBodyTypes = new List<int>();
        private List<int> selectedGearboxTypes = new List<int>();
        private List<int> selectedDriveTypes = new List<int>();
        private List<int> selectedEngineTypes = new List<int>();
        private bool includeStillProduced = false;
        private int? minPower = null;
        private int? maxPower = null;

        private DispatcherTimer powerFilterTimer;
        private const int PowerFilterDelay = 500;

        private int? _clientId; // Добавляем поле для хранения ID клиента

        public Admin_Avtomobili(int? clientId = null)
        {
            _clientId = clientId;
            InitializeComponent();

            // Настройка видимости кнопки "Назад" и отступов заголовка
            if (_clientId.HasValue)
            {
                BtnBack.Visibility = Visibility.Visible;
                TitleText.Margin = new Thickness(90, 0, 0, 30);

                // Загружаем имя клиента для заголовка
                var client = App.Context.dm_Users.FirstOrDefault(u => u.ID_user == _clientId.Value);
                if (client != null)
                {
                    TitleText.Text = $"Автомобили клиента: {client.Familiya} {client.Imya}";
                }
            }
            else
            {
                BtnBack.Visibility = Visibility.Collapsed;
                TitleText.Margin = new Thickness(40, 0, 0, 30);
                TitleText.Text = "Автомобили клиентов";
            }

            Loaded += (s, e) =>
            {
                LoadFilters();
                LoadAvtomobili();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            };

            AvtomobiliList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();

            // Инициализация таймера для фильтрации по мощности
            powerFilterTimer = new DispatcherTimer();
            powerFilterTimer.Interval = TimeSpan.FromMilliseconds(PowerFilterDelay);
            powerFilterTimer.Tick += PowerFilterTimer_Tick;
        }

        public Admin_Avtomobili()
        {
            InitializeComponent();
            LoadAvtomobili();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
            LoadFilters();

            AvtomobiliList.SizeChanged += (s, e) => UpdateGridViewColumnWidths();
            Loaded += (s, e) => UpdateGridViewColumnWidths();

            // Инициализация таймера для фильтрации по мощности
            powerFilterTimer = new DispatcherTimer();
            powerFilterTimer.Interval = TimeSpan.FromMilliseconds(PowerFilterDelay);
            powerFilterTimer.Tick += PowerFilterTimer_Tick;
        }

        private void LoadAvtomobili()
        {
            try
            {
                var query = App.Context.dm_Avtomobili
                    .Include("dm_Komplektacii_avto")
                    .Include("dm_Komplektacii_avto.dm_Modeli_avto")
                    .Include("dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto")
                    .Include("dm_Komplektacii_avto.dm_Tipi_kuzova")
                    .Include("dm_Komplektacii_avto.dm_Tipi_korobki_peredach")
                    .Include("dm_Komplektacii_avto.dm_Tipi_privoda")
                    .Include("dm_Komplektacii_avto.dm_Tipi_dvigatelya")
                    .Include("dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.dm_Gruppi_avto")
                    .Include("dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.dm_Strani")
                    .Include("dm_Users")
                    .AsQueryable();

                // Добавляем фильтрацию по клиенту, если ID указан
                if (_clientId.HasValue)
                {
                    query = query.Where(a => a.Vladelec == _clientId.Value);
                }

                // Применение поиска
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    var searchText = currentSearchText.ToLower();
                    var ownerIds = App.Context.dm_Users
                        .Where(u => u.Familiya.ToLower().Contains(searchText) ||
                                    u.Imya.ToLower().Contains(searchText) ||
                                    u.Otchestvo.ToLower().Contains(searchText))
                        .Select(u => u.ID_user)
                        .ToList();

                    query = query.Where(a =>
                        (a.Gos_nomer != null && a.Gos_nomer.ToLower().Contains(searchText)) ||
                        ownerIds.Contains(a.Vladelec) ||
                        (a.dm_Komplektacii_avto != null &&
                         a.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                         a.dm_Komplektacii_avto.dm_Modeli_avto.Model != null &&
                         a.dm_Komplektacii_avto.dm_Modeli_avto.Model.ToLower().Contains(searchText)) ||
                        (a.dm_Komplektacii_avto != null &&
                         a.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                         a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto != null &&
                         a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki != null &&
                         a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Nazvanie_marki.ToLower().Contains(searchText)));
                }

                // Применение фильтров по маркам
                if (selectedManufacturers.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        a.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                        selectedManufacturers.Contains(a.dm_Komplektacii_avto.dm_Modeli_avto.Marka));
                }

                // Применение фильтров по годам выпуска
                if (selectedStartYears.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        a.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                        selectedStartYears.Contains(a.dm_Komplektacii_avto.dm_Modeli_avto.God_vipuska));
                }

                // Применение фильтров по типам кузова
                if (selectedBodyTypes.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        selectedBodyTypes.Contains(a.dm_Komplektacii_avto.Tip_kuzova));
                }

                // Применение фильтров по типам коробки передач
                if (selectedGearboxTypes.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        selectedGearboxTypes.Contains(a.dm_Komplektacii_avto.Tip_korobki_peredach));
                }

                // Применение фильтров по типам привода
                if (selectedDriveTypes.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        selectedDriveTypes.Contains(a.dm_Komplektacii_avto.Tip_privoda));
                }

                // Применение фильтров по типам двигателя
                if (selectedEngineTypes.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        selectedEngineTypes.Contains(a.dm_Komplektacii_avto.Tip_dvigatelya));
                }

                // Применение фильтров по производственным группам
                if (selectedGroups.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        a.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                        a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto != null &&
                        selectedGroups.Contains(a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Gruppa));
                }

                // Применение фильтров по странам-производителям
                if (selectedCountries.Count > 0)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        a.dm_Komplektacii_avto.dm_Modeli_avto != null &&
                        a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto != null &&
                        selectedCountries.Contains(a.dm_Komplektacii_avto.dm_Modeli_avto.dm_Marki_avto.Strana_proizvoditel));
                }

                // Применение фильтра по мощности
                if (minPower.HasValue)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        a.dm_Komplektacii_avto.Moshnost >= minPower.Value);
                }
                if (maxPower.HasValue)
                {
                    query = query.Where(a =>
                        a.dm_Komplektacii_avto != null &&
                        a.dm_Komplektacii_avto.Moshnost <= maxPower.Value);
                }

                totalItems = query.Count();

                // Применяем сортировку
                switch (currentSortColumn)
                {
                    case "Gos_nomer":
                        query = isAscending ?
                            query.OrderBy(a => a.Gos_nomer) :
                            query.OrderByDescending(a => a.Gos_nomer);
                        break;
                    default:
                        query = isAscending ?
                            query.OrderBy(a => a.ID_avtomobilya) :
                            query.OrderByDescending(a => a.ID_avtomobilya);
                        break;
                }

                // Пагинация
                var avtomobili = query
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                // Загрузка изображений
                foreach (var auto in avtomobili)
                {
                    if (auto.dm_Komplektacii_avto != null && !string.IsNullOrEmpty(auto.dm_Komplektacii_avto.Foto))
                    {
                        auto.dm_Komplektacii_avto.FotoImage = LoadImage(auto.dm_Komplektacii_avto.Foto);
                    }
                }

                AvtomobiliList.ItemsSource = avtomobili;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            string imagePath = System.IO.Path.Combine(projectDirectory, "Komplektacii", imageName);

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

        private void LoadFilters()
        {
            // Загрузка марок автомобилей
            var manufacturers = App.Context.dm_Marki_avto
                .OrderBy(m => m.Nazvanie_marki)
                .ToList();

            ManufacturerFilterPanel.Children.Clear();
            foreach (var manufacturer in manufacturers)
            {
                var checkBox = new CheckBox
                {
                    Content = manufacturer.Nazvanie_marki,
                    Tag = manufacturer.ID_marki,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += ManufacturerCheckBox_Checked;
                checkBox.Unchecked += ManufacturerCheckBox_Unchecked;
                ManufacturerFilterPanel.Children.Add(checkBox);
            }

            // Загрузка годов выпуска
            int currentYear = DateTime.Now.Year;
            StartYearFilterPanel.Children.Clear();
            for (int year = currentYear; year >= 1950; year--)
            {
                var checkBox = new CheckBox
                {
                    Content = year.ToString(),
                    Tag = year,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += StartYearCheckBox_Checked;
                checkBox.Unchecked += StartYearCheckBox_Unchecked;
                StartYearFilterPanel.Children.Add(checkBox);
            }

            // Загрузка типов кузова
            var bodyTypes = App.Context.dm_Tipi_kuzova
                .OrderBy(t => t.Tip_kuzova)
                .ToList();

            BodyTypeFilterPanel.Children.Clear();
            foreach (var type in bodyTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_kuzova,
                    Tag = type.ID_tipa_kuzova,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += BodyTypeCheckBox_Checked;
                checkBox.Unchecked += BodyTypeCheckBox_Unchecked;
                BodyTypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка типов коробки передач
            var gearboxTypes = App.Context.dm_Tipi_korobki_peredach
                .OrderBy(t => t.Tip_korobki_peredach)
                .ToList();

            GearboxTypeFilterPanel.Children.Clear();
            foreach (var type in gearboxTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_korobki_peredach,
                    Tag = type.ID_tipa_korobki_peredach,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += GearboxTypeCheckBox_Checked;
                checkBox.Unchecked += GearboxTypeCheckBox_Unchecked;
                GearboxTypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка типов привода
            var driveTypes = App.Context.dm_Tipi_privoda
                .OrderBy(t => t.Tip_privoda)
                .ToList();

            DriveTypeFilterPanel.Children.Clear();
            foreach (var type in driveTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_privoda,
                    Tag = type.ID_tipa_privoda,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += DriveTypeCheckBox_Checked;
                checkBox.Unchecked += DriveTypeCheckBox_Unchecked;
                DriveTypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка типов двигателя
            var engineTypes = App.Context.dm_Tipi_dvigatelya
                .OrderBy(t => t.Tip_dvigatelya)
                .ToList();

            EngineTypeFilterPanel.Children.Clear();
            foreach (var type in engineTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type.Tip_dvigatelya,
                    Tag = type.ID_tipa_dvigatelya,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += EngineTypeCheckBox_Checked;
                checkBox.Unchecked += EngineTypeCheckBox_Unchecked;
                EngineTypeFilterPanel.Children.Add(checkBox);
            }

            // Загрузка производственных групп
            var groups = App.Context.dm_Gruppi_avto
                .OrderBy(g => g.Nazvanie_gruppi)
                .ToList();

            GroupFilterPanel.Children.Clear();
            foreach (var group in groups)
            {
                var checkBox = new CheckBox
                {
                    Content = group.Nazvanie_gruppi,
                    Tag = group.ID_gruppi,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                checkBox.Checked += GroupCheckBox_Checked;
                checkBox.Unchecked += GroupCheckBox_Unchecked;
                GroupFilterPanel.Children.Add(checkBox);
            }

            // Загрузка стран-производителей
            var countries = App.Context.dm_Strani
                .OrderBy(c => c.Strana)
                .ToList();

            CountryFilterPanel.Children.Clear();
            foreach (var country in countries)
            {
                var checkBox = new CheckBox
                {
                    Content = country.Strana,
                    Tag = country.ID_strani,
                    Margin = new Thickness(0, 5, 0, 3)
                };
                checkBox.Checked += CountryCheckBox_Checked;
                checkBox.Unchecked += CountryCheckBox_Unchecked;
                CountryFilterPanel.Children.Add(checkBox);
            }
        }

        // Обработчики событий для чекбоксов фильтров
        // Обработчики событий для чекбоксов фильтров
        private void ManufacturerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedManufacturers.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void ManufacturerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedManufacturers.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void StartYearCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedStartYears.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void StartYearCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedStartYears.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void BodyTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedBodyTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void BodyTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedBodyTypes.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void GearboxTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGearboxTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void GearboxTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGearboxTypes.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void DriveTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedDriveTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void DriveTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedDriveTypes.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void EngineTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedEngineTypes.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void EngineTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedEngineTypes.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void GroupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGroups.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void GroupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedGroups.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void CountryCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedCountries.Add((int)checkBox.Tag);
                BtnResetFilters.IsEnabled = true;
                ApplyFilters();
            }
        }

        private void CountryCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                selectedCountries.Remove((int)checkBox.Tag);
                CheckFiltersActive();
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            currentPage = 1;
            LoadAvtomobili();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
            CheckFiltersActive();
        }


        private void CheckFiltersActive()
        {
            // Проверяем, что элементы не null перед обращением к их свойствам
            string powerFromText = PowerFromTextBox != null ? PowerFromTextBox.Text : "от";
            string powerToText = PowerToTextBox != null ? PowerToTextBox.Text : "до";

            bool hasActiveFilters = selectedManufacturers.Count > 0 ||
                          selectedStartYears.Count > 0 ||
                          selectedGroups.Count > 0 ||
                          selectedCountries.Count > 0 ||
                          selectedBodyTypes.Count > 0 ||
                          selectedGearboxTypes.Count > 0 ||
                          selectedDriveTypes.Count > 0 ||
                          selectedEngineTypes.Count > 0 ||
                          includeStillProduced ||
                          minPower.HasValue ||
                          maxPower.HasValue ||
                          (powerFromText != "от" && !string.IsNullOrWhiteSpace(powerFromText)) ||
                          (powerToText != "до" && !string.IsNullOrWhiteSpace(powerToText));

            if (BtnResetFilters != null)
            {
                BtnResetFilters.IsEnabled = hasActiveFilters;
            }
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Visible ?
                Visibility.Collapsed :
                Visibility.Visible;
        }

        private void BtnCloseFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Сброс всех фильтров
            foreach (CheckBox checkBox in ManufacturerFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in StartYearFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in BodyTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in GearboxTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in DriveTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in EngineTypeFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in GroupFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }
            foreach (CheckBox checkBox in CountryFilterPanel.Children)
            {
                checkBox.IsChecked = false;
            }

            selectedManufacturers.Clear();
            selectedStartYears.Clear();
            selectedBodyTypes.Clear();
            selectedGearboxTypes.Clear();
            selectedDriveTypes.Clear();
            selectedEngineTypes.Clear();
            selectedGroups.Clear();
            selectedCountries.Clear();
            includeStillProduced = false;

            // Сброс фильтра по мощности
            PowerFromTextBox.Text = "от";
            PowerFromTextBox.Foreground = Brushes.Gray;
            PowerToTextBox.Text = "до";
            PowerToTextBox.Foreground = Brushes.Gray;
            minPower = null;
            maxPower = null;

            BtnResetFilters.IsEnabled = false;
            ApplyFilters();
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header)
            {
                string newSortColumn;
                switch (header.Content.ToString())
                {
                    case "ID":
                        newSortColumn = "ID_avtomobilya";
                        break;
                    case "Гос. номер":
                        newSortColumn = "Gos_nomer";
                        break;
                    default:
                        newSortColumn = currentSortColumn;
                        break;
                }

                if (currentSortColumn == newSortColumn)
                {
                    isAscending = !isAscending;
                }
                else
                {
                    currentSortColumn = newSortColumn;
                    isAscending = true;
                }

                LoadAvtomobili();
                UpdatePaginationInfo();
            }
        }

        private void UpdateSortIndicators()
        {
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(AvtomobiliList))
            {
                if (header.Template.FindName("SortArrow", header) is System.Windows.Shapes.Path sortArrow)
                {
                    bool isCurrentSortColumn = false;
                    switch (header.Content.ToString())
                    {
                        case "ID":
                            isCurrentSortColumn = currentSortColumn == "ID_avtomobilya";
                            break;
                        case "Гос. номер":
                            isCurrentSortColumn = currentSortColumn == "Gos_nomer";
                            break;
                    }

                    sortArrow.Visibility = isCurrentSortColumn ? Visibility.Visible : Visibility.Collapsed;
                    if (isCurrentSortColumn)
                    {
                        sortArrow.RenderTransform = new RotateTransform(isAscending ? 0 : 180, 0.5, 0.5);
                    }
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var addEditWindow = new AddEdit_Avtomobil(_clientId);
            addEditWindow.Closed += (s, args) =>
            {
                totalItems = App.Context.dm_Avtomobili.Count();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
                LoadAvtomobili();
            };
            addEditWindow.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var currentAuto = AvtomobiliList.SelectedItem as Entities.dm_Avtomobili;

            if (currentAuto == null)
            {
                MessageBox.Show("Пожалуйста, выберите автомобиль для удаления.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isUsedInOrders = App.Context.dm_Zakazi
                .Any(z => z.Avtomobil == currentAuto.ID_avtomobilya);

            if (isUsedInOrders)
            {
                MessageBox.Show($"Невозможно удалить автомобиль (ID: {currentAuto.ID_avtomobilya}), " +
                               "так как он используется в одном или нескольких заказах.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите удалить автомобиль (ID: {currentAuto.ID_avtomobilya})?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    App.Context.dm_Avtomobili.Remove(currentAuto);
                    App.Context.SaveChanges();

                    int totalItemsAfterDelete = App.Context.dm_Avtomobili.Count();
                    int maxPage = (int)Math.Ceiling((double)totalItemsAfterDelete / itemsPerPage);

                    if (currentPage > maxPage && maxPage > 0)
                    {
                        currentPage = maxPage;
                    }
                    else if (maxPage == 0)
                    {
                        currentPage = 1;
                    }

                    LoadAvtomobili();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();

                    MessageBox.Show("Автомобиль успешно удален!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении автомобиля:\n{ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAvtomobili();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedAuto = (sender as Button).DataContext as Entities.dm_Avtomobili;

            if (selectedAuto == null)
            {
                MessageBox.Show("Выберите автомобиль для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var autoToEdit = App.Context.dm_Avtomobili
                .FirstOrDefault(a => a.ID_avtomobilya == selectedAuto.ID_avtomobilya);

            if (autoToEdit != null)
            {
                var addEditWindow = new AddEdit_Avtomobil(autoToEdit);
                bool? result = addEditWindow.ShowDialog();

                if (result == true)
                {
                    LoadAvtomobili();
                }
            }
            else
            {
                MessageBox.Show("Автомобиль не найден в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime lastPopupInteractionTime = DateTime.MinValue;

        private void ItemsPerPageButton_Click(object sender, RoutedEventArgs e)
        {
            if ((DateTime.Now - lastPopupInteractionTime).TotalMilliseconds < 200)
                return;

            lastPopupInteractionTime = DateTime.Now;

            var button = sender as Button;

            if (itemsPerPagePopup != null && itemsPerPagePopup.IsOpen && currentButton == button)
            {
                itemsPerPagePopup.IsOpen = false;
                return;
            }

            if (itemsPerPagePopup != null)
            {
                itemsPerPagePopup.IsOpen = false;
            }

            currentButton = button;

            itemsPerPagePopup = new Popup
            {
                PlacementTarget = currentButton,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                Width = currentButton.ActualWidth,
                IsOpen = true
            };

            itemsPerPagePopup.Closed += (s, args) =>
            {
                lastPopupInteractionTime = DateTime.Now;
            };

            var stackPanel = new StackPanel { Background = Brushes.White };

            int[] items = { 20, 50, 100, 200 };
            foreach (var item in items)
            {
                var btn = new Button
                {
                    Content = item.ToString(),
                    Style = (Style)FindResource("ComboBoxItemStyle"),
                    Tag = item
                };
                btn.Click += (s, args) =>
                {
                    itemsPerPage = (int)((Button)s).Tag;
                    currentButton.Content = itemsPerPage.ToString();
                    currentPage = 1;
                    itemsPerPagePopup.IsOpen = false;
                    LoadAvtomobili();
                    UpdatePaginationInfo();
                    UpdatePaginationButtons();
                };
                stackPanel.Children.Add(btn);
            }

            itemsPerPagePopup.Child = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Child = stackPanel
            };
        }

        private void UpdatePaginationInfo()
        {
            try
            {
                int start = (currentPage - 1) * itemsPerPage + 1;
                int end = Math.Min(currentPage * itemsPerPage, totalItems);

                PaginationInfo.Text = $"{start}-{end} из {totalItems}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения информации о пагинации: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "Поиск...")
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
                textBox.Text = "Поиск...";
                textBox.Foreground = Brushes.Gray;
                SearchTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            }
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;

            // Проверка клика вне панели фильтров
            bool clickInsideFilterPanel = false;
            while (originalSource != null)
            {
                if (originalSource == FilterPanel || originalSource == BtnFilter)
                {
                    clickInsideFilterPanel = true;
                    break;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (!clickInsideFilterPanel && FilterPanel.Visibility == Visibility.Visible)
            {
                FilterPanel.Visibility = Visibility.Collapsed;
            }

            // Остальная логика обработки клика
            bool clickInsideSearchBox = false;
            originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null)
            {
                if (originalSource == SearchTextBox)
                {
                    clickInsideSearchBox = true;
                    break;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (!clickInsideSearchBox)
            {
                if (SearchTextBox.IsFocused)
                {
                    Keyboard.ClearFocus();

                    if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                    {
                        SearchTextBox.Text = "Поиск...";
                        SearchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                        SearchTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    }
                }
            }
            else
            {
                if (SearchTextBox.Text == "Поиск...")
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
                    SearchTextBox.BorderBrush = new SolidColorBrush(Colors.Blue);
                }
            }

            var clickedElement = e.OriginalSource as DependencyObject;
            bool isClickInside = false;

            while (clickedElement != null)
            {
                if (clickedElement == currentButton || clickedElement == itemsPerPagePopup)
                {
                    isClickInside = true;
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            if (!isClickInside && itemsPerPagePopup != null && itemsPerPagePopup.IsOpen)
            {
                itemsPerPagePopup.IsOpen = false;
            }
        }

        private void UpdatePaginationButtons()
        {
            if (currentPage > 1)
            {
                BtnPrevPage.Style = (Style)FindResource("PaginationButtonStyle");
                BtnPrevPage.IsEnabled = true;
            }
            else
            {
                BtnPrevPage.Style = (Style)FindResource("PaginationButtonDisabledStyle");
                BtnPrevPage.IsEnabled = false;
            }

            if (currentPage < (int)Math.Ceiling((double)totalItems / itemsPerPage))
            {
                BtnNextPage.Style = (Style)FindResource("PaginationButtonStyle");
                BtnNextPage.IsEnabled = true;
            }
            else
            {
                BtnNextPage.Style = (Style)FindResource("PaginationButtonDisabledStyle");
                BtnNextPage.IsEnabled = false;
            }
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadAvtomobili();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadAvtomobili();
                UpdatePaginationInfo();
                UpdatePaginationButtons();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox.Text == "Поиск...")
            {
                currentSearchText = "";
                return;
            }

            currentSearchText = textBox.Text;
            currentPage = 1;
            LoadAvtomobili();
            UpdatePaginationInfo();
            UpdatePaginationButtons();
        }

        // Обработчики для фильтрации по мощности
        private void PowerFilterTimer_Tick(object sender, EventArgs e)
        {
            powerFilterTimer.Stop();
            UpdatePowerFilterValues();
            ApplyFilters();
        }

        private void UpdatePowerFilterValues()
        {
            // Защита от NullReferenceException
            if (PowerFromTextBox == null || PowerToTextBox == null)
                return;

            var fromText = PowerFromTextBox.Text;
            var toText = PowerToTextBox.Text;

            // Обработка минимальной мощности
            if (fromText != "от" && !string.IsNullOrWhiteSpace(fromText))
            {
                if (int.TryParse(fromText, out int value))
                    minPower = value;
                else
                    minPower = null;
            }
            else
            {
                minPower = null;
            }

            // Обработка максимальной мощности
            if (toText != "до" && !string.IsNullOrWhiteSpace(toText))
            {
                if (int.TryParse(toText, out int value))
                    maxPower = value;
                else
                    maxPower = null;
            }
            else
            {
                maxPower = null;
            }

            CheckFiltersActive();
        }

        private void PowerTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void PowerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = textBox.Tag.ToString();
                    textBox.Foreground = Brushes.Gray;
                }
                UpdatePowerFilterValues();
                ApplyFilters();
            }
        }

        private void PowerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (powerFilterTimer != null)
            {
                powerFilterTimer.Stop();
                powerFilterTimer.Start();
            }

            CheckFiltersActive();
        }

        private void PowerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private double[] columnRatios = new double[] { 0.05, 0.2, 0.15, 0.15, 0.15, 0.2, 0.1 };

        private void UpdateGridViewColumnWidths()
        {
            if (AvtomobiliList.View is GridView gridView)
            {
                double totalWidth = AvtomobiliList.ActualWidth;
                double availableWidth = totalWidth - 10;

                if (availableWidth <= 0)
                    return;

                int columnCount = gridView.Columns.Count;

                if (columnRatios.Length != columnCount)
                {
                    columnRatios = Enumerable.Repeat(1.0 / columnCount, columnCount).ToArray();
                }

                double sumRatios = columnRatios.Sum();

                for (int i = 0; i < columnCount; i++)
                {
                    gridView.Columns[i].Width = availableWidth * (columnRatios[i] / sumRatios);
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
