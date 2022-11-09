using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RR.ui;
using RR.Properties;
using System.Windows.Threading;
using RR.ui.conts;
using RR.Dataplane.NOS;
using RR.Comuting.computing;
using RR.NetAnimator;
using RR.Comuting.Routing;
using RR.Comuting.SinkClustering;
using RR.RingRouting;
using RR.Models.Energy;
using MP.MergedPath.Routing;
using RR.Cluster;

namespace RR.Dataplane
{
    public enum SensorState { intalized, Active, Sleep } // defualt is not used. i 
   


    /// <summary>
    /// Interaction logic for Node.xaml
    /// </summary>
    public partial class Sensor : UserControl
    {

        #region MP
        public bool IsHightierNode { get; set; } // true means this node is high tier node.
        public bool IsAnAgent { get; set; } // is the node an agent.
        public Sensor RightVldNeighbor { get; set; } // right neighbor on the diaogonal
        public Sensor LeftVldNeighbor { get; set; }  // left neighbor on the diaogonal

        public List<Sensor> VLDNodesNeighbobre = new List<Sensor>(); // for the low-tier node that are close to the diaogonal

        public NetworkOverheadCounter counter = new NetworkOverheadCounter();
        public MyNetAnimator Animator;

        /// <summary>
        /// this is to be used as an agent record only.
        /// the agent save this one.
        /// </summary>
        private List<SinksAgentsRow> SinksAgentsList = new List<SinksAgentsRow>(); 

        /// <summary>
        /// This is to be used When high tier node response to the source. The high tier node save the records of sinks in this list.
        /// </summary>
        private  List<SinksAgentsRow> HighTierKeepRecordsOFSinksLocations = new List<SinksAgentsRow>();
        public RecoveryRow RecoveryRow;// for recovery.

        private Dictionary<int, AgentsRow> AnchorNodes = new Dictionary<int, AgentsRow>();

        private Dictionary<int, Sensor> AgentFollowUpList = new Dictionary<int, Sensor>();
        //-- clusring items:
        public bool IsClustered = false; // this is used when clustering the sinks. The agent of the sink is representing the sink.
        public List<AngleSimlirityEdge> Simlirities = new List<AngleSimlirityEdge>();

        public AgentsRow agentRow { get; set; }
        public TimeSpan NextqueryAfter 
        { 
            get 
            { 
                return agentRow != null ? agentRow.returnTime: new TimeSpan(0,0,0); 
            } 
        }

        public int inCluster = -1;
        public NetCluster myCluster { get; set; }
        public Sensor clusterHeader
        {
            get
            {
                return myCluster != null ? myCluster.Header : null;
            }
        }
        public bool isHeader { get; set; }


        #endregion
        #region Commone parameters.

        public Radar Myradar; 
        public List<Arrow> MyArrows = new List<Arrow>();
        public MainWindow MainWindow { get; set; } // the mian window where the sensor deployed.
        public static double SR { get; set; } // the radios of SENSING range.
        public double SensingRangeRadius { get { return SR; } }
        public static double CR { get; set; }  // the radios of COMUNICATION range. double OF SENSING RANGE
        public double ComunicationRangeRadius { get { return CR; } }
        public double BatteryIntialEnergy; // jouls // value will not be changed
        private double _ResidualEnergy; //// jouls this value will be changed according to useage of battery
        public BoXMAC Mac { get; set; } // the mac protocol for the node.
        public SensorState CurrentSensorState { get; set; } // state of node.
        public List<RoutingLog> Logs = new List<RoutingLog>();
        public List<Sensor> NeighborsTable = null; // neighboring table.
        public int ID { get; set; } // the ID of sensor.
        private DispatcherTimer SendPacketTimer = new DispatcherTimer();// 
        public DispatcherTimer QueuTimer = new DispatcherTimer();// to check the packets in the queue right now.
        public Queue<Packet> WaitingPacketsQueue = new Queue<Packet>(); // packets queue.

        // for recharging
        public int numOfPacketsFollowing { get; set; }
        public int numOfPacketsPassingThrough { get; set; }
        public double transmittingRate
        {
            get { return numOfPacketsPassingThrough / PublicParamerters.SimulationTime; }
        }

        public RingNodes RingNodesRule = new RingNodes(); // Ring Nodes Rule
        public RingNeighbor RingNeighborRule { get; set; }
        public bool isExpanding = true;
        public bool isSinkAgent = false;
        public Point NetworkCenter { get; set; }
        public bool isInsideRing { get; set; } //will be null for ring nodes like that

