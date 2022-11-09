using RR.Dataplane;
using RR.Intilization;
using System;
using System.Collections.Generic;
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

namespace RR.RingRouting
{
    public partial class RingNodeCandidate
    {
        public Sensor node { get; set; }
        public double difference { get; set; }
        public Sensor nextHop { get; set; }
        public List<RingNodeCandidate> NextHopCandidate = new List<RingNodeCandidate>();
        public bool alreadyRingNode = false;



        public RingNodeCandidate(Sensor sen, double dif)
        {
            difference = dif;
            node = sen;
        }
    }

    public partial class ConvexHullNodes
    {
        public Sensor node { get; set; }
        public double polarAngle { get; set; }
        public bool isInsideHull = false;
        public List<ConvexHullNodes> PotentialNextHop = new List<ConvexHullNodes>();
        public Queue<ConvexHullNodes> SortedConvexHullSet = new Queue<ConvexHullNodes>();
        public ConvexHullNodes(Sensor x, double y)
        {
            node = x;
            polarAngle = y;
        }

    }


    public partial class Ring : UserControl
    {

        private static double InitialRadius { get; set; }
        private static double ThreshHold { get; set; }
        private Point NetworkCenter = PublicParamerters.networkCenter;
        private static Canvas SensingField { get; set; }

        private List<RingNodeCandidate> RNodes = new List<RingNodeCandidate>();



        public Ring()
        {
            InitializeComponent();
        }
        public static void setInitialParameters(double rad, double thresh, Canvas sensingField)
        {
            InitialRadius = rad;
            ThreshHold = thresh;
            SensingField = sensingField;
        }

        private static void showVirtualRadius()
        {
            Ring ring = new Ring();
            ring.ell_ring.Height = InitialRadius * 2;
            ring.ell_ring.Width = InitialRadius * 2;
            Ring thresh = new Ring();
            thresh.ell_ring.Height = ThreshHold + InitialRadius * 2;
            thresh.ell_ring.Width = ThreshHold + InitialRadius * 2;
            //Margin ne?
            Point networkCenter = PublicParamerters.networkCenter;
            ring.Margin = new Thickness(networkCenter.X - ring.ell_ring.Height / 2, networkCenter.Y - ring.ell_ring.Height / 2, 0, 0);
            thresh.Margin = new Thickness(networkCenter.X - thresh.ell_ring.Height / 2, networkCenter.Y - thresh.ell_ring.Height / 2, 0, 0);
            SensingField.Children.Add(ring);
            SensingField.Children.Add(thresh);
        }


        #region  RingRouting Method
        private static RingNodeCandidate StartingPoint { get; set; }
        private List<RingNodeCandidate> RingNodeCandidates = new List<RingNodeCandidate>();
        private List<Sensor> PreviousRingNodes = new List<Sensor>();

        private static void RingRoutingBuildMethod()
        {
            Ring constructor = new Ring();
            // showVirtualRadius();
            constructor.findRingCandidates();
            constructor.getStartingPoint();
            constructor.populateNextHopeCandidates();
            constructor.startBuildingFromPoint(StartingPoint);
            //constructor.drawVirtualLine();
        }

        private void findRingCandidates()
        {
            List<Sensor> networkNodes = PublicParamerters.MainWindow.myNetWork;

            foreach (Sensor cand in networkNodes)
            {
                double distance = Operations.DistanceBetweenTwoPoints(cand.CenterLocation, NetworkCenter);
                double difference = (distance - InitialRadius);
                if (difference <= ThreshHold && difference >= 0)
                {
                    //   cand.Ellipse_indicator.Visibility = Visibility.Visible;
                    RingNodeCandidate candidate = new RingNodeCandidate(cand, difference);
                    RingNodeCandidates.Add(candidate);
                }

            }

        }

        private void getStartingPoint()
        {
            double lowest = ThreshHold;
            foreach (RingNodeCandidate cand in RingNodeCandidates)
            {
                if (cand.difference < ThreshHold)
                {
                    lowest = cand.difference;
                    StartingPoint = cand;
                }
            }
            try
            {

            }
            catch
            {
                StartingPoint = null;
            }
        }

