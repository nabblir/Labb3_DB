using Labb3_DB.Models;
using Labb3_DB.Mongo;
using Labb3_DB.ViewModels;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace Labb3_DB
    {
    public partial class MainWindow : Window
        {
        public MainWindow()
            {
            InitializeComponent();
            DataContext = new MainViewModel(); 
            }
        }
    }