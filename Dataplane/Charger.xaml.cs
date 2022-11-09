using RR.Computations;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.Routing;
using RR.Models.Mobility;
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
using System.Threading;
using RR.Dataplane.NOS;

namespace RR.Dataplane
{
    public class TourAudit
    {
        public int TourID { get; set; }
        public TimeSpan TotalTravelTime = new TimeSpan();
        public int taskID { get; set; }
    }
    /// <summary>
    /// Interaction logic for Charger.xaml
    /// </summary>
    public partial class Charger : UserControl
    {
        private DispatcherTimer timer_move = new DispatcherTimer(); 
        private DispatcherTimer timer_Recharging = new DispatcherTimer();
        private DispatcherTimer timer_GoingBack = new DispatcherTimer();

        public static Canvas mycanvas = PublicParamerters.MainWindow.Canvas_SensingFeild;
        public BaseStation Bstation { get; set; }
        public TourTask myTask;
        public TourAudit Tour;
        public List<Packet> packets_in_order = new List<Packet>();
        public int ID { get; set; }
        public bool isFree { get; set; }
        public bool isgoingBack { get; set; }
        
        //
        private double speed { get; set; }  
        private Point next_position { get; set; }
        private Point destination { get; set; }
        private int TimeToNextSojoun { get; set; }
        public double ResidualEnergy { get; set; }
        public TimeSpan TravelTime = new TimeSpan();
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

        public Charger()
        {
            InitializeComponent();
            int id = 1;
            //lbl_charger_id.Text = id.ToString();
            Width = 15;
            Height = 15;
            ID = id;      
            isFree = true;
            ResidualEnergy = PublicParamerters.BatteryIntialEnergyForMC;
            Dispatcher.Invoke(() => mobile_charger.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
            setPosition();

            PublicParamerters.MainWindow.MCharger = this;

        }
        ///
        public void StopCharging()
        {
            timer_move.Stop();
            timer_Recharging.Stop();
            timer_GoingBack.Stop();

            Position = Bstation.Position;
        }
        
        /// <summary>
        /// Mobile charger tour starts here.
        /// </summary>
        /// <param name="task"></param>
        /// 
        public void initiateCharger(TourTask task)
        {
            myTask = task;
            isFree = false;

            Tour = new TourAudit();

            Tour.taskID = myTask.TID;
            var startTime = DateTime.Now;

            startTour();
          
            var finishTime = DateTime.Now;
            var finish = finishTime - startTime;
            Tour.TotalTravelTime += finish;                     

            PrintTourInfo();
            //clearTourInfo();
            //myTask.Requests.Clear();
            //myTask.Dispose();
        }
        public void startTour()
        {
            speed = 1; // 5m/s
            if (myTask != null && myTask.Requests.Count > 0)
            {
                if (myTask.Requests.Peek().remainingEnergy_Joule == 0)
                {                   
                    System.Console.WriteLine("Node {0} is Dead.", myTask.Requests.Peek().Source.ID);
                    myTask.Requests.Clear();
                    StopCharging();
                    Bstation.StopScheduling();
                    return;
                }
                else
                {
                    destination = myTask.Requests.Peek().Source.CenterLocation;
                }              
            }
            else 
            {
                destination = Bstation.Position;
            }
           

            double MeToDes = Operations.DistanceBetweenTwoPoints(Position, destination);
            double DesToBs = Operations.DistanceBetweenTwoPoints(destination, Bstation.Position);
            double MeToBs = Operations.DistanceBetweenTwoPoints(Position, Bstation.Position);

            if (destination == Bstation.Position)
            {
                if (ResidualEnergy < (5 * MeToBs))
                {
                    MessageBox.Show("Mobile Charger Finish Power, Location : " + Position);
                    Dispatcher.Invoke(() => mobile_charger.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
                    return;
                }
                else
                {
                    goingBackTime = 1 + (int)(MeToBs / (speed * 2));
                    ResidualEnergy -= 5 * MeToBs;
                    PublicParamerters.TotalDistance_CoveredMC += MeToBs;
                    PublicParamerters.TotalEnergyForTravelMC += 5 * MeToBs;
                    WalkTimecounter = 0;
                    Dispatcher.Invoke(() => mobile_charger.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));

                    timer_GoingBack.Interval = TimeSpan.FromSeconds(1);
                    timer_GoingBack.Start();
                    timer_GoingBack.Tick += MoveToBS;
                }
            }
            else
            {
                if (ResidualEnergy - (5 * MeToDes) - (5 * DesToBs) < 100)
                {
                    while (myTask.Requests.Count > 0)
                    {
                        Bstation.SaveToQueue(myTask.Requests.Dequeue());
                    }
                    startTour(); // go back to check conditions again and return to Basesatation
                }
                else
                {
                    ResidualEnergy -= 5 * MeToDes;
                    PublicParamerters.TotalDistance_CoveredMC += MeToDes;
                    PublicParamerters.TotalEnergyForTravelMC += 5 * MeToDes;
                    TimeToNextSojoun = 1 + (int)(Operations.DistanceBetweenTwoPoints(Position, destination) / speed);
                    WalkTimecounter = 0;

                    timer_move.Interval = TimeSpan.FromSeconds(1); // 1 second to follow step move
                    timer_move.Start();
                    timer_move.Tick += MoveToNextNode;
                }
            }
        }
      
        private void MoveToNextNode(Object sender, EventArgs e)
        {
            if (myTask != null && myTask.Requests.Count > 0)
            {
                //next_position = get_NextPosition(); // to have animated/step move
                next_position = destination; // just jump to the destination
                Position = next_position;
                WalkTimecounter++;

                Dispatcher.Invoke(() => mobile_charger.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));

                if (Position == destination) //(Operations.DistanceBetweenTwoPoints(Position, destination) < 3) // if MC is close enough to sensor. 
                {
                    if (WalkTimecounter == TimeToNextSojoun)
                    {
                        timer_move.Stop();
                        Dispatcher.Invoke(() => mobile_charger.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));

                        tobeCharged = myTask.Requests.Dequeue();
                        double senConsummedE = PublicParamerters.BatteryIntialEnergy - tobeCharged.Source.ResidualEnergy;
                        tobeCharged.Rechargingtime = 1 + (int)Math.Ceiling(senConsummedE / PublicParamerters.chargingRate);
                        tobeCharged.ChargeTimecount = 0;
                        PublicParamerters.TotalTransferredEnergy += senConsummedE;               

                        timer_Recharging.Interval = TimeSpan.FromSeconds(1);
                        timer_Recharging.Tick += Recharging;
                        timer_Recharging.Start();
                    }                    
                }
                else
                {
                    ////next_position = get_NextPosition(); // to have animated/step move
                    //next_position = destination; // just jump to the destination

                    //Position = next_position;
                }
            }
            
        }
        