        private void populateNextHopeCandidates()
        {
            RingNodeCandidate fromPoint = StartingPoint;
            Queue<RingNodeCandidate> queue = new Queue<RingNodeCandidate>();
            queue.Enqueue(fromPoint);

            while (queue.Count > 0)
            {

                fromPoint = queue.Dequeue();
                if (fromPoint.NextHopCandidate.Count == 0)
                {
                    foreach (Sensor neighbor in fromPoint.node.NeighborsTable)
                    {
                        foreach (RingNodeCandidate candidate in RingNodeCandidates)
                        {
                            if (neighbor.ID == candidate.node.ID)
                            {

                                fromPoint.NextHopCandidate.Add(candidate);
                                queue.Enqueue(candidate);
                            }
                        }
                    }
                }

            }

        }

        private static int counter = 0;
        private void startBuildingFromPoint(RingNodeCandidate fromPoint)
        {
            counter++;
            double lowest = ThreshHold + 10;
            RingNodeCandidate nextCandi = null;
            if (fromPoint.node.ID == 57)
            {
                //  Console.WriteLine();
            }
            foreach (RingNodeCandidate candi in fromPoint.NextHopCandidate)
            {

                if (counter > 5 && candi == StartingPoint)
                {
                    nextCandi = candi;
                    fromPoint.nextHop = StartingPoint.node;
                    fromPoint.alreadyRingNode = true;
                    RNodes.Add(fromPoint);
                    return;

                }
                if (candi.difference < lowest && !candi.alreadyRingNode)
                {
                    lowest = candi.difference;
                    nextCandi = candi;
                }
            }

            if (nextCandi == null)
            {
                MessageBox.Show("Null here");
                return;
            }

            fromPoint.nextHop = nextCandi.node;
            fromPoint.alreadyRingNode = true;
            RNodes.Add(fromPoint);
            PreviousRingNodes.Add(nextCandi.node);
            startBuildingFromPoint(nextCandi);


        }

        #endregion

        #region ConvexHull Method
        public static List<Sensor> SetofNodes = new List<Sensor>(); // All Nodes inside the area of intreset

        private static List<ConvexHullNodes> ConvexHullSet = new List<ConvexHullNodes>(); // All Nodes inside the area of intreset

        private static Stack<ConvexHullNodes> SubsetOfHull = new Stack<ConvexHullNodes>(); // Stack to go throw all the nodes

        public static List<Sensor> ConvexNodes = new List<Sensor>(); // Final Ring Nodes

        private static Queue<ConvexHullNodes> SortedSetOfConvexNodes = new Queue<ConvexHullNodes>();

        public static Sensor PointZero { get; set; }

        private static ConvexHullNodes PointZeroConvex { get; set; }


        private static void ConvexHullBuildMethod()
        {
            Ring constructor = new Ring();
            // showVirtualRadius();
            constructor.findTheSetofNodes();
            constructor.findPointZero();
            constructor.getPolarAngleToPointsFromAnchor(PointZero);
            ConvexHullNodes AnchorPoint = new ConvexHullNodes(PointZero, 0);
            AnchorPoint.PotentialNextHop = ConvexHullSet;
            PointZeroConvex = AnchorPoint;
            constructor.sortConvexHullSet(PointZero);
            SubsetOfHull.Push(AnchorPoint);
            constructor.startBuilding();
            constructor.drawVirtualLine();
            //constructor.startFromPointZero();



        }

        private void findTheSetofNodes()
        {
            List<Sensor> NetworkNodes = PublicParamerters.MainWindow.myNetWork;

            foreach (Sensor sen in NetworkNodes)
            {
                double distance = Operations.DistanceBetweenTwoPoints(sen.CenterLocation, NetworkCenter);
                if (((distance - InitialRadius)) <= ThreshHold)
                {
                    // Console.WriteLine(sen.ID);
                    SetofNodes.Add(sen);
                }

            }

        }

        private void findPointZero()
        {
            //O(n) where n = SetofNodes.Count();
            //P0 has the lowest y value , if two points have the same y value, we take the point that has the lowest x value amongst them
            List<Sensor> lowestYPoint = new List<Sensor>();
            double lowestYval = SetofNodes[0].CenterLocation.Y;
            Sensor holder = null;
            //(1) Find the lowest Y value
            foreach (Sensor p in SetofNodes)
            {
                if (p.CenterLocation.Y > lowestYval)
                {
                    lowestYval = p.CenterLocation.Y;
                }
            }
            //(2) Find the points with the lowest Y value, can be more than one
            foreach (Sensor p in SetofNodes)
            {
                if (p.CenterLocation.Y == lowestYval)
                {
                    lowestYPoint.Add(p);
                }
            }

            // Only One point 
            if (lowestYPoint.Count == 1)
            {
                PointZero = lowestYPoint[0];
            }
            else
            {
                //More than one point --> we check the lowest X value
                double lowestXVal = lowestYPoint[0].CenterLocation.X;
                foreach (Sensor p in lowestYPoint)
                {
                    if (p.CenterLocation.X <= lowestXVal)
                    {
                        holder = p;
                        lowestXVal = p.CenterLocation.X;
                    }
                }
                PointZero = holder;
            }


        }


