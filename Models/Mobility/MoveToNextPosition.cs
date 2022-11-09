using RR.Intilization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RR.Dataplane;
using RR.Dataplane.PacketRouter;
using RR.Comuting.Routing;
using RR.Models.Charging;
using RR.Properties;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RR.Comuting.computing;
using RR.Dataplane.NOS;
using static RR.Computations.RandomvariableStream;


namespace RR.Models.Mobility
{
    internal class MoveToNextPosition
    {
        private UserControl ObjectToMove; // the object to be moved
        private DispatcherTimer SelectDistinationLocation = new DispatcherTimer();
        private DispatcherTimer MovmentScheduler = new DispatcherTimer();
        private DispatcherTimer timer_Recharging = new DispatcherTimer();
        public Packet Request;
        private RechargingModel recharge { get; set; }
        private NetworkOverheadCounter counter;
        private Sink charger { get; set; }
        /// <summary>
        /// get the position of the object
        /// </summary>
        public Point ObjectPosition
        {
            get
            {
                double x = ObjectToMove.Margin.Left;
                double y = ObjectToMove.Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
            set
            {
                Point p = value;
                ObjectToMove.Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        Point velocityVector;
        Point m_current;
        Point destination;
        double speed { get; set; }
        double MaxX, MaxY;
        private bool isBound = false;


        /// <summary>
        /// set the object to be moved.
        /// no Bounds of the area to cruise.
        /// </summary>
        /// <param name="_sink"></param>
        /// 

        public MoveToNextPosition(UserControl _sink)
        {
            ObjectToMove = _sink;
            isBound = false;
            ObjectToMove.Focusable = true;

        }

        /// <summary>
        /// MaxX , MaxY are Bounds of the area to cruise.
        /// </summary>
        /// <param name="_sink"></param>
        /// <param name="MaxX"></param>
        /// <param name="MaxY"></param>
        public MoveToNextPosition(UserControl _sink, double _MaxX, double _MaxY , Sink _charger)
        {
            MaxX = _MaxX;
            MaxY = _MaxY;
            isBound = true;
            ObjectToMove = _sink;
            charger = _charger;
            counter = new NetworkOverheadCounter();
            speed = 5;
            PublicParamerters.MainWindow.lbl_par_Speed.Content = speed.ToString();
        }

        /// <summary>
        /// start moving
        /// </summary>
        public void StartMove(Packet req)
        {
            Request = req;
            SelectDistinationLocation.Tick += SelectDistinationLocation_Tick;
            SelectDistinationLocation.Start();
            MovmentScheduler.Tick += Scheduler_Tick;
        }

        /// <summary>
        /// stop moving
        /// </summary>
        public void StopMoving()
        {           
            SelectDistinationLocation.Stop();
            MovmentScheduler.Stop();
            counter.DisplayRefreshAtReceivingPacket(PublicParamerters.MainWindow.myNetWork[0]);

            if (Request != null)
            {
                NodeCharging();
            }
            else if(ObjectPosition == new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2))
            {
                charger.isFree = true;
                charger.ResidualEnergy = PublicParamerters.BatteryIntialEnergyForMC;
                charger.BreakTime();
                charger.isTraveling = false;

                var TourEnded = DateTime.Now - charger.TourStated;
                PublicParamerters.ServiceTimeInSecond += TourEnded.TotalSeconds;

                //charger.Bstation.TriggerCharger();
                charger.PrintTourInfo();
            }
            else
            {
                MessageBox.Show("Simulation stopped");
            }
           
        }

        public void StopOperation()
        {
            SelectDistinationLocation.Stop();
            MovmentScheduler.Stop();
            charger.BreakTime();
            if (recharge != null) recharge.timer_Recharging.Stop();

            counter.DisplayRefreshAtReceivingPacket(PublicParamerters.MainWindow.myNetWork[0]);
        }

        public void NodeCharging()
        {
            charger.ChargingAlert();
            RechargingModel recharge = new RechargingModel(charger, Request);            
            recharge.startRecharing();
        }

        private void Scheduler_Tick(object sender, EventArgs e)
        {
            ScheduleMobility();
        }

        private void SetTravelDelay(TimeSpan timeSpan)
        {
            SelectDistinationLocation.Interval = timeSpan;
            MovmentScheduler.Start();
        }

        private void SelectDistinationLocation_Tick(object sender, EventArgs e)
        {          
            BeginWalk();
        }

        
        /// <summary>
        /// get the 
        /// </summary>
        public void BeginWalk()
        {

            m_current = ObjectPosition;
            if (Request != null)
            {
                destination = Request.Source.CenterLocation;
            }           
            else
            {
                destination = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);
            }           

            //speed = 2; //Speed; // random distrubiton
            double dx = destination.X - m_current.X;
            double dy = destination.Y - m_current.Y;
            double k = speed / Math.Sqrt((dx * dx) + (dy * dy));//

            if (!double.IsInfinity(k))
            {

                velocityVector = new Point(k * dx, k * dy); // speed +direction
                TimeSpan travelDelay = TimeSpan.FromSeconds(Operations.DistanceBetweenTwoPoints(m_current, destination) / speed);
                SetTravelDelay(travelDelay); // Timer.
            }
            else
            {
                Console.WriteLine("Got move problem. Destination is " + Operations.DistanceBetweenTwoPoints(m_current, destination).ToString() + " far");
                charger.SetMobility();
            }
        }

        public void ScheduleMobility()
        {
            MovmentScheduler.Interval = TimeSpan.FromSeconds(1); 
            m_current = ObjectPosition;
            double x = (m_current.X + velocityVector.X);
            double y = (m_current.Y + velocityVector.Y);

            Point moveto = new Point(x, y);


            ObjectPosition = moveto;
            ObjectToMove.Width += 0.00001; // just to trigger an event. dont remove this line.


            // Operations.DrawLine(PublicParamerters.MainWindow.Canvas_SensingFeild, m_current, moveto);

            if (Operations.DistanceBetweenTwoPoints(ObjectPosition, destination) < 5)
            {
                ObjectPosition = destination;
                StopMoving();
                
            }

        }
    }
}
