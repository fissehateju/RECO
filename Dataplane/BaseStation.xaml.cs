using RR.Computations;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.Routing;
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
using RR.Dataplane.NOS;
using RR.Cluster;

namespace RR.Dataplane
{
    /// <summary>
    /// a Task will be created here 
    /// </summary>
    public class TourTask:IDisposable
    {
        public Queue<Packet> Requests = new Queue<Packet>();
        public bool isEmergency = false;
        public double averageTransRate 
        { 
            get 
            { 
                double avg = 0;
                foreach (Packet packet in Requests)
                {
                    avg += packet.dataTransmiting_Rate;
                }
                return avg / Requests.Count;
            } 
        }
        public double MinRemainingEnergyPer
        {
            get
            {
                double remain = 100;
                foreach (Packet packet in Requests)
                {
                    if (packet.remainingEnergyPercentage < remain)
                    {
                        remain = packet.remainingEnergyPercentage;
                    }   
                }
                return remain;
            }
        }
        public int TID { get; set; }       

        //public Task()
        //{
        //    System.Console.WriteLine(" Constructor !!! ");
        //}
        //~Task()
        //{
        //    System.Console.WriteLine(" Destructor !!! ");
        //}
        public void displaySensorsinTask()
        {
            System.Console.WriteLine("  ");
            System.Console.WriteLine("Task {0}", TID);
            System.Console.WriteLine("=======================");
            foreach (Packet pack in Requests)
            {
                System.Console.Write("Sensors {0} , ", pack.Source.ID);               
            }
            System.Console.WriteLine("\n=======================");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }


    public class TourTaskSort : IComparer<TourTask>
    {
        public int Compare(TourTask task1, TourTask task2)
        {
            return task1.MinRemainingEnergyPer.CompareTo(task2.MinRemainingEnergyPer);
        }
    }

    public class RequesSort : IComparer<Packet>
    {
        public int Compare(Packet pak2, Packet pak1)
        {
            return pak1.Source.ResidualEnergy.CompareTo(pak2.Source.ResidualEnergy);
        }
    }

    public class TerritorySort : IComparer<ClusteringForRECO>
    {
        public int Compare(ClusteringForRECO ter2, ClusteringForRECO ter1)
        {
            return ter1.AverageRemainingEnergy.CompareTo(ter2.AverageRemainingEnergy);
        }
    }



    /// <summary>
    /// Interaction logic for BaseStation.xaml
    /// </summary>
    /// 
    public partial class BaseStation : UserControl
    {
        public int ID { get; set; }
        public DispatcherTimer timer_checkingMC= new DispatcherTimer(); 
        public DispatcherTimer QueuTimer = new DispatcherTimer();
        
        public List<Packet> Arriving_reqPackets = new List<Packet>();
        public List<Packet> Emerg_temp = new List<Packet>();
        public List<ClusteringForRECO> territories = new List<ClusteringForRECO>();

        public Queue<Packet> SortedreqPackets = new Queue<Packet>();
        public List<TourTask> TourTasks = new List<TourTask>();
        public List<Packet> Emerg_req = new List<Packet>();
        //public List<int> firstKreq = new List<int>();
        public Charger charger { get; set; }
        public Sink sink { get; set; }
       
        private RequestScheduling RequestScheduler;

