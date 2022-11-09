using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RR.Dataplane;
using RR.db;
using RR.Intilization;
using RR.Properties;
using System.Windows.Media;
using System.Windows.Threading;
using RR.Forwarding;
using RR.ExpermentsResults.Energy_consumptions;
using RR.ControlPlane.NOS.TC;
using RR.DataPlane.NeighborsDiscovery;
using System.Windows.Input;
using RR.Comuting.Routing;
using RR.Dataplane.NOS;
using RR.RingRouting;
using RR.Cluster;

namespace RR.ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DispatcherTimer TimerCounter = new DispatcherTimer();
        public DispatcherTimer RandomSelectSourceNodesTimer = new DispatcherTimer();
        public static double Swith;// sensing feild width.
        public static double Sheigh;// sensing feild height.

        /// <summary>
        /// the area of sensing feild.
        /// </summary>
        public static double SensingFeildArea
        {
            get
            {
                return Swith * Sheigh;
            }
        }

        
        public List<Sensor> myNetWork = new List<Sensor>();
        public List<Sink> mySinks = new List<Sink>();
        //public List<Charger> myChargers = new List<Charger>();
        public Charger MCharger { get; set; }
        public BaseStation myBstation { get; set; }

        bool isCoverageSelected = false;


        public MainWindow()
        {
            InitializeComponent();
            // sensing feild
            Swith = Canvas_SensingFeild.Width - 218;
            Sheigh = Canvas_SensingFeild.Height - 218;
            PublicParamerters.SensingFeildArea = SensingFeildArea;
            PublicParamerters.MainWindow = this;
            // battery levels colors:
            FillColors();

            PublicParamerters.RandomColors = RandomColorsGenerator.RandomColor(100); // 100 diffrent colores.



            _show_id.IsChecked = Settings.Default.ShowID;
            _show_battrey.IsChecked = Settings.Default.ShowBattry;
            _show_sen_range.IsChecked= Settings.Default.ShowSensingRange;
            _show_com_range.IsChecked= Settings.Default.ShowComunicationRange;
            _Show_Routing_Paths.IsChecked = Settings.Default.ShowRoutingPaths;
            _Show_Packets_animations.IsChecked = Settings.Default.ShowAnimation;
        }

        private void TimerCounter_Tick(object sender, EventArgs e)
        {
            //
            //
            if (PublicParamerters.SimulationTime <= Settings.Default.StopSimlationWhen + PublicParamerters.MacStartUp)
            {
                PublicParamerters.SimulationTime += 1;
                Title = "RECO:" + PublicParamerters.SimulationTime.ToString();
            }
            else
            {

                StopSimulation();
            }

            if (PublicParamerters.TotalNumChargedSensors >= 100)
            //if (PublicParamerters.NumberOfDataPacket >= 5000)
            {
                StopSimulation();
            }

        }


        public void StopSimulation()
        {
            TimerCounter.Stop();
            RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0);
            RandomSelectSourceNodesTimer.Stop();
            top_menu.IsEnabled = true;

            // stop sinks:
            StopSinks();
            StopCharging();
        }

        public void StopSinks()
        {
            foreach (Sink sink in mySinks) sink.StopMoving();
        }
        public void StopCharging()
        {
            if(MCharger != null && myBstation != null)
            {
                myBstation.StopScheduling();
            }
            
        }

        /// <summary>
        /// Stops the simulation; first it will stop the sinks from moving, second if there is a dead node, all packets in the WaitingQueue will be dropped
        /// </summary>
        /// <param name="isNodeDead"></param>

        private void SelectRandomNodesUntilFirstNodeDie(object sender, EventArgs e)
        {
            // start sending after the nodes are intilized all.
            if (PublicParamerters.SimulationTime > PublicParamerters.MacStartUp)
            {
                GenerateRandomPacket();
            }
        }

        /// <summary>
        ///  recharging calling from mainwindow
        /// </summary>
        //public void GenerateCReqPacket(int id)
        //{
        //    Sensor sens = myNetWork[id];
        //    _ = new RequestForCharging(sens);
        //}

        private void GenerateRandomPacket()
        {
            int index = 1 + Convert.ToInt16(UnformRandomNumberGenerator.GetUniform(PublicParamerters.NumberofNodes - 2));
            Sensor sensor = myNetWork[index];
            sensor.numOfPacketsPassingThrough += 1;

            DateTime currentdate = DateTime.Now;
            TimeSpan currentTime = currentdate.Subtract(currentdate.Date);

            if (TimeSpan.Compare(sensor.NextqueryAfter, currentTime) == 1 && sensor.agentRow != null) // || sensor.isAgentAvailable())
            {
                new DataPacketMessages(sensor, null);
            }
            else
            {
                ObtainSinkFreshPositionMessage ob = new ObtainSinkFreshPositionMessage(sensor);
            }
        }


        public void SetPacketRateS1(double s)
        {
            if (s == 0)
            {
                Settings.Default.AnimationSpeed = 1;
                RandomSelectSourceNodesTimer.Stop();
                Settings.Default.PacketRate = s;
                Settings.Default.Save();
            }
            else
            {
                RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(s);
                RandomSelectSourceNodesTimer.Start();
                RandomSelectSourceNodesTimer.Tick += SelectRandomNodesUntilFirstNodeDie;
                Settings.Default.PacketRate = s;
                Settings.Default.Save();
            }
        }


        /// <summary>
        /// SCIONARO2: packet rate is set to 0.5. the packets are generate such that
        /// </summary>
        /// <param name="NumberOfPacketsTobeGenerated"></param>
        /// <param name=""></param>
        public void SetNumberOfPackets(long Maxnumpck)
        {
            maxPCK = 0; // Clear the counter.
            Settings.Default.MaxNumberofObtianPackets = Maxnumpck;
            Settings.Default.PacketRate = 0.1;
            RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(Settings.Default.PacketRate);
            RandomSelectSourceNodesTimer.Start();
            RandomSelectSourceNodesTimer.Tick += GeneratePacketsWithinMax;

        }

        long maxPCK = 0;
        /// <summary>
        /// generate x number of packet and then stop the simulation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GeneratePacketsWithinMax(object sender, EventArgs e)
        {
            if (PublicParamerters.NumberOfDataPacket < Settings.Default.MaxNumberofObtianPackets)
            {

                GenerateRandomPacket();
                maxPCK++;
            }
            else
            {
                // stop:
                StopSimulation();
                maxPCK = 0;
            }
        }

        private void FillColors()
        {

            // POWER LEVEL:
            lvl_0.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));
            lvl_1_9.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9));
            lvl_10_19.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19));
            lvl_20_29.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29));
            lvl_30_39.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39));
            lvl_40_49.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49));
            lvl_50_59.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59));
            lvl_60_69.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69));
            lvl_70_79.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79));
            lvl_80_89.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89));
            lvl_90_100.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100));

            // MAC fuctions:
            lbl_node_state_check.Fill = NodeStateColoring.ActiveColor;
            lbl_node_state_sleep.Fill = NodeStateColoring.SleepColor;
        }


        private void BtnFile(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string Header = item.Header.ToString();
            switch (Header)
            {
                case "_Multiple Nodes":
                    {
                        if (myNetWork.Count == 0)
                        {
                            UiAddNodes ui = new UiAddNodes();
                            ui.MainWindow = this;
                            ui.Show();
                        }
                        else
                        {
                            MessageBox.Show("plz clear the network first");
                        }
                        break;
                    }
                case "_Add sink":
                    if (isCoverageSelected)
                    {
                        Sink sink = new Sink();
                        Canvas_SensingFeild.Children.Add(sink);
                    }
                    else
                    {
                        MessageBox.Show("Select the Coverage!");
                    }
                    break;
                case "_Add BaseStation":
                    if (isCoverageSelected)
                    {
                        BaseStation Bs = new BaseStation();
                        Canvas_SensingFeild.Children.Add(Bs);
                        Charger ch = new Charger();                      
                        //Canvas_SensingFeild.Children.Add(ch);
                    }
                    else
                    {
                        MessageBox.Show("Select the Coverage!");
                    }
                    break;
                case "_Export Topology":
                    {
                        UiExportTopology top = new UiExportTopology(myNetWork);
                        top.Show();
                        break;
                    }

                case "_Import Topology":
                    {
                        UiImportTopology top = new UiImportTopology(this);
                        top.Show();
                        break;
                    }
            }

        }

       


        public void DisplaySimulationParameters(string deblpaymentMethod)
        {
           
            lbl_coverage.Content = deblpaymentMethod;
            lbl_network_size.Content = myNetWork.Count;

            lbl_Transmitter_Electronics.Content = PublicParamerters.E_elec;
            lbl_fes.Content = PublicParamerters.Efs;
            lbl_Transmit_Amplifier.Content = PublicParamerters.Emp;
            //lbl_data_length_control.Content = PublicParamerters.ControlDataLength;
           // lbl_data_length_routing.Content = PublicParamerters.RoutingDataLength;

            Settings.Default.IsIntialized = true;

            TimerCounter.Interval=TimeSpan.FromSeconds(1); // START count the running time.
            TimerCounter.Start(); // START count the running time.
            TimerCounter.Tick += TimerCounter_Tick;

            //:
            prog_total_energy.Maximum = Convert.ToDouble(myNetWork.Count) * PublicParamerters.BatteryIntialEnergy;
            prog_total_energy.Value = 0;



            lbl_x_active_time.Content = Settings.Default.ActivePeriod + ",";
            lbl_x_queue_time.Content = Settings.Default.QueueTime + ".";
            lbl_x_sleep_time.Content = Settings.Default.SleepPeriod + ",";
            lbl_x_start_up_time.Content = Settings.Default.MacStartUp + ",";
            lbl_intial_energy.Content = Settings.Default.BatteryIntialEnergy;


        }

        public void HideSimulationParameters()
        {

            Settings.Default.StopSimlationWhen = 1000000;

            rounds = 0;
            lbl_rounds.Content = "0";
            lbl_sink_id.Content = "nil";
            lbl_coverage.Content = "nil";
            lbl_network_size.Content = "unknown";
            lbl_sensing_range.Content = "unknown";
            lbl_communication_range.Content = "unknown";
            lbl_Transmitter_Electronics.Content = "unknown";
            lbl_fes.Content = "unknown";
            lbl_Transmit_Amplifier.Content = "unknown";
            lbl_data_length_control.Content = "unknown";
            lbl_data_length_routing.Content = "unknown";
            lbl_density.Content = "0";
            // lbl_control_range.Content = "0";
            //  lbl_zone_width.Content = "0";
            Settings.Default.IsIntialized = false;

            //
            RandomSelectSourceNodesTimer.Stop();
            TimerCounter.Stop();


            lbl_x_active_time.Content = "0";
            lbl_x_queue_time.Content = "0";
            lbl_x_sleep_time.Content = "0";
            lbl_x_start_up_time.Content = "0";
            lbl_intial_energy.Content = "0";

            lbl_par_D.Content = "0";
            lbl_par_Dir.Content = "0";
            lbl_par_H.Content = "0";
            lbl_par_L.Content = "0";
            lbl_par_R.Content = "0";

            lbl_delivData_packets.Content = "0";
            lbl_Number_of_Droped_Packet.Content = "0";
            lbl_num_of_gen_packets.Content = "0"; 
            lbl_sucess_ratio.Content = "0";
            lbl_Wasted_Energy_percentage.Content = "0";
            lbl_update_percentage.Content = "0";
            PublicParamerters.NumberofControlPackets = 0;
            PublicParamerters.EnergyComsumedForControlPackets = 0;
            PublicParamerters.SimulationTime = 0;
        }



        private void EngageMacAndRadioProcol()
        {
            foreach (Sensor sen in myNetWork)
            {
                sen.Mac = new BoXMAC(sen);
                //sen.BatRangesList = PublicParamerters.getRanges();
                sen.Myradar = new Intilization.Radar(sen);
            }
        }


        public void RandomDeplayment()
        {
            PublicParamerters.NumberofNodes = myNetWork.Count;
            NeighborsDiscovery overlappingNodesFinder = new NeighborsDiscovery(myNetWork);
            overlappingNodesFinder.GetOverlappingForAllNodes();

            isCoverageSelected = true;

            DisplaySimulationParameters("Random");

            EngageMacAndRadioProcol();

            TopologyConstractor.BuildToplogy(Canvas_SensingFeild, myNetWork);
            // merged path
            PublicParamerters.clusterRadius = 120;
            PublicParamerters.clusterRadius = PublicParamerters.NetworkSquareSideLength / 4;

            RandomeNumberGenerator.SetSeedFromSystemTime();

            //_ = new Clustering(Canvas_SensingFeild, myNetWork);

            Ring.getCenterOfNetwork();
            foreach (Sensor sen in myNetWork)
            {
                sen.NetworkCenter = PublicParamerters.networkCenter;
                sen.RingNeighborRule = new RingNeighbor(sen);
            }
            Ring.setInitialParameters(PublicParamerters.clusterRadius, 0, Canvas_SensingFeild);
            Ring.startRingConstruction(); // RingNodes are all defined 
        }


        private void Coverage_Click(object sender, RoutedEventArgs e)
        {
            if (!Settings.Default.IsIntialized)
            {
                if (myNetWork.Count > 0)
                {
                    MenuItem item = sender as MenuItem;
                    string Header = item.Name.ToString();
                    switch (Header)
                    {
                        case "btn_Random":
                            {
                                RandomDeplayment();
                            }

                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Please imort the nodes from Db.");
                }
            }
            else
            {
                MessageBox.Show("Network is deployed already. please clear first if you want to re-deploy.");
            }
        }

       
        private void Display_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string Header = item.Name.ToString();
            switch (Header)
            {
                case "_show_id":
                    foreach (Sensor sensro in myNetWork)
                    {
                        if (sensro.lbl_Sensing_ID.Visibility == Visibility.Hidden)
                        {
                            sensro.lbl_Sensing_ID.Visibility = Visibility.Visible;
                          //  sensro.lbl_hops_to_sink.Visibility = Visibility.Visible;

                            Settings.Default.ShowID = true;
                        }
                        else
                        {
                            sensro.lbl_Sensing_ID.Visibility = Visibility.Hidden;
                           // sensro.lbl_hops_to_sink.Visibility = Visibility.Hidden;
                            Settings.Default.ShowID = false;
                        }
                    }
                    break;

                case "_show_sen_range":
                    foreach (Sensor sensro in myNetWork)
                    {
                        if (sensro.Ellipse_Sensing_range.Visibility == Visibility.Hidden)
                        {
                            sensro.Ellipse_Sensing_range.Visibility = Visibility.Visible;
                            Settings.Default.ShowSensingRange = true;
                        }
                        else
                        {
                            sensro.Ellipse_Sensing_range.Visibility = Visibility.Hidden;
                            Settings.Default.ShowSensingRange = false;
                        }
                    }
                    break;
                case "_show_com_range":
                    foreach (Sensor sensro in myNetWork)
                    {
                        if (sensro.Ellipse_Communication_range.Visibility == Visibility.Hidden)
                        {
                            sensro.Ellipse_Communication_range.Visibility = Visibility.Visible;
                            Settings.Default.ShowComunicationRange = true;
                        }
                        else
                        {
                            sensro.Ellipse_Communication_range.Visibility = Visibility.Hidden;
                            Settings.Default.ShowComunicationRange = false;
                        }
                    }
                    break;
              
                case "_show_battrey":
                    foreach (Sensor sensro in myNetWork)
                    {
                        if (sensro.Prog_batteryCapacityNotation.Visibility == Visibility.Hidden)
                        {
                            sensro.Prog_batteryCapacityNotation.Visibility = Visibility.Visible;
                            Settings.Default.ShowBattry = true;
                        }
                        else
                        {
                            sensro.Prog_batteryCapacityNotation.Visibility = Visibility.Hidden;
                            Settings.Default.ShowBattry = false;
                        }
                    }
                    break;
                case "_Show_Routing_Paths":
                    {
                        if (Settings.Default.ShowRoutingPaths == true)
                        {
                            Settings.Default.ShowRoutingPaths = false;
                        }
                        else
                        {
                            Settings.Default.ShowRoutingPaths = true;
                        }
                    }
                    break;

                case "_Show_Packets_animations":
                    {
                        if (Settings.Default.ShowAnimation == true)
                        {
                            Settings.Default.ShowAnimation = false;
                        }
                        else
                        {
                            Settings.Default.ShowAnimation = true;
                        }
                    }
                    break;
            }
        }

        private void btn_other_Menu(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string Header = item.Header.ToString();
            switch (Header)
            {

                //
                case "_Show Dead Node":
                    {
                        if (myNetWork.Count > 0)
                        {
                            /*
                            if (PublicParamerters.DeadNodeList.Count > 0)
                            {
                                UiNetworkLifetimeReport xx = new UiNetworkLifetimeReport();
                                xx.Title = "LORA Lifetime report";
                                xx.dg_grid.ItemsSource = PublicParamerters.DeadNodeList;
                                xx.Show();
                            }
                            else
                                MessageBox.Show("No Dead node.");*/
                        }
                        else
                        {
                            MessageBox.Show("No Network is selected.");
                        }
                    }
                    break;
                case "_InQueue Packets":
                    {
                        List<Packet> QuePackets = new List<Packet>();
                        foreach (Sensor s in myNetWork) QuePackets.AddRange(s.WaitingPacketsQueue);
                        UiRecievedPackertsBySink INqueList  = new UiRecievedPackertsBySink(QuePackets);
                        INqueList.Show();

                    }
                    break;
                case "_Show Resultes":
                    {
                        if (myNetWork.Count > 0)
                        {
                            ExpReport xx = new ExpReport(this);
                            xx.Show();
                        }
                    }
                    break;
                case "_Draw Tree":

                    break;
                case "_Print Info":
                    UIshowSensorsLocations uIlocations = new UIshowSensorsLocations(myNetWork);
                    uIlocations.Show();
                    break;
                case "_Entir Network Routing Log":
                    UiRoutingDetailsLong routingLogs = new ui.UiRoutingDetailsLong(myNetWork);
                    routingLogs.Show();
                    break;
                case "_Log For Each Sensor":

                    break;
                //_Relatives:
                case "_Node Forwarding Probability Distributions":
                    {
                        UiShowLists windsow = new UiShowLists();
                        windsow.Title = "Forwarding Probability Distributions For Each Node";
                        foreach (Sensor source in myNetWork)
                        {
                            
                        }
                        windsow.Show();
                        break;
                    }
                //
                case "_Expermental Results":
                    UIExpermentResults xxxiu = new UIExpermentResults();
                    xxxiu.Show();
                    break;
                case "_Probability Matrix":
                    {
                       
                    }
                    break;
                //
                case "_Packets Paths":
                    UiRecievedPackertsBySink packsInsinkList = new UiRecievedPackertsBySink( PublicParamerters.FinishedRoutedPackets);
                    packsInsinkList.Show();

                    break;
                //
                case "_Random Numbers":

                    List<KeyValuePair<int, double>> rands = new List<KeyValuePair<int, double>>();
                    int index = 0;
                    foreach (Sensor sen in myNetWork)
                    {
                        foreach (RoutingLog log in sen.Logs)
                        {
                            if (log.IsSend)
                            {
                                index++;
                                rands.Add(new KeyValuePair<int, double>(index, log.ForwardingRandomNumber));
                            }
                        }
                    }

                    UiRandomNumberGeneration wndsow = new ui.UiRandomNumberGeneration();
                    wndsow.chart_x.DataContext = rands;
                    wndsow.Show();

                    break;
                case "_Nodes Load":
                    {
                        /*
                        SegmaManager sgManager = new SegmaManager();
                        Sensor sink = PublicParamerters.SinkNode;
                        List<string> Paths = new List<string>();
                        if (sink != null)
                        {
                            foreach (Packet pck in sink.PacketsList)
                            {
                                Paths.Add(pck.Path);
                            }

                        }*/
                        /*
                        sgManager.Filter(Paths);
                        UiShowLists windsow = new UiShowLists();
                        windsow.Title = "Nodes Load";
                        SegmaCollection collectionx = sgManager.GetCollection;
                        foreach (SegmaSource source in collectionx.GetSourcesList)
                        {
                            source.NumberofPacketsGeneratedByMe = myNetWork[source.SourceID].NumberofPacketsGeneratedByMe;
                            ListControl List = new conts.ListControl();
                            List.lbl_title.Content = "Source:" + source.SourceID + " Pks:" + source.NumberofPacketsGeneratedByMe + " Relays:" + source.RelaysCount + " Hops:" + source.HopsSum + " Mean:" + source.Mean + " Variance:" + source.Veriance + " E:" + source.PathsSpread;
                            List.dg_date.ItemsSource = source.GetRelayNodes;
                            windsow.stack_items.Children.Add(List);
                        }
                        windsow.Show();
                      */
                    }
                    break;
                //_Distintc Paths
              
            }
        }

        int rounds = 0;
        int alreadPassedRound = 0;

        private void Btn_rounds_uplinks_mousedown(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.IsIntialized)
            {
                MenuItem slected = sender as MenuItem;
                int rnd = Convert.ToInt16(slected.Header.ToString().Split('_')[1]);
              

                 rounds = rnd;
                 alreadPassedRound = 0;
              
                RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(5);
                RandomSelectSourceNodesTimer.Start();
                RandomSelectSourceNodesTimer.Tick += RoundsPacketsGeneator; 

            }
            else
            {
                MessageBox.Show("Please selete the coverage.Coverage->Random");
            }
        }

        private void RoundsPacketsGeneator(object sender, EventArgs e)
        {
            alreadPassedRound++;
            if (alreadPassedRound <= rounds)
            {
                lbl_rounds.Content = alreadPassedRound;
                foreach (Sensor sen in myNetWork)
                {
                   
                }
            }
            else
            {
                RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0);
                RandomSelectSourceNodesTimer.Stop();
            }
        }



        private void Btn_rounds_downlinks_mousedown(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.IsIntialized)
            {
                // not random:
                MenuItem slected = sender as MenuItem;
                int pktsNumber = Convert.ToInt16(slected.Header.ToString().Split('_')[1]);
                rounds += pktsNumber;
                lbl_rounds.Content = rounds;

                for (int i = 1; i <= pktsNumber; i++)
                {
                    foreach (Sensor sen in myNetWork)
                    {
                      //  PublicParamerters.SinkNode.GenerateControlPacket(sen);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please selete the coverage.Coverage->Random");
            }
        }

        private void BuildTheTree(object sender, RoutedEventArgs e)
        {

        }

        private void tconrol_charts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
        }

        public void ClearExperment()
        {
            try
            {
                PublicParamerters.ResetAndStopSimulation();
               

                top_menu.IsEnabled = true;
                
                Canvas_SensingFeild.Children.Clear();
                if (myNetWork != null)
                    myNetWork.Clear();

                isCoverageSelected = false;


                HideSimulationParameters();
                col_Path_Efficiency.DataContext = null;
                col_Delay.DataContext = null;
                col_EnergyConsumptionForEachNode.DataContext = null;

              

                cols_hops_ditrubtions.DataContext = null;
                lbl_PowersString.Content = "";
                cols_hops_ditrubtions.DataContext = null;
                cols_energy_distribution.DataContext = null;
                cols_delay_distribution.DataContext = null;

               

                

            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }


        private void ben_clear_click(object sender, RoutedEventArgs e)
        {
            TimerCounter.Stop();
            RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0);
            RandomSelectSourceNodesTimer.Stop();

            Settings.Default.IsIntialized = false;

            ClearExperment();

        }



        public object NetworkLifeTime { get; private set; }

        private void tab_network_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           

        }

        private void lbl_show_grid_line_x_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (col_network_X_Gird.ShowGridLines == false) col_network_X_Gird.ShowGridLines = true;
            else col_network_X_Gird.ShowGridLines = false;
        }

        private void lbl_show_grid_line_y_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (col_network_Y_Gird.ShowGridLines == false) col_network_Y_Gird.ShowGridLines = true;
            else col_network_Y_Gird.ShowGridLines = false;
        }



        private void setDisributaions_Click(object sender, RoutedEventArgs e)
        {
           
        }


        private void _set_paramertes_Click(object sender, RoutedEventArgs e)
        {
            /*
            ben_clear_click(sender, e);

            UiMultipleExperments setpa = new UiMultipleExperments(this);
            this.WindowState = WindowState.Minimized;
            setpa.Show();*/

        }



        private void btn_chek_lifetime_Click(object sender, RoutedEventArgs e)
        {
            if (isCoverageSelected)
            {
                this.WindowState = WindowState.Minimized;
                for (int i = 0; ; i++)
                {
                    rounds++;
                    lbl_rounds.Content = rounds;
                    if (!PublicParamerters.IsNetworkDied)
                    {
                        foreach (Sensor sen in myNetWork)
                        {
                            
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please selete the coverage. Coverage->Random");
            }
        }

        private void btn_lifetime_s1_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                /*
                RandomDeplayment();
                UiComputeLifeTime lifewin = new UiComputeLifeTime(this);
                lifewin.Show();
                lifewin.Owner = this;
                top_menu.IsEnabled = false;
                Settings.Default.IsIntialized = true;*/
            }
            else
            {
                MessageBox.Show("File->clear and try agian.");
            }

           
        }


        

        /// <summary>
        /// _Randomly Select Nodes With Distance
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnCon_RandomlySelectNodesWithDistance_Click(object sender, RoutedEventArgs e)
        {
            if (isCoverageSelected)
            {
                if (PublicParamerters.FinishedRoutedPackets.Count == 0)
                {
                    ui.UiSelectNodesWidthDistance win = new UiSelectNodesWidthDistance(this);
                    win.Show();
                }
                else
                {
                    MessageBox.Show("Please clear first: File->Clear!");
                }
            }
            else
            {
                MessageBox.Show("Please selected the Coverage.Coverage->Random");
            }

        }

       

        private void btn_select_sources_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string Header = item.Header.ToString();
            if (Settings.Default.IsIntialized)
            {
                switch (Header)
                {
                    case "1pck/1s":
                        SetPacketRateS1(1);
                        break;
                    case "1pck/2s":
                        SetPacketRateS1(2);
                        break;
                    case "1pck/4s":
                        SetPacketRateS1(4);
                        break;
                    case "1pck/6s":
                        SetPacketRateS1(6);
                        break;
                    case "1pck/8s":
                        SetPacketRateS1(8);
                        break;
                    case "1pck/10s":
                        SetPacketRateS1(10);
                        break;
                    case "1pck/0s(Stop)":
                        SetPacketRateS1(0);
                        break;
                    case "1pck/0.1s":
                        SetPacketRateS1(0.1);
                        break;
                }
            }
            else
            {
                MessageBox.Show("Please select Coverage->Random. then continue.");
            }
        }


        #region Upink Generator //////////////////////////////////////////////////////////////////////
         int UplinkTobeGeneratedPackets = 0;
         int UplinkalreadyGeneratedPackets = 0;

        public void GenerateUplinkPacketsRandomly(int numofPackets)
        {
            UplinkTobeGeneratedPackets = 0;
            UplinkalreadyGeneratedPackets = 0;

            UplinkTobeGeneratedPackets = numofPackets;
            RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0.01);
            RandomSelectSourceNodesTimer.Start();
            RandomSelectSourceNodesTimer.Tick += UplinkPacketsGenerate_Tirk;
        }
         
        private void UplinkPacketsGenerate_Tirk(object sender, EventArgs e)
        {
            UplinkalreadyGeneratedPackets++;
            if (UplinkalreadyGeneratedPackets <= UplinkTobeGeneratedPackets)
            {
                int index = 1 + Convert.ToInt16(UnformRandomNumberGenerator.GetUniform(PublicParamerters.NumberofNodes - 2));
               // myNetWork[index].GenerateDataPacket();
            }
            else
            {
                RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0);
                RandomSelectSourceNodesTimer.Stop();
                UplinkalreadyGeneratedPackets = 0;
                UplinkTobeGeneratedPackets = 0;
            }
        }

        private void btn_uplLINK_send_numbr_of_packets(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string Header = item.Header.ToString();
            if (Settings.Default.IsIntialized)
            {
                int Header_int = Convert.ToInt16(Header);
                GenerateUplinkPacketsRandomly(Header_int);
            }
            else
            {
                MessageBox.Show("Please select Coverage->Random. then continue.");
            }
        }

        #endregion ///////////////////////////////////////////////////////////////


      


        int DownlinkTobeGenerated = 0;
        int DownlinkAlreadyGenerated = 0;

        public void GenerateDownLinkPacketRandomly(int numofpackets)
        {
            DownlinkTobeGenerated = 0;
            DownlinkAlreadyGenerated = 0;

            DownlinkTobeGenerated = numofpackets;
            RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0.01);
            RandomSelectSourceNodesTimer.Start();
            RandomSelectSourceNodesTimer.Tick += DownLINKRandomSentAnumberofPackets;
        }

        private void btn_DOWNN_send_numbr_of_packets(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string Header = item.Header.ToString();
            if (Settings.Default.IsIntialized)
            {
                int Header_int = Convert.ToInt16(Header);
                GenerateDownLinkPacketRandomly(Header_int);

            }
            else
            {
                MessageBox.Show("Please select Coverage->Random. then continue.");
            }
        }

        private void DownLINKRandomSentAnumberofPackets(object sender, EventArgs e)
        {
            DownlinkAlreadyGenerated++;
            if (DownlinkAlreadyGenerated <= DownlinkTobeGenerated)
            {
                int index = Convert.ToInt16(UnformRandomNumberGenerator.GetUniform(PublicParamerters.NumberofNodes - 2));
                Sensor EndNode = myNetWork[index];
               // PublicParamerters.SinkNode.GenerateControlPacket(EndNode);
            }
            else
            {
                RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0);
                RandomSelectSourceNodesTimer.Stop();
                DownlinkAlreadyGenerated = 0;
                DownlinkTobeGenerated = 0;
            }
        }


        private void btn_simTime_Click(object sender, RoutedEventArgs e)
        {
            /*
            MenuItem item = sender as MenuItem;
            string Header = item.Header.ToString();
            if (Settings.Default.IsIntialized)
            {
                 = Convert.ToInt32(Header.ToString());
               
            }
            else
            {
                MessageBox.Show("Please select Coverage->Random. then continue.");
            }*/
        }

        private void Btn_comuputeEnergyCon_withinTime_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (Settings.Default.IsIntialized)
            {
                MessageBox.Show("File->clear and try agian.");
            }
            else
            {
                PacketRate = "";
                stopSimlationWhen = 0;
                UISetParEnerConsum con = new UISetParEnerConsum(this);
                con.Owner = this;
                con.Show();
                top_menu.IsEnabled = false;
            }*/
        }

        private void Canvas_SensingFeild_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {

            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            if (e.Delta > 0)
            {
                _slider.Value += 0.1;
            }
            else if (e.Delta < 0)
            {
                _slider.Value -= 0.1;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _slider.Value = Settings.Default.SliderValue;
            Settings.Default.IsIntialized = false;
        }

       

        private void btn_EnergyConsumptionMergedvsIndependent_Click(object sender, RoutedEventArgs e)
        {
            if (myNetWork.Count == 0)
            {
                Experments c = new Experments(this);
                c.Show();
            }
            else
            {
                MessageBox.Show("plz clear the network first");
            }
        }


        private void ComputeAveragDistance()
        {
            /*
            RandomSelectSourceNodesTimer.Interval = TimeSpan.FromMilliseconds(0.00001);
            RandomSelectSourceNodesTimer.Tick += DistanceComputerEventor;
            RandomSelectSourceNodesTimer.Start();*/
        }

        

        private void DistanceComputerEventor(object sender, EventArgs e)
        {
           
            /*
            // random point:
            double x = RandomvariableStream.UniformRandomVariable.GetDoubleValue(0, PublicParamerters.NetworkSquareSideLength);
            double y = RandomvariableStream.UniformRandomVariable.GetDoubleValue(0, PublicParamerters.NetworkSquareSideLength);
            Point random = new Point(x, y);

            
            PointCount += 1;
            double DisToD= testAverageDistance.FindDistanceToDiagonal(random);
            double DisToR= testAverageDistance.FindDistanceToRing(random);
            SumdisD += DisToD;
            SumdisR += DisToR;
            AverdisD = SumdisD / PointCount;
            AverdisR = SumdisR / PointCount;
            Console.WriteLine(PointCount+ "> ("+random +") Diagonal =" + AverdisD + " Ring=" + AverdisR);
            
            */

            /*
            double disInner = testAverageDistance.InnerAverageDistance(random);
            if(disInner>0)
            {
                innerSum += disInner;
                innerCount += 1;
                InnerAverag = innerSum / innerCount;

                Console.WriteLine(innerCount + "> (" + random + ") Inner Dis =" + InnerAverag );
            }
            */



        }

        private void btn_test_average_hops(object sender, RoutedEventArgs e)
        {
            RandomSelectSourceNodesTimer.Interval = TimeSpan.FromSeconds(0.00001);
            RandomSelectSourceNodesTimer.Tick +=TestAverageNumberHopsToDiagonal; 
            RandomSelectSourceNodesTimer.Start();

        }

        private void TestAverageNumberHopsToDiagonal(object sender, EventArgs e)
        {
            
        }
        public double StreenTimes = 1;

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double sctim = StreenTimes / 10;
            double x = _slider.Value;
            if (x <= sctim)
            {
                x = sctim;
                Settings.Default.SliderValue = x;
                Settings.Default.Save();
            }
            var scaler = Canvas_SensingFeild.LayoutTransform as ScaleTransform;
            Canvas_SensingFeild.LayoutTransform = new ScaleTransform(x, x, SystemParameters.FullPrimaryScreenWidth / 2, SystemParameters.FullPrimaryScreenHeight / 2);
            lbl_zome_percentage.Text = (x * 100).ToString() + "%";


            Settings.Default.SliderValue = x;
            Settings.Default.Save();
        }
    }
}



           
