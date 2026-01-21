using Labb3_DB.Commands;
using Labb3_DB.Data;
using Labb3_DB.Models;
using Labb3_DB.Mongo;
using Labb3_DB.ViewModels;
using Labb3_DB.Views;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                OnPropertyChanged(nameof(_username));
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


            }

        private async Task SignUp()
            {
            Debug.WriteLine($"{_username} = {_username.Length} {Password} = {PasswordLength} ");
            if (_username.Length < 3 || PasswordLength < 6)
                {
                MessageBox.Show("Username must be at least 3 characters and password at least 6 characters long.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
                }

            await _dbService.CreateUserAsync(_username, Password);
            }
        private async Task Login()
            {
            Debug.WriteLine($"{_username} = {_username.Length} {Password} = {PasswordLength} ");
            if (_username.Length < 3 || PasswordLength < 6)
                {
                MessageBox.Show("Username must be at least 3 characters and password at least 6 characters long.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
                }

            var user = await _dbService.GetUserAsync(_username, Password);
            if (user == null)
                {
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
                }

            // Proceed with login
            }
        }
    }