        private BatteryLevelThresh BT = new BatteryLevelThresh();

       // private bool ReachedBatteryThresh { set; get; }

        public bool ReachedBatterThresh
        {
            get
            {
                return BT.threshReached(ResidualEnergyPercentage);
            }
        }

        /// <summary>
        /// CONFROM FROM NANO NO JOUL
        /// </summary>
        /// <param name="UsedEnergy_Nanojoule"></param>
        /// <returns></returns>




        public Sensor(int nodeID)
        {
            InitializeComponent();
            //: sink is diffrent:
            if (IsAnAgent==true)
                BatteryIntialEnergy = PublicParamerters.BatteryIntialEnergyForSink; // the value will not be change
            else
                BatteryIntialEnergy = PublicParamerters.BatteryIntialEnergy;
           
            
            ResidualEnergy = BatteryIntialEnergy;// joules. intializing.
            Prog_batteryCapacityNotation.Value = BatteryIntialEnergy;
            Prog_batteryCapacityNotation.Maximum = BatteryIntialEnergy;
            lbl_Sensing_ID.Content = nodeID;
            ID = nodeID;
            QueuTimer.Interval = PublicParamerters.QueueTime;
            QueuTimer.Tick += DeliveerPacketsInQueuTimer_Tick;
            //:

            SendPacketTimer.Interval = TimeSpan.FromSeconds(1);
            Animator = new MyNetAnimator(this);

           
        }

       

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            

        }

