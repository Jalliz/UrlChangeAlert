using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace UrlChangeAlert
{
    public class Difference : INotifyPropertyChanged
    {
        private string _selectedValue;
        private string _value;
        private bool _ignored;
        private Page _parent;
        private ICommand _ignoreSelectedCommand;

        public ICommand IgnoreSelectedCommand
        {
            get { return _ignoreSelectedCommand; }
        }

        public string SelectedValue
        {
            get { return _selectedValue; }
            set
            {
                _selectedValue = value;
                NotifyPropertyChanged();
            }
        }

        public Page Parent
        {
            get { return _parent; }
        }

        public bool Ignored
        {
            get { return _ignored; }
            set { _ignored = value; }
        }

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public Difference(string value, Page parent)
        {
            SelectedValue = "";
            _value = value;
            _parent = parent;
            _ignoreSelectedCommand = new RelayCommand(param => AddIgnoreSelected(), emnu => SelectedValue != "" );
        }

     
        private void AddIgnoreSelected()
        {
            if (!_parent.IgnorePart.Contains(SelectedValue))
                _parent.IgnorePart.Add(SelectedValue);

            Ignored = true;
            Parent.NotifyPropertyChanged("Difference");

            if (Parent.Difference.Count == 0)
            {
                Parent.Start();
                Parent.MainWindowViewModel.UpdateXml();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
