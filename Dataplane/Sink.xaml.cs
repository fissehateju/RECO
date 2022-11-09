using RR.Computations;
using RR.Dataplane.PacketRouter;
using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Intilization;
using RR.Comuting.Routing;
using RR.Models.Mobility;
using RR.Models.Charging;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using RR.Cluster;

namespace RR.Dataplane
{

    public class AgentsRow
    {
        public Sink Sink { get; set; }
        public Sensor MainAgentNode { get; set; }
        public Dictionary<Sensor, TimeSpan> agentAtTimeT = new Dictionary<Sensor, TimeSpan>();
        public TimeSpan returnTime { get; set; }
        public Point RingAccessPointDestination { get; set; }
        public AgentsRow(Sink sink, Sensor mainAgentNode, Dictionary<Sensor, TimeSpan> agentAtTimeT, TimeSpan Returnn)
        {
            Sink = sink;
            MainAgentNode = mainAgentNode;
            this.agentAtTimeT = agentAtTimeT;
            returnTime = Returnn;
        }
    }


    /// <summary>
    /// Interaction logic for Sink.xaml
    /// </summary>
    public partial class Sink : UserControl
    {

        public BaseStation Bstation { get; set; }
        public TourTask myTask;
        public AgentsRow agentsRow;
        public TourAudit Tour;
        public List<Packet> NewReqArrivals = new List<Packet>();
        public List<NetCluster> netClusters = new List<NetCluster>();
        private MoveToNextPosition RandomWaypoint { get; set; }        
        public bool isFree { get; set; }
        public bool isReturning { get; set; }
        public bool isTraveling { get; set; } 
        //
        private double speed { get; set; }
        private Point next_position { get; set; }
        private Point destination { get; set; }
        private int TimeToNextSojoun { get; set; }
        public double ResidualEnergy { get; set; }
        public DateTime TourStated { get; set; }
        public int ID
        {
            get;
            set;
        }
        public Sink()
        {
            InitializeComponent();
            int id = PublicParamerters.SinkCount+1;
            lbl_sink_id.Text = "C"+id.ToString();
            //lbl_sink_id.Text = "MC";
            lbl_sink_id.FontWeight = FontWeights.Bold;
            ID = id;
            netClusters = PublicParamerters.listOfRegs;
            isFree = true;
            PublicParamerters.MainWindow.mySinks.Add(this);
            Dispatcher.Invoke(() => Mobile_CS.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
            ResidualEnergy = PublicParamerters.BatteryIntialEnergyForMC;

            Position = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);

            //RandomAgentAtInitialization();


            if (Settings.Default.IsMobileSink)
            {
                //SetMobility();
            }

            Width = 16;
            Height = 16;
            DateTime curr = DateTime.Now;
            TimeSpan timeSpan = curr.Subtract(curr.Date);
            agentsRow = new AgentsRow(this, PublicParamerters.MainWindow.myNetWork[0], new Dictionary<Sensor, TimeSpan>(), timeSpan.Add(TimeSpan.FromMinutes(4)));

            ReportMyPosition();
        }
        
       public void StopMobility()
        {
            RandomWaypoint.StopMoving();
        }
        /// <summary>
        /// set the mobility model
        /// </summary>
        public void SetMobility()
        {
            RandomWaypoint = new MoveToNextPosition(this, PublicParamerters.NetworkSquareSideLength, PublicParamerters.NetworkSquareSideLength, this);
                       
            if (myTask != null && myTask.Requests.Count > 0)
            {
                double MeToDes = Operations.DistanceBetweenTwoPoints(CenterLocation, myTask.Requests.Peek().Source.CenterLocation);
                double DesToBs = Operations.DistanceBetweenTwoPoints(myTask.Requests.Peek().Source.CenterLocation, Bstation.Position);
               
                if (ResidualEnergy - (PublicParamerters.E_MCmove * MeToDes) - (PublicParamerters.E_MCmove * DesToBs) < 100)
                {
                    while (myTask.Requests.Count > 0)
                    {
                        PublicParamerters.TotalNumRequests -= 1;

                        Bstation.SaveToQueue(myTask.Requests.Dequeue());
                    }
                    SetMobility(); // go back to check conditions again and return to Basesatation
                }
                else if (MeToDes > 3)
                {
                    ////// compute travel cost
                    ///
                    Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));
                   
                    PublicParamerters.TotalDistance_CoveredMC += MeToDes;
                    PublicParamerters.TotalEnergyForTravelMC += PublicParamerters.E_MCmove * MeToDes;
                    ResidualEnergy -= PublicParamerters.E_MCmove * MeToDes;

                    RandomWaypoint.StartMove(myTask.Requests.Dequeue());
                }
                else
                {
                    myTask.Requests.Peek().Source.ResidualEnergy = PublicParamerters.BatteryIntialEnergy;
                    myTask.Requests.Dequeue();
                    SetMobility();
                }
            }
            else
            {                

                if (NewReqArrivals.Count > 0)
                {
                    //reOrdering();
                    //SetMobility();
                }
                //else if (ResidualEnergy > 1000)
                //{
                //    traveling = false;
                //    Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
                //    Bstation.TriggerCharger();
                //}
                else
                {                   

                    Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));

