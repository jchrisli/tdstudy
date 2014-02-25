using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;

namespace SocketTestClient
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : Window
    {
        RadioButton[] visButtonGroup;
        RadioButton[] backgroundButtonGroup;
        public ConfigStatus config;

        public Configuration()
        {
            InitializeComponent();

            this.visButtonGroup = new RadioButton[3];
            this.visButtonGroup[0] = this._vis_0;
            this.visButtonGroup[1] = this._vis_1;
            this.visButtonGroup[2] = this._vis_2;

            backgroundButtonGroup = new RadioButton[3];
            backgroundButtonGroup[0] = _complexity_0;
            backgroundButtonGroup[1] = _complexity_1;
            backgroundButtonGroup[2] = _complexity_2;

            this.config = new ConfigStatus();
        }

        private void _vis_Checked(object sender, RoutedEventArgs e)
        {
            string numberStr = (((RadioButton)sender).Name.Split(new char[] { '_' }))[2];
            int numberClicked = Convert.ToInt32(numberStr);
            for (int i = 0; i < 3; i++)
            {
                this.visButtonGroup[i].IsChecked = (i == numberClicked) ? true : false;
            }
            this.config.Vis = (ConfigStatus.VisSts)numberClicked;
        }

        private void _complexity_Checked(object sender, RoutedEventArgs e)
        {
            string numberStr = (((RadioButton)sender).Name.Split(new char[] { '_' }))[2];
            int numberClicked = Convert.ToInt32(numberStr);
            for (int i = 0; i < 3; i++)
            {
                this.backgroundButtonGroup[i].IsChecked = (i == numberClicked) ? true : false;
            }
            this.config.Background = (ConfigStatus.BckgrdSts)numberClicked;
        }
    }

    public class ConfigStatus : INotifyPropertyChanged
    {
        public enum BckgrdSts {No = 0, Level1 = 1, Level2 = 2}; 
        public enum VisSts {No = 0, Dot = 1, Ripple = 2};

        private string partID;
        private BckgrdSts background;
        private VisSts vis;

        public event PropertyChangedEventHandler PropertyChanged;

        public string PartID
        {
            get { return partID; }
            set
            {
                partID = value;
                OnPropertyChanged("ID");
            }
        }

        public BckgrdSts Background
        {
            get { return background; }
            set
            {
                background = value;
                OnPropertyChanged("background");
            }
        }

        public VisSts Vis
        {
            get { return vis; }
            set
            {
                vis = value;
                OnPropertyChanged("visualization");
            }
        }

        public ConfigStatus()
        {
            this.partID = "empty name";
            this.background = 0;
            this.vis = 0;
        }

        void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    
}
