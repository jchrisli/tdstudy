#define SERVER_SIDE
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Timers;
using System.Diagnostics;


namespace SocketTestClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if SERVER_SIDE
        const int serverPort = 12345;
        int maxZ = 90;
        int minZ = 50;
#else 
        const int serverPort = 12346;
#endif
        const string serverIPStr = "192.168.21.147";
        const int recvBufferSize = 1024;

        Socket clientSocket;

        Timer tempTimer;
        Timer countDown;
        int countDownNumber;
        Stopwatch stopwatch;

        //test variable
        int frameCount = 0;

        Configuration configWindow;
        Targets targets;
        Vis visualization;
        bool studyOnGoing = false;
        LogData lData;
        bool localBeingUpdated = false;
        bool remoteBeingUpdated = false;
        Logger logger;
        PatternGenerator pg;

#if !SERVER_SIDE
        Distractor dis;
#endif

#if SERVER_SIDE
        MutualCommunication mcServer;
#else
        MutualCommunication mcClient;
#endif

        //touch event handler
        public event EventHandler<TrackingEventArgs> RaiseTrackingEvent;

        public MainWindow()
        {
            InitializeComponent();

            IPAddress serverIP = IPAddress.Parse(serverIPStr);

            IPEndPoint ipEndPoint = new IPEndPoint(serverIP, serverPort);

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //connect the server
            try
            {
                clientSocket.Connect(ipEndPoint);
                Console.WriteLine("Socket connected to {0}",
                        clientSocket.RemoteEndPoint.ToString());
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }

            StateObject state = new StateObject();
            state.workSocket = clientSocket;

#if !SERVER_SIDE
            this.configWindow = new Configuration();
            this.configWindow.config.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(config_PropertyChanged);

            this.configWindow.Show();

            pg = new PatternGenerator(this);
            pg.AddToParent(_my_canvas);
#else
            lData = new LogData();
            stopwatch = new Stopwatch();
#endif

#if SERVER_SIDE
            mcServer = new MutualCommunication("192.168.21.147", 12347);
            mcServer.ServerListen();
            //mcServer.RaiseMsgRcvEvent += new MutualCommunication.MessageReceivedEventHandler(mcServer_RaiseMsgRcvEvent);
#else
            mcClient = new MutualCommunication("192.168.21.147", 12347);
            mcClient.ClientConnect();
            mcClient.RaiseMsgRcvEvent += new MutualCommunication.MessageReceivedEventHandler(mcClient_RaiseMsgRcvEvent);

#endif

            //initialize the visualization (null at the beginning) and the targets
            this.visualization = null;
            targets = new Targets();
            // Receive the response from the remote device.
            clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            //Console.WriteLine("Echoed test = {0}",
            //    Encoding.ASCII.GetString(bytes, 0, bytesRec));
            // this._json_text.Text = Encoding.ASCII.GetString(bytesReceived, 0, bytesRec);

            //a timer to see how many pakages are received per sec
            this.tempTimer = new Timer(1000);
            this.tempTimer.Elapsed += new ElapsedEventHandler(tempTimer_Elapsed);
            this.tempTimer.Enabled = false;

#if !SERVER_SIDE
            dis = new Distractor();
#endif

            // Release the socket.
            if (Keyboard.GetKeyStates(Key.Escape) == KeyStates.Down)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }

#if !SERVER_SIDE
        void mcClient_RaiseMsgRcvEvent(object sender, MessageReceivedArgs msgRcvArgs)
        {
            string dataRcv = msgRcvArgs.Message;
            string[] dataArray = dataRcv.Split(new char[] { ' ' });
            if (dataArray.Length > 0)
            {
                switch (dataArray[0])
                {
                    case "n":
                       string nextWord = dis.NextWord();
                        _distractor.Text = nextWord;
                    default:
                        break;
                }
            }
        }

#else
#endif

#if SERVER_SIDE

