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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для ProfilPage.xaml
    /// </summary>
    public partial class ProfilPage : Page
    {
        private string _selectedImagePath;
        private string _originalPhotoPath;

        private Entities.dm_Users _currentUser;

        public ProfilPage(Entities.dm_Users currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            LoadUserData();
        }

        public void LoadUserData()
        {
            try
            {
                var user = App.Context.dm_Users.Include("dm_Roli").FirstOrDefault(u => u.ID_user == App.CurrentUser.ID_user);
                TextBlockFIO.Text = $"{user.Familiya} {user.Imya} {user.Otchestvo}";
                TextBlockRol.Text = $"{user.dm_Roli.Rol}";
                TextBlockLogin.Text = $"{user.Login}";
                TextBlockData_rojdeniya.Text = user.Data_rojdeniya.ToString("dd.MM.yyyy");
                _originalPhotoPath = user.Foto;

                // Загрузка фото
                if (!string.IsNullOrEmpty(user.Foto))
                {
                    UserPhoto.Source = LoadImage(user.Foto);
                    PhotoPlaceholder.Visibility = Visibility.Collapsed; // Это теперь скроет и синий фон
                }
                else
                {
                    UserPhoto.Source = null;
                    PhotoPlaceholder.Visibility = Visibility.Visible;
                    PhotoPlaceholder.Text = GetUserInitials(user.Familiya, user.Imya);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage LoadImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            string imagePath = GetImagePath(imageName);

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

        private string GetImagePath(string imageName)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", ".."));
            return System.IO.Path.Combine(projectDirectory, "Users", imageName);
        }

        private string GetUserInitials(string surname, string name)
        {
            string initials = "";
            if (!string.IsNullOrEmpty(surname))
                initials += surname[0];
            if (!string.IsNullOrEmpty(name))
                initials += name[0];
            return initials.Length > 0 ? initials : "?";
        }

        private void ButtonSelectImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                Title = "Выберите фото профиля"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _selectedImagePath = openFileDialog.FileName;
                    UserPhoto.Source = new BitmapImage(new Uri(_selectedImagePath));
                    PhotoPlaceholder.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
                }
            }
        }

        public event EventHandler PhotoUpdated;

        private void ButtonSohr_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = PasswordBoxCurrentPasswordBox.Password;
            string newPassword = PasswordBoxNewPasswordBox.Password;
            string confirmNewPassword = PasswordBoxRepeatPasswordBox.Password;

            try
            {
                var user = App.Context.dm_Users.Find(_currentUser.ID_user);
                bool passwordChanged = false;
                bool photoChanged = false;

                // Проверка полей пароля
                bool anyPasswordFieldFilled = !string.IsNullOrEmpty(currentPassword) ||
                                            !string.IsNullOrEmpty(newPassword) ||
                                            !string.IsNullOrEmpty(confirmNewPassword);

                bool allPasswordFieldsFilled = !string.IsNullOrEmpty(currentPassword) &&
                                             !string.IsNullOrEmpty(newPassword) &&
                                             !string.IsNullOrEmpty(confirmNewPassword);

                if (anyPasswordFieldFilled && !allPasswordFieldsFilled)
                {
                    MessageBox.Show("Для изменения пароля заполните все обязательные поля!",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    return;
                }

                // Обновление пароля (если все поля заполнены)
                if (allPasswordFieldsFilled)
                {
                    if (currentPassword != user.Password)
                    {
                        MessageBox.Show("Текущий пароль введен неверно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (newPassword == user.Password)
                    {
                        MessageBox.Show("Новый пароль не должен совпадать с текущим!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (newPassword != confirmNewPassword)
                    {
                        MessageBox.Show("Новый пароль и повтор нового пароля не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    user.Password = newPassword;
                    passwordChanged = true;
                }

                // Обновление фото
                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    string usersFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(GetImagePath("")), "Users");
                    bool isAlreadyInUsersFolder = _selectedImagePath.StartsWith(usersFolder, StringComparison.OrdinalIgnoreCase);

                    if (isAlreadyInUsersFolder)
                    {
                        // Фото уже в папке Users - просто сохраняем имя файла
                        user.Foto = System.IO.Path.GetFileName(_selectedImagePath);
                    }
                    else
                    {
                        // Фото не в папке Users - копируем с оригинальным именем
                        if (!Directory.Exists(usersFolder))
                            Directory.CreateDirectory(usersFolder);

                        string fileName = System.IO.Path.GetFileName(_selectedImagePath);
                        string newPhotoPath = System.IO.Path.Combine(usersFolder, fileName);

                        // Проверяем, не существует ли файл с таким именем
                        if (File.Exists(newPhotoPath))
                        {
                            // Если файл существует, добавляем к имени временную метку
                            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                            string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                            string fileExt = System.IO.Path.GetExtension(fileName);
                            fileName = $"{fileNameWithoutExt}_{timestamp}{fileExt}";
                            newPhotoPath = System.IO.Path.Combine(usersFolder, fileName);
                        }

                        File.Copy(_selectedImagePath, newPhotoPath);
                        user.Foto = fileName;
                    }
                    photoChanged = true;
                }

                if (passwordChanged || photoChanged)
                {
                    App.Context.SaveChanges();
                    PhotoUpdated?.Invoke(this, EventArgs.Empty);
                    MessageBox.Show("Изменения успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (passwordChanged)
                    {
                        PasswordBoxCurrentPasswordBox.Clear();
                        PasswordBoxNewPasswordBox.Clear();
                        PasswordBoxRepeatPasswordBox.Clear();
                    }
                    _selectedImagePath = null;
                }
                else
                {
                    MessageBox.Show("Нет изменений для сохранения", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