        /// <summary>
        /// hide all arrows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            
            if(SinksAgentsList.Count>0)
            {
                string str = "";
                foreach(SinksAgentsRow row in SinksAgentsList)
                {
                    str += "Agent ID=" + row.AgentNode.ID + "  Sink ID=" + row.Sink.ID + "\r\n";
                }

                ToolTip = new Label() { Content = str };
            }
            else
            {
                ToolTip = new Label() { Content = ResidualEnergyPercentage.ToString() };
            }
            

        }

        

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           
        }

       

        public int ComputeMaxHopsUplink
        {
            get
            {
               
                return 10;
            }
        }

        public int ComputeMaxHopsDownlink(Sensor endNode)
        {
            return 10;
        }




        #region send data: /////////////////////////////////////////////////////////////////////////////


        public void IdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Visible;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Yellow;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void UnIdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Hidden;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Transparent;
                Dispatcher.Invoke(actionxx);
            }
        }


        /// <summary>
        /// in JOULE
        /// </summary>
        public double ResidualEnergy // jouls this value will be changed according to useage of battery
        {
            get { return _ResidualEnergy; }
            set
            {
                _ResidualEnergy = value;
                Prog_batteryCapacityNotation.Value = _ResidualEnergy;
            }
        } //@unit(JOULS);


        /// <summary>
        /// 0%-100%
        /// </summary>
        public double ResidualEnergyPercentage
        {
            get { return (ResidualEnergy / BatteryIntialEnergy) * 100; }
        }
        /// <summary>
        /// visualized sensing range and comuinication range
        /// </summary>
        public double VisualizedRadius
        {
            get { return Ellipse_Sensing_range.Width / 2; }
            set
            {
                // sensing range:
                Ellipse_Sensing_range.Height = value * 2; // heigh= sen rad*2;
                Ellipse_Sensing_range.Width = value * 2; // Width= sen rad*2;
                SR = VisualizedRadius;
                CR = SR * 2; // comunication rad= sensing rad *2;

                // device:
                Device_Sensor.Width = value * 4; // device = sen rad*4;
                Device_Sensor.Height = value * 4;
                // communication range
                Ellipse_Communication_range.Height = value * 4; // com rang= sen rad *4;
                Ellipse_Communication_range.Width = value * 4;

                // battery:
                Prog_batteryCapacityNotation.Width = 8;
                Prog_batteryCapacityNotation.Height = 2;
            }
        }

        /// <summary>
        /// Real postion of object.
        /// </summary>
        public Point Position
        {
            get
            {
                double x = Margin.Left;
                double y = Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
            set
            {
                Point p = value;
                Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        /// <summary>
        /// center location of node.
        /// </summary>
        public Point CenterLocation
        {
            get
            {
                double x = Margin.Left;
                double y = Margin.Top;
                Point p = new Point(x + CR, y + CR);
                return p;
            }
        }

        bool StartMove = false; // mouse start move.
        private void Device_Sensor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    Position = P;
                    StartMove = true;
                }
            }
        }

        private void Device_Sensor_MouseMove(object sender, MouseEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (StartMove)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    this.Position = P;
                }
            }
        }

        private void Device_Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StartMove = false;
        }
        #endregion

        //private void GenerateCReqPacket()
        //{
        //    _ = new RequestForCharging(this);
        //}

        public int rechargeReqCount = 0;
        public DateTime ChargingRequestInitiate { get; set; }
        private void Prog_batteryCapacityNotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            double val = ResidualEnergyPercentage;
            if (val <= 0)
            {

                PublicParamerters.IsNetworkDied = true;
                // dead certificate:
                ExpermentsResults.Lifetime.DeadNodesRecord recod = new ExpermentsResults.Lifetime.DeadNodesRecord();
                recod.DeadAfterPackets = PublicParamerters.NumberofGeneratedPackets;
                recod.DeadOrder = PublicParamerters.DeadNodeList.Count + 1;
                double deadAt = PublicParamerters.SimulationTime;
                recod.DeadAtSecond = deadAt;
                recod.DeadNodeID = ID;

                PublicParamerters.DeadNodeList.Add(recod);

                Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));
                Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));


                if (Settings.Default.StopeWhenFirstNodeDeid)
                {
                    MainWindow.TimerCounter.Stop();
                    MainWindow.RandomSelectSourceNodesTimer.Stop();
                    Settings.Default.StopSimlationWhen = PublicParamerters.SimulationTime;
                    MainWindow.top_menu.IsEnabled = true;
                    Settings.Default.Save();


                    // stop the sinks.
                    //MainWindow.StopSinks();


                }

            }

            if (val < PublicParamerters.RechargeReThr && val > 0)
            {
                if (ID == 0)
                {
                    ResidualEnergy = PublicParamerters.BatteryIntialEnergy; // in this case sensor 0 is located close to the Base station.
                }
                else
                {
                    if (!PublicParamerters.requestedList.Contains(this))
                    {
                        rechargeReqCount++;
                        ChargingRequestInitiate = DateTime.Now;

                        _ = new RequestForCharging(this, "regular");
                        PublicParamerters.requestedList.Add(this);
                        //System.Console.WriteLine("Sensor sent recharge request: {0}", this.ID);
                    }
                }             
            }
            if (val < 40 && val > 0)
            {
                rechargeReqCount++;

                if(rechargeReqCount < 3)
                {
                    ChargingRequestInitiate = DateTime.Now;
                    _ = new RequestForCharging(this, "emergency");

                    if (!PublicParamerters.requestedList.Contains(this))
                    {
                        PublicParamerters.requestedList.Add(this);
                    }

                    foreach (Sensor nei in this.NeighborsTable)
                    {
                        if(nei.RingNodesRule.isRingNode && nei.ResidualEnergyPercentage < PublicParamerters.RechargeReThr && nei.ResidualEnergyPercentage > 0)
                        {
                            nei.rechargeReqCount++;

                            ChargingRequestInitiate = DateTime.Now;
                            _ = new RequestForCharging(nei, "emergency");

                            if (!PublicParamerters.requestedList.Contains(nei))
                            {
                                PublicParamerters.requestedList.Add(nei);
                            }
                        }
                        
                    }
                }
                               
            }          

            if (val >= 1 && val <= 9)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));

            }

            if (val >= 10 && val <= 19)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
            }

            if (val >= 20 && val <= 29)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29)));
                Dispatcher.Invoke(() => Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29))));
            }

            // full:
            if (val >= 30 && val <= 39)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
            }
            // full:
            if (val >= 40 && val <= 49)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));

            }
            // full:
            if (val >= 50 && val <= 59)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
            }
            // full:
            if (val >= 60 && val <= 69)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
            }
            // full:
            if (val >= 70 && val <= 79)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
            }
            // full:
            if (val >= 80 && val <= 89)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
                
            }            
            // full:
            if (val >= 90 && val <= 100)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
            }
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool isAgentAvailable()
        {
            bool ans = false;

            if (agentRow == null)
            {
                return ans;
            }

            DateTime currentdate = DateTime.Now;
            int s = currentdate.Second;
            int m = currentdate.Minute;
            int h = currentdate.Hour;
            int d = currentdate.Day;
            TimeSpan currentTime = new TimeSpan(h, m, s);
            currentTime = currentdate.Subtract(currentdate.Date); // also possible

            List<Sensor> sensorList = new List<Sensor>();

            foreach (var agent in agentRow.agentAtTimeT)
            {
                if (TimeSpan.Compare(currentTime, agent.Value) != 1)
                {
                    ans = true;
                }
                else
                {
                    sensorList.Add(agent.Key);
                }

                //int hdif = currentdate.Hour - agent.Value.Hours;
                //int mdif = currentdate.Minute - agent.Value.Minutes;
                //int sdif = currentdate.Second - agent.Value.Seconds;

                //if (hdif == 0 && mdif == 0 && (sdif >= -50 && sdif <= 10)) //// -1 if t1 is shorter than t2. 0 if  t1 is equal to t2. 1 if  t1 is longer than t2.
                //{
                //    ans = true;
                //}
                //else
                //{ 
                //    sensorList.Add(agent.Key);
                //}
            }
            

            foreach (var sensor in sensorList)
            {
                //Console.WriteLine("Sensor " + sensor.ID.ToString() + " is removed from agentlist. currentTime is " + currentTime.ToString() + " agentTime is " + agentRow.agentAtTimeT[sensor]);
                agentRow.agentAtTimeT.Remove(sensor);               
            }

            return ans;
        }

        #region RingRouting FollowupMechansim

        private DispatcherTimer FollowupMechansimTimer = new DispatcherTimer(); //This timer will make sure that the information is not old
        private Stack<int> OldSinkIDSStack = new Stack<int>();// Stack that holds the sinkIDs to be refereshed after timer interval

        

        /// <summary>
        /// Whenever the sink has a new agent it will inform the old agent of the position information of the new agent
        /// </summary>
        /// <param name="sinkID"></param>
        /// <param name="newAgent"></param>
        public void AgentStartFollowupMechansim(int sinkID, Sensor newAgent)
        {
            
            OldSinkIDSStack.Push(sinkID);
            AgentFollowUpList[sinkID] = newAgent;
            if (!FollowupMechansimTimer.IsEnabled)
            {
                FollowupMechansimTimer.Interval = TimeSpan.FromSeconds(60);
                FollowupMechansimTimer.Tick += FollowupMechansimTimer_Tick;
                FollowupMechansimTimer.Start();
            }

        }

        /// <summary>
        /// This timer makes sure the information contained in the followup mehcansim dictionary is up date
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FollowupMechansimTimer_Tick(object sender, EventArgs e)
        {
           
            if (OldSinkIDSStack.Count == 0)
            {
                FollowupMechansimTimer.Stop();
                return;
            }
            int oldSinkID = OldSinkIDSStack.Pop();
            //Console.WriteLine("Agent " + ID + " removed all new agent information for the sinkID: "+oldSinkID+" newAgent ID: " + AgentFollowUpList[oldSinkID]);
            AgentFollowUpList[oldSinkID] = null;
            if (AgentFollowUpList.Count == 0)
            {
                FollowupMechansimTimer.Stop();
            }
        }

        private bool isAnOldAgentNode()
        {
            return (OldSinkIDSStack.Count > 0);
        }

        public Sensor GetNewAgentForFollowup(int sinkID)
        {
            if (isAnOldAgentNode())
            {
                if(AgentFollowUpList[sinkID] != null)
                {
                    return AgentFollowUpList[sinkID];
                }
            }

            return null;
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        public void SwichToActive()
        {
            Mac.SwichToActive();

        }

        /// <summary>
        /// 
        /// </summary>
        private void SwichToSleep()
        {
            Mac.SwichToSleep();
        }
        


        /// <summary>
        ///  select this node as a source and let it 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btn_send_packet_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //ObtainSinkFreshPositionMessage ob = new ObtainSinkFreshPositionMessage(this);
        }

        
        // try.
        private void DeliveerPacketsInQueuTimer_Tick(object sender, EventArgs e)
        {
            Packet waitingpacket = WaitingPacketsQueue.Dequeue();
            waitingpacket.ReTransmissionTry++;
            if (waitingpacket.ReTransmissionTry <= Settings.Default.ReTransmissionAttemps)
            {
               // Console.WriteLine(waitingpacket.PID + ">>>" + waitingpacket.ReTransmissionTry);
                switch (waitingpacket.PacketType)
                {
                    case PacketType.ReportSinkPosition:
                        new ReportSinkPositionMessage().HandelInQueuPacket(this, waitingpacket);
                        break;
                    case PacketType.ShareSinkPosition:
                        new ShareSinkPositionIntheHighTier().HandelInQueuPacket(this, waitingpacket);
                        break;
                    case PacketType.ObtainSinkPosition:
                        new ObtainSinkFreshPositionMessage().HandelInQueuPacket(this, waitingpacket);
                        break;
                    case PacketType.ResponseSinkPosition:
                        new ResonseSinkPositionMessage().HandelInQueuPacket(this, waitingpacket);
                        break;
                    case PacketType.ReportSojournPoints:
                        new ShareSinkPosition().HandelInQueuPacket(this, waitingpacket);
                        break;
                    case PacketType.Data:
                        new DataPacketMessages().HandelInQueuPacket(this, waitingpacket);
                        break;
                }
            }
            else
            {
                // the packet should be droped here:
                counter.DropPacket(waitingpacket, this, PacketDropedReasons.WaitingTime);
            }

            
            if (WaitingPacketsQueue.Count == 0)
            {
                if(Settings.Default.ShowRadar) Myradar.StopRadio();
                QueuTimer.Stop();
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);
            }
        }



        #endregion


        private void lbl_MouseEnter(object sender, MouseEventArgs e)
        {
            ToolTip = new Label() { Content = "("+ID + ") [ " + ResidualEnergyPercentage + "% ] [ " + ResidualEnergy + " J ]" };
        }

        private void btn_show_routing_log_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Logs.Count>0)
            {
                UiShowRelativityForAnode re = new ui.UiShowRelativityForAnode();
                re.dg_relative_shortlist.ItemsSource = Logs;
                re.Show();
            }
        }

        private void btn_draw_random_numbers_MouseDown(object sender, MouseButtonEventArgs e)
        {
            List<KeyValuePair<int, double>> rands = new List<KeyValuePair<int, double>>();
            int index = 0;
            foreach (RoutingLog log in Logs )
            {
                if(log.IsSend)
                {
                    index++;
                    rands.Add(new KeyValuePair<int, double>(index, log.ForwardingRandomNumber));
                }
            }
            UiRandomNumberGeneration wndsow = new ui.UiRandomNumberGeneration();
            wndsow.chart_x.DataContext = rands;
            wndsow.Show();
        }

        private void Ellipse_center_MouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        private void btn_show_my_duytcycling_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
        }

        private void btn_draw_paths_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
        }

       
         
        private void btn_show_my_flows_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
            ListControl ConMini = new ui.conts.ListControl();
            ConMini.lbl_title.Content = "Mini-Flow-Table";
            //ConMini.dg_date.ItemsSource = MiniFlowTable;


            ListControl ConNei = new ui.conts.ListControl();
            ConNei.lbl_title.Content = "Neighbors-Table";
            ConNei.dg_date.ItemsSource = NeighborsTable;

            UiShowLists win = new UiShowLists();
            win.stack_items.Children.Add(ConMini);
            win.stack_items.Children.Add(ConNei);
            win.Title = "Tables of Node " + ID;
            win.Show();
            win.WindowState = WindowState.Maximized;
        }

        private void btn_send_1_p_each1sec_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SendPacketTimer.Start();
            SendPacketTimer.Tick += SendPacketTimer_Random; // redfine th trigger.
        }



        public void RandomSelectEndNodes(int numOFpACKETS)
        {
            
        }

        private void SendPacketTimer_Random(object sender, EventArgs e)
        {
           
        }

        /// <summary>
        /// i am slected as end node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_select_me_as_end_node_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            
        }

        public void SelectMeAsEndNodeAndSendonepacketPer5s_Tick(object sender, EventArgs e)
        {
         //   PublicParamerters.SinkNode.GenerateMultipleControlPackets(1, this);
        }

        public Point getDestinationForRingAccess()
        {
            Point destination = new Point();
            if (isInsideRing)
            {
                //send to the opposite side direction of the network center
                destination = Operations.GetDirectionToRingNodes(this);
            }
            else
            {
                destination = PublicParamerters.networkCenter;
            }
            return destination;
        }


        #region MergedPaths
        /// <summary>
        /// add or update sink agent record.
        /// agent only. 
        /// </summary>
        /// <param name="newsink"></param>
        public void AddNewAGent(SinksAgentsRow newsink)  
        {
            if (newsink != null)
            {
                // add new record
                SinksAgentsList.Add(newsink);
                //: add notation for being selected as agent:
                Ellipse_nodeTypeIndicator.Fill = Brushes.BurlyWood;
                IsAnAgent = true;
                Ellipse_Communication_range.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// remove sink record from my list
        /// agent only.
        /// </summary>
        /// <param name="newsink"></param>
        /// <returns></returns>
        public bool RemoveFromAgent(SinksAgentsRow newsink)
        {
           bool isRemoved= SinksAgentsList.Remove(newsink);
            if (isRemoved)
            {
                if (SinksAgentsList.Count == 0)
                {
                    //: notation for a 
                    Ellipse_nodeTypeIndicator.Fill = Brushes.Transparent;
                    Ellipse_Communication_range.Visibility = Visibility.Hidden;
                    IsAnAgent = false;
                }
            }

            return isRemoved;
        }

        /// <summary>
        /// This checks if the RingNode already recieved this information from its other side neighbor
        /// if true that means that both AntiClockwise and Clockwise packets have made a full turn around the ring and we need to succefuly stop them from resednding
        /// </summary>
        /// <param name="info">Current Received Information in the ANPIS packet</param>
        public bool AlreadyRecievedAgentInfo(AgentsRow info)
        {
            if (agentRow == null) return false;

            if (agentRow != null && TimeSpan.Compare(agentRow.returnTime, info.returnTime) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// this is for the nodes in the high tier node. they save the locations of the sink forever.
        /// </summary>
        /// <param name="newsinkInfo"></param>
        public void AddSinkRecordInHighTierNode(AgentsRow newsinkInfo) 
        {
            
            AnchorNodes[newsinkInfo.Sink.ID] = newsinkInfo;
            RingNodesRule.AnchorNodes[newsinkInfo.Sink.ID] = newsinkInfo;

        }

       
       

        /// <summary>
        /// This is called when the ring change chooses a new ring node
        /// </summary>
        /// <param name="setNewData"></param>
        public void SetSinksAgentsForNewHighTierNode(Dictionary<int,AgentsRow> setNewData)
        {
            AnchorNodes = setNewData;
            RingNodesRule.AnchorNodes = setNewData;
        }

        /// <summary>
        /// retrun the list of sinks
        /// the node is high tier node. 
        /// this will be uese to response to the requester source node.
        /// </summary>
        public Dictionary<int,  AgentsRow> GetAnchorNodesFromHighTierNodes
        {
            get
            {
                return AnchorNodes;
            }
        }

        public void NotHighTierNode()
        {
            IsHightierNode = false;
            AnchorNodes = null;
        }


       /// <summary>
       /// get the sinks which are mine.
       /// the node is low tier node
       /// </summary>
        public List<SinksAgentsRow> GetSinksAgentsList
        {
            get
            {
                return SinksAgentsList;
            }
        }

        public int GetNumberOfSinksForAgent()
        {
            return SinksAgentsList.Count;
        }
        

        #endregion





        /*** Vistualize****/

        public void ShowID(bool isVis )
        {
           // if (isVis) { lbl_Sensing_ID.Visibility = Visibility.Visible; lbl_hops_to_sink.Visibility = Visibility.Visible; }
           // else { lbl_Sensing_ID.Visibility = Visibility.Hidden; lbl_hops_to_sink.Visibility = Visibility.Hidden; }
        }

        public void ShowSensingRange(bool isVis)
        {
            if (isVis) Ellipse_Sensing_range.Visibility = Visibility.Visible;
            else Ellipse_Sensing_range.Visibility = Visibility.Hidden;
        }

        public void ShowComunicationRange(bool isVis)
        {
            if (isVis) Ellipse_Communication_range.Visibility = Visibility.Visible;
            else Ellipse_Communication_range.Visibility = Visibility.Hidden;
        }

        public void ShowBattery(bool isVis) 
        {
            if (isVis) Prog_batteryCapacityNotation.Visibility = Visibility.Visible;
            else Prog_batteryCapacityNotation.Visibility = Visibility.Hidden;
        }

        private void btn_update_mini_flow_MouseDown(object sender, MouseButtonEventArgs e)
        {
           // UplinkRouting.UpdateUplinkFlowEnery(this);
        }

        private void Btn_see_distrubutions_MouseDown(object sender, MouseButtonEventArgs e)
        {
           

        }
    }
}
