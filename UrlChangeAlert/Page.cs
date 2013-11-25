using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using System.Net;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;

using System.Windows.Controls;

namespace UrlChangeAlert
{
    public class Page : INotifyPropertyChanged
    {
        private string _url;
        private ICommand _startCommand;
        private ICommand _openUrlCommand;
        private ICommand _removeCommand;
        private ICommand _refreshCommand;

        //private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        private Timer _dispatcherTimer = new Timer();
        private List<Difference> _difference;
        private Dictionary<string, int> _allWords;
        private HashSet<string> _ignorePart;
        private MainWindowViewModel _mainWindowViewModel;
        private DateTime _startTime;
        private string _status;

        public MainWindowViewModel MainWindowViewModel
        {
            get { return _mainWindowViewModel; }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged();
            }
        }

        public string Url
        {
            get { return _url; }
        }

        public HashSet<string> IgnorePart
        {
            get { return _ignorePart; }
            set { _ignorePart = value; }
        }

        public ObservableCollection<Difference> Difference
        {
            get { return new ObservableCollection<Difference>(_difference.Where(d => !d.Ignored)); }
        }

        public Visibility RefreshVisibility
        {
            get { return Enabled ? Visibility.Visible : Visibility.Collapsed; }
        }

        public ICommand RemoveCommand
        {
            get { return _removeCommand; }
        }

        public ICommand OpenUrlCommand
        {
            get { return _openUrlCommand; }
        }
        public ICommand StartCommand
        {
            get { return _startCommand; }
        }

        public ICommand RefreshCommand
        {
            get { return _refreshCommand; }
        }

        public bool Enabled
        {
            get { return _dispatcherTimer.Enabled; }
        }

        public string IconStatus
        {
            get { return _dispatcherTimer.Enabled ? @"Icons\Tick.png" : @"Icons\Alert.png"; }
        }

        public double Interval
        {
            get { return _dispatcherTimer.Interval; }
            set { _dispatcherTimer.Interval = value; }
        }

        public double TimeLeft
        {
            get 
            {
                if (Enabled)
                {
                    DateTime endTime = new DateTime(_startTime.Year, _startTime.Month, _startTime.Day, _startTime.Hour, _startTime.Minute, _startTime.Second);
                    endTime = endTime.AddMilliseconds(Interval);
                    return (endTime - DateTime.Now).TotalSeconds + 1;
                }
                else
                    return 0;
            }
        }

        public Page(string url, MainWindowViewModel mainWindowViewModel)
        {
            _difference = new List<Difference>();
            _allWords = new Dictionary<string, int>();
            _ignorePart = new HashSet<string>();
            _url = url;
            _mainWindowViewModel = mainWindowViewModel;

            _dispatcherTimer.Interval = 10;
            _dispatcherTimer.Elapsed += _dispatcherTimer_Elapsed;
            _dispatcherTimer.Start();
            _startTime = DateTime.Now;

            _openUrlCommand = new RelayCommand(emnu => OpenUrl());
            _startCommand = new RelayCommand(emnu => Start());
            _removeCommand = new RelayCommand(emnu => Remove());
            _refreshCommand = new RelayCommand(emnu => Refresh());
        }

        public void Refresh()
        {
            _dispatcherTimer.Enabled = false;
            _dispatcherTimer.Interval = 1;
            _dispatcherTimer.Enabled = true;
        }

        void _dispatcherTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_dispatcherTimer.Interval != 60000)
                _dispatcherTimer.Interval = 60000;
            GetAndCheckHtml();
            _startTime = DateTime.Now;
        }

        public void Remove()
        {
            _dispatcherTimer.Stop();
            _mainWindowViewModel.RemovePage(this);
            _mainWindowViewModel.UpdateXml();
        }


        public void Start()
        {
            if (!_dispatcherTimer.Enabled)
            {
                Status = "";
                _startTime = DateTime.Now;
                _mainWindowViewModel.PlayAlert = false;
                _dispatcherTimer.Start();
                NotifyPropertyChanged("IconStatus");

                foreach (Difference difference in Difference.Where(d => d.Ignored == false))
                {
                    difference.Ignored = true;
                }

                NotifyPropertyChanged("Difference");
            }

            NotifyPropertyChanged("RefreshVisibility");
        }

        void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Status = "";
            GetAndCheckHtml();
        }

        private void OpenUrl()
        {
            System.Diagnostics.Process.Start(Url);
        }

        public void GetAndCheckHtml()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string newHtml = client.DownloadString(Url);
                    //string stripHtml = Regex.Replace(newHtml, "<.*?>", string.Empty);//.Replace(Environment.NewLine, "").Replace("\n", "");
                    //string stipMess = Regex.Replace(stripHtml, @"[^a-zA-Z0-9\. -]", string.Empty);

                    bool first = _allWords.Count == 0 ? true : false;
                    bool changed = false;

                    string[] splitWords = new string[] { "\n" };

                    foreach (string word in newHtml.Replace(Environment.NewLine, "\n").Split(splitWords, StringSplitOptions.RemoveEmptyEntries).Distinct().Select(s => s.ToLower().Trim()).Where(w => !_allWords.Keys.Contains(w)))
                    {
                        if (!first && IgnorePart.Where(i => word.Contains(i)).Count() == 0)
                        {
                            _difference.Add(new Difference(word, this));
                            changed = true;
                        }

                        _allWords.Add(word, 0);
                    }

                    if (changed)
                    {
                        _dispatcherTimer.Stop();
                        _mainWindowViewModel.PlayAlert = true;
                    }

                }
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }

            NotifyPropertyChanged("IconStatus");
            NotifyPropertyChanged("Difference");
            NotifyPropertyChanged("RefreshVisibility");
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