        private void findPotentialPointsForAnchor(ConvexHullNodes anchor)
        {

            Point dest = new Point(anchor.node.CenterLocation.X, 10); // x-axis
            foreach (Sensor entry in anchor.node.NeighborsTable)
            {
                foreach (Sensor x in SetofNodes)
                {
                    if (ConvexNodes.Count > 3 && x == PointZero)
                    {
                        ConvexHullNodes nei = new ConvexHullNodes(x, 0);
                        anchor.PotentialNextHop.Add(nei);
                        return;
                    }
                    if (x.ID == entry.ID && !ConvexNodes.Contains(x))
                    {
                        double angle = Operations.GetDirectionAngle(anchor.node.CenterLocation, dest, x.CenterLocation);
                        ConvexHullNodes nei = new ConvexHullNodes(x, angle);
                        anchor.PotentialNextHop.Add(nei);
                        //anchor.PotentialNextHop.Add(x);
                    }
                }
            }
            SubsetOfHull.Push(anchor);
        }

        private bool isClockwise(ConvexHullNodes one, ConvexHullNodes two, ConvexHullNodes three)
        {
            Point a = one.node.CenterLocation;
            Point b = two.node.CenterLocation;
            Point c = three.node.CenterLocation;

            double value = ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));

            if (value >= 0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        private void getPolarAngleToPointsFromAnchor(Sensor anchor)
        {
            // according to the direction they make with P0 and the X-Axis
            Point dest = new Point(anchor.CenterLocation.X + InitialRadius + ThreshHold, anchor.CenterLocation.Y); // x-axis
            foreach (Sensor px in SetofNodes)
            {
                if (px.ID != anchor.ID)
                {
                    double angle = Operations.GetDirectionAngle(anchor.CenterLocation, dest, px.CenterLocation);
                    ConvexHullNodes node = new ConvexHullNodes(px, angle);
                    ConvexHullSet.Add(node);
                    //Console.WriteLine("Node {0} , has Angle {1}", node.node.ID, node.polarAngle);
                    // Console.WriteLine("Node {0} , has Angle {1}", px.ID, angle);
                }
            }
        }

        private ConvexHullNodes getLowestAngle(List<ConvexHullNodes> set)
        {
            double lowest = 10;
            ConvexHullNodes holder = null;

            foreach (ConvexHullNodes compare in set)
            {
                if (compare.polarAngle < lowest)
                {
                    lowest = compare.polarAngle;
                    holder = compare;
                }
            }

            return holder;

        }


        private void sortConvexHullSet(Sensor anchor)
        {
            List<ConvexHullNodes> beforeSort = ConvexHullSet;
            ConvexHullNodes small = null;
            do
            {
                try
                {
                    small = getLowestAngle(beforeSort);
                    SortedSetOfConvexNodes.Enqueue(small);
                    beforeSort.Remove(small);
                    // Console.WriteLine("Node {0} has angle {1}", small.node.ID, small.polarAngle);
                }
                catch
                {
                    small = null;
                    MessageBox.Show("Just returned a null");
                }


            } while (beforeSort.Count > 0);

        }

        private void sortMyPotentialNextHop(ConvexHullNodes from)
        {
            List<ConvexHullNodes> beforeSort = from.PotentialNextHop;

            ConvexHullNodes small = null;
            do
            {
                try
                {
                    small = getLowestAngle(beforeSort);
                    from.SortedConvexHullSet.Enqueue(small);
                    beforeSort.Remove(small);
                    //  Console.WriteLine("Node {0} has angle {1}", small.node.ID, small.polarAngle);
                }
                catch
                {
                    small = null;
                    MessageBox.Show("Just returned a null");
                }


            } while (beforeSort.Count > 0);



        }

        private static bool isMyNeighbor(Sensor s1, Sensor s2)
        {
            bool isNeighbor = false;
            foreach (Sensor entry in s1.NeighborsTable)
            {
                if (entry.ID == s2.ID)
                {
                    isNeighbor = true;
                    return true;
                }
            }
            return isNeighbor;
        }


        private void startBuilding()
        {
            bool takeNextPoint = true;
            SubsetOfHull.Push(SortedSetOfConvexNodes.Dequeue());
            do
            {
                if (takeNextPoint)
                {
                    SubsetOfHull.Push(SortedSetOfConvexNodes.Dequeue());
                }

                ConvexHullNodes PointThree = SubsetOfHull.Pop();
                ConvexHullNodes PointTwo = SubsetOfHull.Pop();
                ConvexHullNodes PointOne = SubsetOfHull.Pop();



                if (isClockwise(PointOne, PointTwo, PointThree))
                {
                    SubsetOfHull.Push(PointOne);
                    SubsetOfHull.Push(PointTwo);
                    SubsetOfHull.Push(PointThree);
                    takeNextPoint = true;
                }
                else
                {
                    SubsetOfHull.Push(PointOne);
                    SubsetOfHull.Push(PointThree);
                    if (SubsetOfHull.Count >= 3)
                    {
                        takeNextPoint = false;
                    }
                }

            } while (SortedSetOfConvexNodes.Count > 0);

            // Console.WriteLine("Ending ****");
            int c = SubsetOfHull.Count;
            do
            {
                ConvexHullNodes x = SubsetOfHull.Pop();
                ConvexNodes.Add(x.node);
                //   x.node.ShowComunicationRange(true);
                //  Console.WriteLine(x.node.ID);
            } while (SubsetOfHull.Count > 0);

            RingNodesFunctions.doLastCheck();

        }

        #endregion




        private void drawVirtualLine()
        {
            // Line l = SensingField.Children.
            foreach (RingNodes x in PublicParamerters.RingNodes)
            {
                Sensor from = x.Node;
                Sensor to = x.AntiClockWiseNeighbor;
                Line lineBetweenTwo = new Line();
                string name = "line" + from.ID.ToString() + to.ID.ToString();
                lineBetweenTwo.Name = name;
                lineBetweenTwo.Fill = Brushes.Black;
                lineBetweenTwo.Stroke = Brushes.Black;
                lineBetweenTwo.X1 = from.CenterLocation.X;
                lineBetweenTwo.Y1 = from.CenterLocation.Y;
                lineBetweenTwo.X2 = to.CenterLocation.X;
                lineBetweenTwo.Y2 = to.CenterLocation.Y;
                SensingField.Children.Add(lineBetweenTwo);
                PublicParamerters.MyArrowLines.Add(lineBetweenTwo);
            }
        }

        public static void removeOldLine(Sensor from, Sensor to)
        {
            string name = "line" + from.ID.ToString() + to.ID.ToString();
            Line line = null;
            foreach (Line l in PublicParamerters.MyArrowLines)
            {
                if (l.Name == name)
                {
                    line = l;
                    break;
                }
            }
            try
            {
                PublicParamerters.MyArrowLines.Remove(line);
                SensingField.Children.Remove(line);
            }
            catch
            {
                line = null;
                Console.WriteLine("Line = null");
            }

        }
        public static void addNewLine(Sensor from, Sensor to)
        {
            Line lineBetweenTwo = new Line();
            lineBetweenTwo.Name = "line" + from.ID + to.ID;
            lineBetweenTwo.Fill = Brushes.Black;
            lineBetweenTwo.Stroke = Brushes.Black;
            lineBetweenTwo.X1 = from.CenterLocation.X;
            lineBetweenTwo.Y1 = from.CenterLocation.Y;
            lineBetweenTwo.X2 = to.CenterLocation.X;
            lineBetweenTwo.Y2 = to.CenterLocation.Y;
            SensingField.Children.Add(lineBetweenTwo);
            PublicParamerters.MyArrowLines.Add(lineBetweenTwo);
        }
        public static void startRingConstruction()
        {
            ConvexHullBuildMethod();
            // RingRoutingBuildMethod();

        }


        public static void getCenterOfNetwork()
        {
            double sumX = 0;
            double sumY = 0;
            double count = 0;
            foreach (Sensor sensor in PublicParamerters.MainWindow.myNetWork)
            {
                sumX += sensor.CenterLocation.X;
                sumY += sensor.CenterLocation.Y;
                count++;
            }
            sumX /= count;
            sumY /= count;
            PublicParamerters.networkCenter = new Point(sumX, sumY);
        }

    }
    /// <summary>
    /// Interaction logic for Ring.xaml
    /// </summary>
   
}