        private bool isTerritorydefind { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public BaseStation()
        {
            InitializeComponent();
            int id = 1;
            //lbl_BaseStation_id.Text = id.ToString();
            ID = id;                  
            Width = 20;
            Height = 30;
            setPosition();

            SensingField = PublicParamerters.MainWindow.Canvas_SensingFeild;
            netNodes = PublicParamerters.MainWindow.myNetWork;
            canvasHeight = SensingField.Height;
            canvasWidth = SensingField.Width;           

            PublicParamerters.MainWindow.myBstation = this;
            
            PublicParamerters.TotalNumTerritorys = territories.Count;

            timer_checkingMC.Interval = TimeSpan.FromSeconds(10);
            timer_checkingMC.Start();
            timer_checkingMC.Tick += TriggerCharger;


        }

        public void StopScheduling()
        {
            Arriving_reqPackets.Clear();

            timer_checkingMC.Stop();
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

        private bool defineTerritories(int numReq)
        {
            if (numReq > Netdenstiy())
            {
                Partitioning_Network_area();                   
                drawNewBorder();
                foreach (var packet in Arriving_reqPackets)
                {
                    comingFrom(packet);
                }              
                return true;
            }
            return false;
        }

        private int Netdenstiy()
        {
            int num = Convert.ToInt16 ((PublicParamerters.MainWindow.myNetWork.Count * Math.PI * Math.Pow(PublicParamerters.CommunicationRangeRadius, 2)) /
                Math.Pow(PublicParamerters.NetworkSquareSideLength, 2));
            return num;
        }
        public void setPosition ()
        {
            //Point pt = PublicParamerters.MainWindow.myNetWork[1].CenterLocation;
            Position = new Point(PublicParamerters.NetworkSquareSideLength / 2 - Width, PublicParamerters.NetworkSquareSideLength / 2 - Height);
        }

        private void comingFrom(Packet pack)
        {
            foreach (var terr in territories)
            {
                if (terr.MemberNodes.Contains(pack.Source))
                {
                    terr.Requests.Add(pack);
                    break;
                }
            }
        }
        private void ReshufflingTerritories()
        {
            territories.Sort(new TerritorySort()); // this sorts from large to small
            territories.Reverse();
        }
      
        public void SaveToQueue(Packet packet)
        {
            PublicParamerters.TotalNumRequests += 1;
            if (packet.isEmergencyAlert && !Emerg_req.Contains(packet))
            {
                Emerg_req.Add(packet);
            }
            else
            {
                Arriving_reqPackets.Add(packet);
                if (isTerritorydefind)
                {
                    comingFrom(packet);                   
                }
                else
                {
                    isTerritorydefind = defineTerritories(PublicParamerters.TotalNumRequests);
                }
                
            }            
 
            PublicParamerters.TotalWaitingTimeRechargeQueue += 1; // total;           
            packet.Recharg_WaitingTimes += 1;

            //TriggerCharger();
        }

        public void TriggerCharger(object sender, EventArgs e)
        {

            foreach(var mySink in PublicParamerters.MainWindow.mySinks)
            {
                sink = mySink;
                if (sink.isFree)
                {
                    if (Emerg_req.Count > 0)
                    {
                        Scheduling(null, Emerg_req);
                    }
                    else if (!isTerritorydefind && Arriving_reqPackets.Count >= Netdenstiy())
                    {
                        Scheduling(null, Arriving_reqPackets);
                    }
                    else
                    {
                        ReshufflingTerritories(); // reshuffle based on the maximum data transmission rate in the territories

                        foreach (var terr in territories)
                        {
                            if (terr.Requests.Count >= Netdenstiy() || terr.Requests.Count > terr.MemberNodes.Count / 2) 
                            {
                                Scheduling(terr, terr.Requests);
                                break;
                            }
                        }
                    }
                }              
            }
        }
        
        public void Scheduling(ClusteringForRECO Terry, List<Packet> Requests)
        {
            if (Terry == null && Requests[0].isEmergencyAlert)
            {
                Requests.Sort(new RequesSort()); // order big to small
                Requests.Reverse();
                while (Requests.Count > 0)
                {
                    SortedreqPackets.Enqueue(Requests[0]);
                    Requests.RemoveAt(0); // this removes the request in the territory
                }
            }
            else
            {
                RequestScheduler = new RequestScheduling(this, sink, Requests);
                SortedreqPackets = RequestScheduler.reOrdering(Terry);
            }

            foreach (Packet pp in SortedreqPackets)
            {
                if(Terry != null)
                {
                    Terry.Requests.Remove(pp);
                }
                Arriving_reqPackets.Remove(pp);
                Emerg_req.Remove(pp);
            }

            createTasks(Terry);

            forwardingTasks();
        }

        private void forwardingTasks()
        {
            if (TourTasks.Count > 0)
            {
                sink.Bstation = this;
                sink.initiateCharger(TourTasks[0]);
                TourTasks.RemoveAt(0);
            }
            else
            {
                //System.Console.WriteLine("Charger is busy. # of waiting Tasks {0}", tasks.Count); 
                return;
            }
        }       

        //// creating task
        ///
        public void createTasks(ClusteringForRECO tery)
        {

            if (SortedreqPackets.Count > 0)//ReqCounter > 0)
            {
               var task = new TourTask();
                PublicParamerters.TotalNumofTasks += 1;
                task.TID = PublicParamerters.TotalNumofTasks;

                if (tery == null)
                {
                    task.isEmergency = true;
                }

                while (SortedreqPackets.Count > 0) // && task.Requests.Count < task.numOfRequests); 
                {
                    task.Requests.Enqueue(SortedreqPackets.Dequeue());
                }
                // add task to Tasks Queue
                TourTasks.Add(task);
                //task.displaySensorsinTask();
                if (TourTasks.Count > 1)
                {
                    TourTasks.Sort(new TourTaskSort());
                }
            }
        }

        private Canvas SensingField;
        private double canvasHeight { get; set; }
        private double canvasWidth { get; set; }
        private double CellRadius { get; set; }
        private double minClusterRad = PublicParamerters.CommunicationRangeRadius * 2;

        private Point TopLeft = new Point();
        private Point TopRight = new Point();
        private Point BottomLeft = new Point();
        private Point BottomRight = new Point();
        private static List<Line> RegBorders = new List<Line>();
        private static List<Sensor> netNodes;
        private void Partitioning_Network_area()
        {
            double numOfReghorizon = 2; // Math.Floor(canvasWidth / minClusterRad);
            double numOfRegvert = 2; // Math.Floor(canvasHeight / minClusterRad);

            ClusteringForRECO part;
            int id = 1;
            for (int i = 0; i < numOfReghorizon; i++)
            {
                for (int j = 0; j < numOfRegvert; j++)
                {

                    TopLeft = new Point(i * (canvasWidth / numOfReghorizon), j * (canvasHeight / numOfRegvert));
                    TopRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), j * (canvasHeight / numOfRegvert));
                    BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), (j + 1) * (canvasHeight / numOfRegvert));
                    BottomRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), (j + 1) * (canvasHeight / numOfRegvert));

