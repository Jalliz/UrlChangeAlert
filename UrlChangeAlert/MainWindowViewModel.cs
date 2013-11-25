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
using System.Windows.Threading;

using System.Xml.Linq;

namespace UrlChangeAlert
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private ICommand _stopAlertCommand;
        private ICommand _addUrlCommand;
        private List<Page> _pages = new List<Page>();
        private string _addUrlValue;
        private MediaElement _mediaElement = new System.Windows.Controls.MediaElement();
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        private bool _playAlert;
        private bool _playingAlert;

        public string AddUrlValue
        {
            get { return _addUrlValue; }
            set 
            { 
                _addUrlValue = value;
                NotifyPropertyChanged();
            }
        }

        public ICommand StopAlertCommand
        {
            get { return _stopAlertCommand; }
        }

        public ICommand AddUrlCommand
        {
            get { return _addUrlCommand; }
        }
        
        public bool PlayAlert
        {
            get { return _playAlert; }
            set
            {
                _playAlert = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<Page> Pages
        {
            get { return new ObservableCollection<Page>(_pages); }
        }

        public MainWindowViewModel()
        {
            AddUrlValue = "";
            _addUrlCommand = new RelayCommand(emnu => AddUrl());
            _stopAlertCommand = new RelayCommand(emnu => PlayAlert = false);

            XDocument xDocument = null;

            try
            {
                xDocument = XDocument.Parse(UrlChangeAlert.Properties.Settings.Default.Urls);
            }
            catch (Exception) { }

            if(xDocument != null)
            {
                XElement xPages = xDocument.Element("Pages");

                if(xPages != null)
                    foreach (XElement xPage in xPages.Elements())
                    {
                        Page page = new Page(xPage.Attribute("url").Value, this);

                        foreach (XElement xIgnorePart in xPage.Elements("IgnorePart"))
                        {
                            page.IgnorePart.Add(xIgnorePart.Attribute("value").Value);
                        }

                        _pages.Add(page);
                    }
            }

            _mediaElement.LoadedBehavior = MediaState.Manual;
            _mediaElement.UnloadedBehavior = MediaState.Manual;
            _mediaElement.Source = new Uri("Alarm.wav", UriKind.Relative);
            _mediaElement.MediaEnded += _mediaElement_MediaEnded;

            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
            _dispatcherTimer.Tick += _dispatcherTimer_Tick;
            _dispatcherTimer.Start();
        }

        void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_playAlert && !_playingAlert)
            {
                PlayAlert = true;
                _playingAlert = true;
                _mediaElement.Play();
            }
            else if (!_playAlert && _playingAlert)
            {
                PlayAlert = false;
                _playingAlert = false;
                _mediaElement.Stop();
            }

            foreach (Page page in Pages)
                page.NotifyPropertyChanged("TimeLeft");
        }

        public void RemovePage(Page page)
        {
            _pages.Remove(page);
        }

        void _mediaElement_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            _mediaElement.Position = TimeSpan.Zero;
            _mediaElement.Play();
        }

        public bool CheckValidateUrl(string url)
        {
            Uri outUri;
            return Uri.TryCreate(AddUrlValue, UriKind.RelativeOrAbsolute, out outUri);
        }

        public void AddUrl()
        {
            if (AddUrlValue.Length > 0)
            {
                _pages.Add(new Page(AddUrlValue, this));
                AddUrlValue = "";
                UpdateXml();
            }
        }

        public void UpdateXml()
        {
            UrlChangeAlert.Properties.Settings.Default.Urls = CurrentPageXml.ToString();
            UrlChangeAlert.Properties.Settings.Default.Save();
            NotifyPropertyChanged("Pages");
        }

        private XDocument CurrentPageXml
        {
            get
            {
                XDocument xDocument = new XDocument();

                XElement xPages = new XElement("Pages");
                xDocument.Add(xPages);

                foreach (Page page in _pages)
                {
                    XElement xPage = new XElement("Page");
                    xPage.Add(new XAttribute("url", page.Url));
                    xPages.Add(xPage);

                    foreach (string ignorePart in page.IgnorePart)
                    {
                       xPage.Add(new XElement("IgnorePart", new XAttribute("value", ignorePart)));
                    }
                }

                return xDocument;
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


