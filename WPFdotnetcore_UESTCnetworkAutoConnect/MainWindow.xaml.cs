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
using libCheckAndReconnectUESTC;
using localStorageConfiguration;
using libMantainNetwork;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace WPFdotnetcore_UESTCnetworkAutoConnect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LoginConfiguration loginConfig;
        private CancellationTokenSource mantainTaskCancelToken;
        private FileSystemWatcher logFileWatcher;
        private static long perviousLogFileSize;
        public LogBlockTextValue logBlockTextValue;

        public System.Windows.Forms.NotifyIcon notifyIcon = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotifyIcon();
            InitializeLoginConfiguration();
            InitializeLogConfiguration();
            if(loginConfig.autoSetup==true)
            {
                button_keepConnect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Visible = false;
            notifyIcon.Icon = new System.Drawing.Icon("icon-256.ico");
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            if(this.WindowState==WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Activate();
        }


        private void InitializeLogConfiguration()
        {
            logFileWatcher = new FileSystemWatcher();
            logFileWatcher.Path = System.IO.Directory.GetCurrentDirectory();
            logFileWatcher.Filter = MantainNet.logFileName;
            logFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            logFileWatcher.Changed += new FileSystemEventHandler(OnLogFileChanged);
            
            logBlockTextValue = new LogBlockTextValue();
            logTextblock.DataContext = logBlockTextValue;
        }

        private void OnLogFileChanged(object sender, FileSystemEventArgs e)
        {
            if (System.IO.File.Exists(e.FullPath))
            {                
                using (FileStream fs = File.Open(MantainNet.logFileName,FileMode.Open,FileAccess.Read,FileShare.Write))
                {
                    fs.Seek(perviousLogFileSize, SeekOrigin.Begin);
                    StringBuilder buffer = new StringBuilder();
                    int c;
                    while ((c = fs.ReadByte())!=-1) 
                    { 
                        buffer.Append(Convert.ToChar(c));
                    }
                    logBlockTextValue.logData += buffer.ToString();
                    perviousLogFileSize = fs.Length;
                }
            }
        }

        private void InitializeLoginConfiguration()
        {
            LoginConfigurationHandler loginConfigurationHandler = new LoginConfigurationHandler();
            loginConfig = loginConfigurationHandler.Load();
            inputBox_loginSite.Text = loginConfig.loginSiteString;
            inputBox_username.Text = loginConfig.usernameString;
            inputPasswordBox_Password.Password = loginConfig.passwordString;
            checkBox_startUp.IsChecked = loginConfig.autoSetup;
            
        }

        private async void button_keepConnect_Click(object sender, RoutedEventArgs e)
        {
            LoginConfigurationHandler loginConfigurationHandler = new LoginConfigurationHandler();
            loginConfig.loginSiteString = inputBox_loginSite.Text;
            loginConfig.usernameString = inputBox_username.Text;
            loginConfig.passwordString = inputPasswordBox_Password.Password;
            loginConfig.autoSetup = checkBox_startUp.IsChecked ?? false;
            loginConfigurationHandler.Save(loginConfig);

            logBlockTextValue.logData = "";
            mantainTaskCancelToken = new CancellationTokenSource();
            perviousLogFileSize = 0;
            logFileWatcher.EnableRaisingEvents = true;
            Task.Run(() =>
            {
                MantainNet.MonitorAndReconnect(loginConfig, mantainTaskCancelToken.Token);
            });
        }

        private void checkBox_startUp_Click(object sender, RoutedEventArgs e)
        {
            loginConfig.autoSetup = checkBox_startUp.IsChecked ?? false;
            LoginConfigurationHandler loginConfigurationHandler = new LoginConfigurationHandler();
            loginConfigurationHandler.Save(loginConfig);
            if(checkBox_startUp.IsChecked==true)
            {
                string applicationLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.SetValue("UESTCnetworkAutoConnect", applicationLocation);
            }
            else
            {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.DeleteValue("UESTCnetworkAutoConnect", false);
            }
        }

        private void button_stop_Click(object sender, RoutedEventArgs e)
        {
            if (mantainTaskCancelToken != null)
            {
                mantainTaskCancelToken.Cancel();
            }
            //logFileWatcher.EnableRaisingEvents = false;
        }
        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                notifyIcon.Visible = false;
                this.Show();
            }
            if (this.WindowState == WindowState.Minimized)
            {
                notifyIcon.Visible = true;
                this.Hide();
            }
            base.OnStateChanged(e);
        }

        public class LogBlockTextValue : INotifyPropertyChanged
        {
            private string _logData;
            public string logData
            {
                get { return _logData; }
                set
                {
                    _logData = value;
                    OnPropertyChanged();
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            {
                
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void logTextblockScroll_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            scrollViewer.ScrollToEnd();
            
        }
    }
}