                    if (i == numOfReghorizon - 1)
                    {
                        TopRight = new Point(PublicParamerters.mostright + 10, j * (canvasHeight / numOfRegvert));
                        BottomRight = new Point(PublicParamerters.mostright + 10, (j + 1) * (canvasHeight / numOfRegvert));
                    }
                    if (j == numOfRegvert - 1)
                    {
                        BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), PublicParamerters.mostbottom + 10);
                        BottomRight = new Point((i + 1) * (canvasWidth / numOfReghorizon), PublicParamerters.mostbottom + 10);
                    }
                    if (i == numOfReghorizon - 1 && j == numOfRegvert - 1)
                    {
                        TopRight = new Point(PublicParamerters.mostright + 10, j * (canvasHeight / numOfRegvert));
                        BottomLeft = new Point(i * (canvasWidth / numOfReghorizon), PublicParamerters.mostbottom + 10);
                        BottomRight = new Point(PublicParamerters.mostright + 10, PublicParamerters.mostbottom + 10);
                    }

                    part = new ClusteringForRECO(id, TopLeft, BottomRight);
                    part.width = Operations.DistanceBetweenTwoPoints(TopLeft, TopRight);
                    part.height = Operations.DistanceBetweenTwoPoints(TopLeft, BottomLeft);

                    id += 1;

                    territories.Add(part);

                }
            }

            // Defining the cluster member nodes
            DefineTheClusterElemets();

        }
        private void DefineTheClusterElemets()
        {
            foreach (var Reg in territories)
            {
                foreach (Sensor sen in netNodes)
                {
                    if (sen.CenterLocation.X >= Reg.TopLeft.X && sen.CenterLocation.Y >= Reg.TopLeft.Y &&
                        sen.CenterLocation.X < Reg.BottomRight.X && sen.CenterLocation.Y < Reg.BottomRight.Y)
                    {
                        Reg.MemberNodes.Add(sen);
                    }
                }
            }
        }

        private Line linee;
        private void drawNewBorder()
        {
            int bWeight;
            foreach (var regin in territories)
            {
                if (regin.Id > 4)
                {
                    bWeight = regin.Id % 4 + 1;
                }
                else
                {
                    bWeight = regin.Id;
                }
                bWeight = 1;

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X;
                linee.Y1 = regin.TopLeft.Y;
                linee.X2 = regin.TopLeft.X + regin.width;
                linee.Y2 = regin.TopLeft.Y;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X;
                linee.Y1 = regin.TopLeft.Y;
                linee.X2 = regin.TopLeft.X;
                linee.Y2 = regin.TopLeft.Y + regin.height;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X;
                linee.Y1 = regin.TopLeft.Y + regin.height;
                linee.X2 = regin.TopLeft.X + regin.width;
                linee.Y2 = regin.TopLeft.Y + regin.height;
                SensingField.Children.Add(linee);

                linee = new Line();
                linee.Fill = Brushes.Blue;
                linee.Stroke = Brushes.Blue;
                linee.StrokeThickness = bWeight;
                linee.X1 = regin.TopLeft.X + regin.width;
                linee.Y1 = regin.TopLeft.Y;
                linee.X2 = regin.TopLeft.X + regin.width;
                linee.Y2 = regin.TopLeft.Y + regin.height;
                SensingField.Children.Add(linee);
            }
        }

    }
}