#else
#endif


        void config_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "visualization":
                    if (this.visualization != null)
                    {
                        this.visualization.StopAnimation();
                        this.visualization.DetachFromParent();
                    }
                    switch (this.configWindow.config.Vis)
                    {
                        case ConfigStatus.VisSts.Dot:

                            this.visualization = new DotVis(this._my_canvas);
                            this.visualization.AddToParent(this._my_canvas);
                            break;
                        case ConfigStatus.VisSts.Ripple:
                            this.visualization = new RippleVis(this._my_canvas);
                            this.visualization.AddToParent(this._my_canvas);
                            this.visualization.StartAnimation();
                            break;
                        default:
                            this.visualization = null;
                            break;
                    }
                    break;
                case "background":
                    pg.showImage(this.configWindow.config.Background);
                    break;
                default:
                    break;
            }
        }

        void tempTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Console.WriteLine("Pakages : {0}", this.frameCount);
            //this.frameCount = 0;
        }


        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    //  Get the rest of the data.
                    string str = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                    //Console.WriteLine(str);

                    //using custom unpakaging function here
                    TrackingData dataPackage = ParsePackage(str);
                    if (!dataPackage.invalid)
                    {
                        switch (dataPackage.PakageType)
                        {
                            case "lf":
                            case "lh":
                                UpdateLocal(dataPackage);
                                break;
                            case "rf":
                            case "rh":
                                UpdateRemote(dataPackage);
                                break;
                            default:
                                break;
                        }
                    }
                    else Console.WriteLine("invalid string");

                    this.frameCount++;


                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    Console.WriteLine("No data received...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private Point MappingLocal(float tX, float tY)
        {
#if SERVER_SIDE
            double mapX = -1.3813;
            double mapY = -1.2667;
            return new Point((double)(tX * mapX + 1307.9568), (double)(tY * mapY + 1128.9333));
#else
            double mapX = 1.4237;
            double mapY = -1.2508;
            return new Point((double)(tX * mapX - 94.3930), (double)(tY * mapY + 1119.2477));

#endif
        }

        private Point MappingRemote(float tX, float tY)
        {
#if SERVER_SIDE
            double mapX = 1.4237;
            double mapY = -1.2508;
            return new Point(1280 - (double)(tX * mapX - 94.3930), (double)(tY * mapY + 1119.2477));
#else
            double mapX = -1.3813;
            double mapY = -1.2667;
            return new Point(1280 - (double)(tX * mapX + 1307.9568), (double)(tY * mapY + 1128.9333));
#endif
        }

        private void UpdateLocal(TrackingData pakage)
        {
           
            Point screenCoordinate = MappingLocal(pakage.PositionX, pakage.PositionY);
            Action displayAction = delegate
            {
                _position.Text = screenCoordinate.X.ToString() + "," + screenCoordinate.Y.ToString();
                //_position.Text = pakage.PositionZ.ToString();
            };
            this.Dispatcher.Invoke(displayAction, System.Windows.Threading.DispatcherPriority.Normal);
#if SERVER_SIDE
            PointingExperimenter(pakage, screenCoordinate);
#endif
        }

        private void UpdateRemote(TrackingData pakage)
        {
            Point screenCoordinate = MappingRemote(pakage.PositionX, pakage.PositionY);
            
#if !SERVER_SIDE            
            Action workAction = delegate
            {

                switch (configWindow.config.Vis)
                {
                    case ConfigStatus.VisSts.Dot:

                        ((DotVis)this.visualization).Move(screenCoordinate.X, screenCoordinate.Y);
                        this.visualization.Resize(-Math.Abs((double)pakage.PositionZ) * 2 / 250 + 2);
                        if (pakage.PositionZ > 0) ((DotVis)this.visualization).ToggleColor(true);
                        else ((DotVis)this.visualization).ToggleColor(false);
                        break;
                    case ConfigStatus.VisSts.Ripple:
                        ((RippleVis)this.visualization).Move(screenCoordinate.X, screenCoordinate.Y);
                        ((RippleVis)this.visualization).Resize(-Math.Abs((double)pakage.PositionZ) * 2 / 250 + 2);
                        if (pakage.PositionZ > 0) ((RippleVis)this.visualization).ToggleColor(true);
                        else ((RippleVis)this.visualization).ToggleColor(false);
                        break;
                    default:
                        break;
                }
            };
            this.Dispatcher.BeginInvoke(workAction, System.Windows.Threading.DispatcherPriority.Normal);
#else
            if (pakage.PositionZ < 30 && pakage.PositionZ > 0)
            {
                /*
                Console.WriteLine("Remote localLogged: {0} remoteLogged : {1}", lData.LocalLogged, lData.RemoteLogged);

                if (lData.LogRemoteOrNot())
                {
                    lData.remoteX = pakage.PositionX;
                    lData.remoteY = pakage.PositionY;
                    lData.remoteTouchTime = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine("write remote!");
                    lData.RemoteLogged = true;
                }
                else
                    lData.RemoteLogged = false;
                 */
                if (lData.LocalReady)
                {
                    remoteBeingUpdated = true;
                }
            }
            if (remoteBeingUpdated)
            {
                if (lData.compareRemote(pakage.PositionZ))
                {
                    lData.remoteX = pakage.PositionX;
                    lData.remoteY = pakage.PositionY;
                    lData.remoteTouchTime = stopwatch.ElapsedMilliseconds;
                }
                if (lData.RemoteCount == 1)
                {
                    Console.WriteLine("remote 30 times!");
                    lData.RemoteLogged = true;
                    lData.RemoteCount = 0;
                    remoteBeingUpdated = false;
                }
            }

            if (lData.Completed())
            {
                logger.LogLine(lData.GetStrings());
                lData.EverythingNull();
                Console.WriteLine("log file");
            }
            
#endif
        }



        private void OnRaiseTrackingEvent(TrackingEventArgs e)
        {
            EventHandler<TrackingEventArgs> handler = RaiseTrackingEvent;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        #region Pointing Study
        private void PointingExperimenter(TrackingData t, Point s)
        {
            /*
           Action positionAction = delegate
           {
               _position.Text = pakage.PositionZ.ToString();
           };
           this.Dispatcher.BeginInvoke(positionAction, System.Windows.Threading.DispatcherPriority.Normal);*/
            if (studyOnGoing && t.PositionZ > minZ && t.PositionZ < maxZ && targets.testTouch(s.X, s.Y)) // so that the pointer has touched the target
            {
                if (lData.LocalLogged && (!lData.RemoteLogged))
                {
                    lData.remoteX = 0;
                    lData.remoteY = 0;
                    lData.remoteTouchTime = 0;
                    logger.LogLine(lData.GetStrings());
                    Console.WriteLine("special logging, missing remote");
                    lData.EverythingNull();
                }
                bool end = targets.NextMarker();
#if SERVER_SIDE
                //send the message of updating distractor ("n") to the other side
                mcServer.Send(mcServer.serverStateObject.workSocket, "n");
#endif

                /*
                Console.WriteLine("local localLogged: {0} remoteLogged : {1}", lData.LocalLogged, lData.RemoteLogged);
                if (lData.LogLocalOrNot())
                {
                    lData.localX = pakage.PositionX;
                    lData.localY = pakage.PositionY;
                    lData.localTouchTime = stopwatch.ElapsedMilliseconds;
                    targets.CurrentTargetGrid(ref lData.targetGridX, ref lData.targetGridY);
                    lData.LocalLogged = true;
                    Console.WriteLine("log local!");
                }
                else { Console.WriteLine("cannot log local!"); }
                 */
                localBeingUpdated = true;
                if (end)
                {
                    OneRoundEnd();
                }
                else
                {
                    Action workAction = delegate
                    {
                        targets.MoveMarker();
                    };
                    this.Dispatcher.BeginInvoke(workAction, System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            if (localBeingUpdated)
            {
                lData.LocalReady = true;
                if (lData.compareLocal(t.PositionZ))
                {
                    lData.localX = t.PositionX;
                    lData.localY = t.PositionY;
                    lData.localTouchTime = stopwatch.ElapsedMilliseconds;
                    targets.CurrentTargetGrid(ref lData.targetGridX, ref lData.targetGridY);
                }
                if (lData.LocalCount == 1)
                {
                    Console.WriteLine("local 30 times! Stop tracking local!");
                    localBeingUpdated = false;
                    lData.LocalLogged = true;
                    lData.LocalCount = 0;
                }
            }
        }

        private void OneRoundEnd()
        {
            studyOnGoing = false;
            //logger.Close();
            //logger = null;
            Action workAction = delegate
            {
                _countDown.Visibility = System.Windows.Visibility.Visible;
                _countDown.Text = "One round finished:)";
            };
            this.Dispatcher.BeginInvoke(workAction, System.Windows.Threading.DispatcherPriority.Normal);
        }
        #endregion

        #region parse data
        private TrackingData ParsePackage(string str)
        {

            string[] strArray = str.Split(new char[] { '\0' });

            if (strArray.Length > 0)
            {
                string[] dataArray = strArray[0].Split(new char[] { ' ' });
                if (dataArray.Length == 4)
                    return new TrackingData(Convert.ToInt32(dataArray[dataArray.Length - 3]),
                        Convert.ToInt32(dataArray[dataArray.Length - 2]), Convert.ToInt32(dataArray[dataArray.Length - 1]), dataArray[dataArray.Length - 4]);
                else
                {
                    // Console.WriteLine(strArray[strArray.Length - 1]);
                    return new TrackingData(true);
                }
            }
            else throw new System.InvalidOperationException("oho");

        }
        #endregion

        #region Controller which starts a round of the study
        private void _my_canvas_KeyDown(object sender, KeyEventArgs e)
        {
#if SERVER_SIDE
            if (e.Key == Key.I)
            {
                targets.Clear();
                targets.Shuffle();
                targets.Display(_my_canvas);
            }
            if (e.Key == Key.S)
            {
                countDown = new Timer(1000);
                countDownNumber = 3;
                countDown.Elapsed +=new ElapsedEventHandler(countDown_Elapsed);
                countDown.Enabled = true;
                _countDown.Visibility = System.Windows.Visibility.Visible;
                _countDown.Text = "3";
            }
            if (e.Key == Key.P)
            {
                studyOnGoing = false;
                _countDown.Text = "PAUSED";
                logger.Enabled = false;
            }
            if (e.Key == Key.T)
            {
                studyOnGoing = false;
                targets.Clear();
                logger.Close();
                logger = null;
            }
#endif
        }

        void countDown_Elapsed(object sender, ElapsedEventArgs e)
        {
            countDownNumber--;
            if (countDownNumber > -1)
            {
                Action workAction = delegate
                {
                    _countDown.Text = countDownNumber.ToString();
                };
                this.Dispatcher.BeginInvoke(workAction, System.Windows.Threading.DispatcherPriority.Normal);
            }
            else
            {
                countDownNumber = 3;
                countDown.Enabled = false;
                Action workAction = delegate
                {
                    _countDown.Visibility = System.Windows.Visibility.Hidden;
                    targets.MoveMarker();
                };
                this.Dispatcher.BeginInvoke(workAction, System.Windows.Threading.DispatcherPriority.Normal);
                studyOnGoing = true;
                if (logger == null) logger = new Logger();
                logger.Enabled = true;
                stopwatch.Start();
            }

        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (logger != null) logger.Close();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            _coordinate.Text = e.GetPosition(_my_canvas).X.ToString() + "," + e.GetPosition(_my_canvas).Y.ToString();
        }
    }



    #region async data
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        //public StringBuilder sb = new StringBuilder();
    }

    public class TrackingData
    {
        public int PositionX;
        public int PositionY;
        public int PositionZ;
        public string PakageType;

        public bool invalid;

        public TrackingData(int pX, int pY, int pZ, string t)
        {
            this.PositionX = pX;
            this.PositionY = pY;
            this.PositionZ = pZ;
            this.PakageType = t;
            this.invalid = false;
        }

        public TrackingData(bool validity)
        {
            this.invalid = validity;
        }
    }

    public class LogData
    {
        public double remoteX;
        public double remoteY;
        public double localX;
        public double localY;
        public double remoteTouchTime;
        public double localTouchTime;
        public int targetGridX;
        public int targetGridY;
        double remoteMin;
        double localMax;
        int remoteCount = 0;
        int localCount = 0;

        //30 frames of data, around 1 second
        public static readonly int bufferSize = 30;

        bool remoteLogged = false;
        bool localLogged = false;
        bool localReady = false;
        bool remoteReady = false;

        public int RemoteCount
        {
            get { return remoteCount; }
            set { remoteCount = value; }
        }

        public int LocalCount
        {
            set { localCount = value; }
            get { return localCount; }
        }

        public LogData()
        {
            
        }

        public bool LogRemoteOrNot()
        {
            if ((!remoteLogged) && localLogged) return true;
            else return false;
        }

        public bool compareLocal(double comingLocal)
        {
            if (localCount == 0)
            {
                localMax = comingLocal;
                localCount++;
                return true;
            }
            else
            {

                localCount++;

                if (comingLocal > localMax)
                {

                    localMax = comingLocal;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool compareRemote(double comingRemote)
        {
            if (remoteCount == 0)
            {
                remoteMin = comingRemote;
                remoteCount++;
                return true;
            }
            else
            {
                remoteCount++;
                if (remoteMin >　comingRemote)
                {
                    remoteMin = comingRemote;
                    
                    return true;
                }
                else return false;
            }
        }

        public bool RemoteLogged { set { remoteLogged = value; } get { return remoteLogged; } }
        public bool LocalLogged { set { localLogged = value; } get { return localLogged; } }
        public bool RemoteReady { set { remoteReady = value; } get { return remoteReady; } }
        public bool LocalReady { set { localReady = value; } get { return localReady; } }

        public bool LogLocalOrNot()
        {
            Console.WriteLine("check log local!");
            if ((!localLogged) && (!remoteLogged))
                return true;
            else return false;
        }

        public bool Completed()
        {
            if (remoteLogged && localLogged)
                return true;
            else return false;
        }

        public string[] GetStrings()
        {
            string[] data = new string[8];
            data[0] = localTouchTime.ToString();
            data[1] = localX.ToString();
            data[2] = localY.ToString();
            data[3] = remoteTouchTime.ToString();
            data[4] = remoteX.ToString();
            data[5] = remoteY.ToString();
            data[6] = targetGridX.ToString();
            data[7] = targetGridY.ToString();
            return data;
        }

        //put everyting back to null after they are logged. In this way, if the data is stale can be checked.

        public void EverythingNull()
        {
            remoteLogged = false;
            localLogged = false;
            remoteReady = false;
            localReady = false;
        }
    }
    #endregion
}
