using Labb3_DB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb3_DB.ViewModels
{
    public class BuildingViewModel : BaseViewModel
        {
        private readonly Building _model;

        public string Name => _model.Name;
        public string Description => _model.Description;
        public double CurrentCost => _model.CurrentCost;

        public string BuildingType => _model.BuildingType;


        private int _count;
        public int Count
            {
            get => _count;
            set
                {
                if (SetProperty(ref _count, value))
                    {
                    _model.Count = value;
                    OnPropertyChanged(nameof(CurrentCost)); // TODO: Uppdatera även cost
                    }
                }
            }

        public Building Model => _model;

        public BuildingViewModel(Building model)
            {
            _model = model;
            _count = model.Count;
            }
        }
    }