        public int goingBackTime { get; set; }
        public int WalkTimecounter = 0;
        private void MoveToBS(Object sender, EventArgs e)
        {
            WalkTimecounter++;
            if(WalkTimecounter >= goingBackTime)
            {
                timer_GoingBack.Stop();               

                Position = Bstation.Position;
                isFree = true;
                ResidualEnergy = PublicParamerters.BatteryIntialEnergyForMC;
                Dispatcher.Invoke(() => mobile_charger.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
                //Bstation.TriggerCharger(); 
            }
            else
            {
                //System.Console.WriteLine("Heading to The BaseStation Site !!!");
            }
            
        }

        Packet tobeCharged { get; set; }
        private void Recharging(Object sender, EventArgs e)
        {

            tobeCharged.ChargeTimecount++;

            if (tobeCharged.ChargeTimecount == tobeCharged.Rechargingtime)
            {
                timer_Recharging.Stop();
                //MessageBox.Show("charging " + tobeCharged.Source.ID);
                tobeCharged.Source.ResidualEnergy = PublicParamerters.BatteryIntialEnergy;
                tobeCharged.Source.rechargeReqCount = 0;
                PublicParamerters.requestedList.Remove(tobeCharged.Source);
                Bstation.Arriving_reqPackets.Remove(tobeCharged);

                startTour(); // go to the next node
            }
            else
            {
                //System.Console.WriteLine("Still recharging sensor {0}", tobeCharged.Source.ID);
                if (tobeCharged.ChargeTimecount > tobeCharged.Rechargingtime)                   
                {
                    tobeCharged.Source.ResidualEnergy = PublicParamerters.BatteryIntialEnergy;
                    PublicParamerters.requestedList.Remove(tobeCharged.Source);
                    Bstation.Arriving_reqPackets.Remove(tobeCharged);

                    timer_Recharging.Stop();
                }
            }
        }

        private Point get_NextPosition()
        {
            double EDs = Operations.DistanceBetweenTwoPoints(Position, destination);
            if (EDs == 0) EDs = 1;
            double Next_X = Position.X + (destination.X - Position.X) / EDs;
            double Next_Y = Position.Y + (destination.Y - Position.Y) / EDs;
            Point nextPoint = new Point(Next_X, Next_Y);

            return nextPoint; // return the steps to have smooth move         
        }

        private void PrintTourInfo()
        {

            System.Console.WriteLine("\n #####################################");
            System.Console.WriteLine(" _______________ # of Tasks {0} ______________", PublicParamerters.TotalNumofTasks);
            System.Console.WriteLine("The Total distance coverred     : {0}", PublicParamerters.TotalDistance_CoveredMC);
            System.Console.WriteLine("The MC's Travel Energy          : {0}", PublicParamerters.TotalEnergyForTravelMC);
            System.Console.WriteLine("The Total transfered Energy     : {0}", PublicParamerters.TotalTransferredEnergy);
            System.Console.WriteLine("The Total # of Request Received : {0}", PublicParamerters.TotalNumRequests);
            System.Console.WriteLine("The # of Data Collected         : {0}", PublicParamerters.TotalNumDataCollectedMC);
            System.Console.WriteLine("Collected Data Ratio            : {0}", PublicParamerters.MCCollectedDataPercentage);
            System.Console.WriteLine("\n #####################################");                
            
        }

        public void clearTourInfo()
        {
 
        }

        public void setPosition()
        {
            //Point pt = PublicParamerters.MainWindow.myNetWork[1].CenterLocation;
            Position = new Point(PublicParamerters.MainWindow.Canvas_SensingFeild.ActualWidth / 2, PublicParamerters.MainWindow.Canvas_SensingFeild.ActualHeight / 2);
        }

        public static void DrawLine(Point from, Point to)
        {
            Line lin = new Line();
            lin.Stroke = Brushes.Black;
            lin.StrokeThickness = 2;
            lin.X1 = from.X;
            lin.Y1 = from.Y;
            lin.X2 = to.X;
            lin.Y2 = to.Y;
            mycanvas.Children.Add(lin);
        }
    }
}
