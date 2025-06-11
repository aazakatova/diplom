using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Avtoservis
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Entities.user80_dbEntities Context { get; } = new Entities.user80_dbEntities();
        public static Entities.dm_Users CurrentUser = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Отключаем панель инструментов отладки
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
            base.OnStartup(e);
        }


        private static Dictionary<string, int> _loginAttempts = new Dictionary<string, int>();
        private static Dictionary<string, int> _captchaAttempts = new Dictionary<string, int>();
        private static bool _isCaptchaShown = false;
        private static DateTime? _blockTime = null;

        public static void AddLoginAttempt(string login, bool isUserExists)
        {
            if (!isUserExists) return;

            if (_loginAttempts.ContainsKey(login))
                _loginAttempts[login]++;
            else
                _loginAttempts[login] = 1;
        }

        public static bool ShouldShowCaptcha(string login)
        {
            return _loginAttempts.ContainsKey(login) && _loginAttempts[login] >= 3 && !_isCaptchaShown;
        }

        public static int GetLoginAttempts(string login)
        {
            return _loginAttempts.ContainsKey(login) ? _loginAttempts[login] : 0;
        }

        public static void ShowCaptcha()
        {
            _isCaptchaShown = true;
        }

        public static void ResetCaptcha(string login)
        {
            _isCaptchaShown = false;
            if (_loginAttempts.ContainsKey(login))
                _loginAttempts[login] = 0;
        }

        public static void BlockAccess()
        {
            _blockTime = DateTime.Now.AddSeconds(10);
        }

        public static bool IsBlocked()
        {
            return _blockTime.HasValue && DateTime.Now < _blockTime;
        }

        public static TimeSpan GetRemainingBlockTime()
        {
            return _blockTime.HasValue ? _blockTime.Value - DateTime.Now : TimeSpan.Zero;
        }
    }
}