                    // return to the Base station
                    //// compute travel cost
                    destination = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);
                    double MeToBs = Operations.DistanceBetweenTwoPoints(CenterLocation, destination);
                    PublicParamerters.TotalDistance_CoveredMC += MeToBs;
                    PublicParamerters.TotalEnergyForTravelMC += PublicParamerters.E_MCmove * MeToBs;
                    ResidualEnergy -= PublicParamerters.E_MCmove * MeToBs;
                    isReturning = true;
                    RandomWaypoint.StartMove(null);
                }          
            }           
        }
      
        public void initiateCharger(TourTask task)
        {
            myTask = task;
            isFree = false;
            isReturning = false;
            isTraveling = true;

            System.Console.WriteLine("\n ========== Sink {0} ==========", ID);
            foreach (Packet pack in myTask.Requests)
            {
                System.Console.Write("Sensors {0} , ", pack.Source.ID);
            }
            System.Console.WriteLine("\n =======================");

            DetermineSojournAndShare();

            TourStated = DateTime.Now;
            SetMobility();           

        }

        public TimeSpan ComputeTime(double dist)
        {
            speed = 5;
            TimeSpan timet = TimeSpan.FromSeconds(dist/speed); // + charging time tobe calculated
            return timet;
        }

        private void DetermineSojournAndShare()
        {
            double Distance = 0.0;

            DateTime currentdate = DateTime.Now;
            int s = currentdate.Second;
            int m = currentdate.Minute;
            int h = currentdate.Hour;
            int d = currentdate.Day;
            TimeSpan currentTime = new TimeSpan(h, m, s);
            currentTime = currentdate.Subtract(currentdate.Date); // also possible

            Dictionary<Sensor, TimeSpan> keyValuePairs = new Dictionary<Sensor, TimeSpan>();
            Point chargerNewLoc = CenterLocation;

            foreach (Packet pack in myTask.Requests)
            {
                Distance += Operations.DistanceBetweenTwoPoints(chargerNewLoc, pack.Source.CenterLocation);
                keyValuePairs[pack.Source] = currentTime.Add(ComputeTime(Distance));
                chargerNewLoc = pack.Source.CenterLocation;

                //System.Console.WriteLine("\n =======================");
                //Console.WriteLine("Charger at Sensor {0} at Time {1}", pack.Source.ID, keyValuePairs[pack.Source].ToString());
            }
            //System.Console.WriteLine("\n =======================");

            // time to return to the base station
            Distance += Operations.DistanceBetweenTwoPoints(chargerNewLoc, PublicParamerters.MainWindow.myNetWork[0].CenterLocation);
            TimeSpan ReturningAt = currentTime.Add(ComputeTime(Distance));

            agentsRow = new AgentsRow(this, PublicParamerters.MainWindow.myNetWork[0], keyValuePairs, ReturningAt);

            //new ShareSinkPosition(agentsRow, netClusters);
            ReportMyPosition();
        }

        public void ChargingAlert()
        {
            Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
        }
        public void BreakTime()
        {
            Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
        }

        public void StopMoving()
        {
            Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
            myTask.Requests.Clear();
            if (RandomWaypoint != null) RandomWaypoint.StopOperation();
        }

        public void PrintTourInfo()
        {

            //System.Console.WriteLine("\n #####################################");
            //System.Console.WriteLine(" _______________ # of Tasks {0} ______________", PublicParamerters.TotalNumofTasks);
            //System.Console.WriteLine("The Total distance coverred     : {0}", PublicParamerters.TotalDistance_CoveredMC);
            //System.Console.WriteLine("The MC's Travel Energy          : {0}", PublicParamerters.TotalEnergyForTravelMC);
            //System.Console.WriteLine("The Total transfered Energy     : {0}", PublicParamerters.TotalTransferredEnergy);
            //System.Console.WriteLine("The Total # of charged sensors  : {0}", PublicParamerters.TotalNumChargedSensors);
            //System.Console.WriteLine("The # of Data Collected by Sink : {0}", PublicParamerters.TotalNumDataCollectedMC);
            //System.Console.WriteLine("The # of Data Collected by BS   : {0}", PublicParamerters.TotalNumDatacollectedBS);
            //System.Console.WriteLine("MC Collected Data Ratio         : {0}", PublicParamerters.MCCollectedDataPercentage);
            //System.Console.WriteLine("Total Collected Data            : {0}", PublicParamerters.NumberofDeliveredDataPacket);

            //System.Console.WriteLine("Average Delivery Delay    : {0} ", PublicParamerters.DataCollDelaysInSecond / PublicParamerters.NumberofDeliveredPacket);

            //System.Console.WriteLine("Average Charging Delay    : {0} ", PublicParamerters.ChargingDelayInSecond / PublicParamerters.TotalNumChargedSensors);
            //System.Console.WriteLine("Service Time              : {0} ", PublicParamerters.ServiceTimeInSecond);
            //System.Console.WriteLine("\n #####################################");

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
                Point p = new Point(x, y);
                return p;
            }
        }

        /// <summary>
        /// report the new position of the sink.
        /// </summary>
        public void ReportMyPosition()
        {
            if (agentsRow != null)
            {
                //if (MySinksAgentsRow.d != null)
                //{

                    ReportSinkPositionMessage rep = new ReportSinkPositionMessage(agentsRow);
                //}
            }
        }

        public SinksAgentsRow MySinksAgentsRow { get; set; }


        public Point getDestinationForRingAccess()
        {
            Point destination = new Point();
            double dist = double.MaxValue;
            foreach(Sensor sen in PublicParamerters.MainWindow.myNetWork)
            {
                double DSen = Operations.DistanceBetweenTwoPoints(CenterLocation, sen.CenterLocation);
                if(sen.RingNodesRule.isRingNode && DSen < dist)
                {
                    dist = DSen;
                    destination = sen.CenterLocation;
                }
            }
            return destination;
        }

        /// <summary>
        /// intailization:
        /// </summary>
        public void RandomAgentAtInitialization()
        {
            int count = PublicParamerters.MainWindow.myNetWork.Count;
            // select random sensor and set that as my agent.
            if (count > 0)
            {
                int index;
                if (Settings.Default.SinksStartAtNetworkCenter)
                {
                    index = 0;
                }
                else
                {
                    // select random:
                    bool agentisHighTier = false;
                    do
                    {
                        index = RandomvariableStream.UniformRandomVariable.GetIntValue(0, count - 1);
                        agentisHighTier = PublicParamerters.MainWindow.myNetWork[index].IsHightierNode;
                    } while (agentisHighTier);
                }
                index = 0;
                Sensor agent = PublicParamerters.MainWindow.myNetWork[index];
                SinksAgentsRow sinksAgentsRow = new SinksAgentsRow();
                sinksAgentsRow.AgentNode = agent;
                sinksAgentsRow.Sink = this;
                agent.AddNewAGent(sinksAgentsRow);
                MySinksAgentsRow = sinksAgentsRow;
                Position = agent.CenterLocation;
                ReportMyPosition();
            }
        }

        /// <summary>
        /// check if the distance almost out.
        /// 
        /// </summary>
        /// <returns></returns>
        public bool AlmostOutOfMyAgent()
        {
            if (MySinksAgentsRow != null)
            {
                if (MySinksAgentsRow.AgentNode != null)
                {
                    double dis = Operations.DistanceBetweenTwoPoints(CenterLocation, MySinksAgentsRow.AgentNode.CenterLocation);
                    if (dis >= (PublicParamerters.CommunicationRangeRadius * 0.7))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReselectAgentNode()
        {
            double mindis = double.MaxValue;
            Sensor newAgent = null;
            foreach(Sensor sj in MySinksAgentsRow.AgentNode.NeighborsTable)
            {
                double curDis = Operations.DistanceBetweenTwoPoints(CenterLocation, sj.CenterLocation);
                if (curDis < PublicParamerters.CommunicationRangeRadius)
                {
                    if (curDis < mindis)
                    {
                        mindis = curDis;
                        newAgent = sj;
                    }
                }
            }

            // found:
            if (newAgent != null)
            {
                // Prev one:
                Sensor prevAgent = MySinksAgentsRow.AgentNode;
                prevAgent.AgentStartFollowupMechansim(ID, newAgent);
                bool preRemoved = prevAgent.RemoveFromAgent(MySinksAgentsRow);

                if (preRemoved)
                {
                    // set the new one:
                    SinksAgentsRow newsinksAgentsRow = new SinksAgentsRow();
                    newsinksAgentsRow.AgentNode = newAgent;
                    newsinksAgentsRow.Sink = this;
                    newAgent.AddNewAGent(newsinksAgentsRow);
                    MySinksAgentsRow = newsinksAgentsRow;
                    //Console.WriteLine("Sink:" + ID + " reselected " + newAgent.ID + " as new agent. Prev. Agent:" + prevAgent.ID);
                    ReportMyPosition();
                }
                else
                {
                    //MessageBox.Show("sink->ReselectAgentNode()-> preRemoved=false.");
                }
            }
            else
            {
                //Console.WriteLine("Sink:" + ID + "Out of network and has no agent.");
                // use the same prev agent:
                Position = MySinksAgentsRow.AgentNode.CenterLocation;
                
            }
        }



        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            bool imOut = false; // AlmostOutOfMyAgent();
            if (imOut)
            {
                // reselect:
                ReselectAgentNode();
            }
            else
            {
                // do no thing.
            }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            ToolTip = new Label() { Content = ResidualEnergy.ToString() };
        }
    }
}
