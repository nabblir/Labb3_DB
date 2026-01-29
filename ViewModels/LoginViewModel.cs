using Labb3_DB.Commands;
using Labb3_DB.Data;
using Labb3_DB.Models;
using Labb3_DB.Mongo;
using Labb3_DB.ViewModels;
using Labb3_DB.Views;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Labb3_DB.ViewModels
    {
    public class LoginViewModel : BaseViewModel
        {
        private readonly DatabaseService _dbService;
        private string _username;
        public string Username
            {
            get { return _username; }
            set
                {
                _username = value;
                OnPropertyChanged(nameof(Username));
                }
            }
        private string _password;
        public string Password
            {
            get { return _password; }
            set
                {
                _password = value;
                OnPropertyChanged(nameof(Password));
                }
            }
        private int _passwordLength;
        public int PasswordLength
            {
            get => _passwordLength;
            set
                {
                _passwordLength = value;
                OnPropertyChanged(nameof(PasswordLength));
                }
            }
        private string _statusMessage;
        public string StatusMessage
            {
            get { return _statusMessage; }
            set
                {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
                }
            }
        private System.Windows.Media.Brush _statusTextColor;
        public System.Windows.Media.Brush StatusTextColor
            {
            get { return _statusTextColor; }
            set
                {
                _statusTextColor = value;
                OnPropertyChanged(nameof(StatusTextColor));
                }
            }
        private bool _isLoading;
        public bool IsLoading
            {
            get => _isLoading;
            set
                {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                }
            }

        private bool _userRemembered = false;
        public bool UserRemembered
            {
            get => _userRemembered;
            set
                {
                _userRemembered = value;
                OnPropertyChanged(nameof(UserRemembered));
                }
            }
        public ICommand RememberMeCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand SignupCommand { get; }
        public ICommand LoginCommand { get; }
        public LoginViewModel()
            {
            _dbService = new DatabaseService();
            _username = string.Empty;
            _password = string.Empty;
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            SignupCommand = new RelayCommand(async (_) => await SignUp());
            LoginCommand = new RelayCommand(async (_) => await Login());
            RememberMeCommand = new RelayCommand(async (_) => await UpdateRememberMe());
            _ = TestConnection();
            }

        private async Task SignUp()
            {
            if (_username.Length < 3 || PasswordLength < 6)
                {
                StatusMessage = "Username must be at least 3 characters and password at least 6 characters long.";
                StatusTextColor = System.Windows.Media.Brushes.Red;
                return;
                }

            var (success, message) = await _dbService.CreateUserAsync(_username, Password);
            StatusMessage = message;

            if (success)
                {
                StatusTextColor = System.Windows.Media.Brushes.Green;
                await Task.Delay(1000); // Brief pause to show success message, cause i like information!
                await Login();
                }
            else
                {
                StatusTextColor = System.Windows.Media.Brushes.Red;
                }
            }
        private async Task IsUserRememberedAsync()
            {
            var settings = await _dbService.GetApplicationSettingsAsync();
            if (settings != null)
                {
                var user = await _dbService.GetUserByIdAsync(settings.UserID);
                UserRemembered = settings.IsRemembered;

                if (user != null)
                    {
                    if (settings.IsRemembered)
                        {
                        Username = user.Username;
                        }
                    else
                        {
                        Username = string.Empty;
                        }
                    }
                }
            }

        private async Task UpdateRememberMe()
            {
            var settings = await _dbService.GetApplicationSettingsAsync();

            if (settings != null)
                {
                settings.IsRemembered = UserRemembered;
                var user = await _dbService.GetUserAsync(Username, Password);

                if (user != null)
                    {
                        settings.UserID = user.UserId;
                    }
                if (UserRemembered == false)
                    {
                        settings.UserID = string.Empty;
                    }

                await _dbService.UpdateApplicationSettingsAsync(settings);
                Debug.WriteLine($"Remember Me settings updated. + {settings}");
                }

            }

        private async Task Login()
            {

            IsLoading = true;
            StatusMessage = "Logging in...";
            StatusTextColor = System.Windows.Media.Brushes.Blue;

            try
                {
                var user = await _dbService.GetUserAsync(Username, Password);
                if (user == null)
                    {
                    StatusMessage = "Invalid username or password.";
                    StatusTextColor = System.Windows.Media.Brushes.Red;
                    return;
                    }

                // Initialize database and buildings, if needed
                await _dbService.InitializeDatabaseAsync(user);
                await _dbService.InitializeBuildingsAsync();

                // Refresh user data to get any newly created kingdoms
                user = await _dbService.GetUserByIdAsync(user.UserId);

                StatusMessage = "Login successful!";
                StatusTextColor = System.Windows.Media.Brushes.Green;

                // Finally, update remember me settings and proceed
                await UpdateRememberMe();

                // Small delay to show success message, cause i be damned if i dont show my fancy message system!
                await Task.Delay(1000);
                ShowKingdomSelectionAndOpenMainWindow(user);
                }
            catch (Exception ex)
                {
                StatusMessage = $"Login failed: {ex.Message}";
                StatusTextColor = System.Windows.Media.Brushes.Red;
                }
            finally
                {
                IsLoading = false;
                }
            }

        private void ShowKingdomSelectionAndOpenMainWindow(User user)
            {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                var selectionViewModel = new KingdomSelectionViewModel(user);
                var selectionDialog = new KingdomSelectionDialog
                    {
                    DataContext = selectionViewModel
                    };

                // Show dialog using MaterialDesignThemes DialogHost
                await DialogHost.Show(selectionDialog, "RootDialog");


                if (selectionViewModel.Result != null || selectionViewModel.ShouldCreateNew)
                    {
                    OpenMainWindow(user, selectionViewModel.Result, selectionViewModel.ShouldCreateNew);
                    }
            });
            }

        private async Task TestConnection()
            {
            string isConnected = await _dbService.EstablishConnectionAsync(); // I know.. should use bools, but this will return error messages too.
            if (isConnected == "Connection to MongoDB successful!") 
                {
                await IsUserRememberedAsync();
                StatusTextColor = System.Windows.Media.Brushes.Green;
                StatusMessage = isConnected;

                }
            else
                {
                StatusMessage = isConnected;
                StatusTextColor =  System.Windows.Media.Brushes.Red;
                }
            }

        private void OpenMainWindow(User user, Kingdom? selectedKingdom, bool shouldCreateNew)
            {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = new MainWindow(user, selectedKingdom, shouldCreateNew);
                mainWindow.Show();

                // Close the login window
                foreach (Window window in Application.Current.Windows)
                    {
                    if (window is LoginWindow)
                        {
                        window.Close();
                        break;
                        }
                    }
            });
            }
        }
    }
