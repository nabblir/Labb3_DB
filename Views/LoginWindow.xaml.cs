using Labb3_DB.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Labb3_DB.Views
    {
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
        {
        public LoginWindow()
            {
            InitializeComponent();
            DataContext = new LoginViewModel();
            }
        private void PasswordChanged(object sender, RoutedEventArgs e)
            {
            if (DataContext is LoginViewModel vm)
                {
                vm.PasswordLength = Password.Password.Length;
                vm.Password = Password.Password;  // Set the actual password string
                Debug.WriteLine($"{vm.Username} = {vm.Username?.Length ?? 0} Password = {vm.Password?.Length ?? 0}");
                }
            }
        }
    }
