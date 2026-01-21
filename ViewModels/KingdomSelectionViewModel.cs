using Labb3_DB.Commands;
using Labb3_DB.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Labb3_DB.ViewModels
    {
    public class KingdomSelectionViewModel : BaseViewModel
        {
        private readonly User _currentUser;

        public ObservableCollection<Kingdom> SavedKingdoms { get; set; }

        private Kingdom? _selectedKingdom;
        public Kingdom? SelectedKingdom
            {
            get => _selectedKingdom;
            set => SetProperty(ref _selectedKingdom, value);
            }

        public Kingdom? Result { get; private set; }
        public bool ShouldCreateNew { get; private set; }

        public ICommand SelectKingdomCommand { get; }
        public ICommand CreateNewCommand { get; }
        public ICommand CancelCommand { get; }

        public KingdomSelectionViewModel(User user)
            {
            _currentUser = user;
            SavedKingdoms = new ObservableCollection<Kingdom>(user.SavedKingdoms);

            // Accept the kingdom parameter from the button
            SelectKingdomCommand = new RelayCommand(param => SelectKingdom(param as Kingdom));
            CreateNewCommand = new RelayCommand(_ => CreateNew());
            CancelCommand = new RelayCommand(_ => Cancel());
            }

        private void SelectKingdom(Kingdom? kingdom)
            {
            if (kingdom == null)
                return;

            Result = kingdom;
            ShouldCreateNew = false;
            CloseDialog();
            }

        private void CreateNew()
            {
            Result = null;
            ShouldCreateNew = true;
            CloseDialog();
            }

        private void Cancel()
            {
            Result = null;
            ShouldCreateNew = false;
            CloseDialog();
            }

        private void CloseDialog()
            {
            // Close the MaterialDesign DialogHost
            DialogHost.CloseDialogCommand.Execute(null, null);
            }
        }
    }