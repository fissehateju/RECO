using RR.Dataplane;
using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RR.Comuting.SinkClustering 
{
    #region clusteringAssistance

    /*
    public class ClusteringThreshold
    {
        public static double Max = 48;
        public static double Min = 0;
    }*/

    class Angle
    {
        /// <summary>
        /// b1 = sink 1
        /// b2 = sink 2
        /// s= source node
        /// </summary>
        /// <param name="sink1"></param>
        /// <param name="sink2"></param>
        /// <param name="bifrication"></param>
        /// <returns></returns>
        public double GetAngle(Point sink1, Point sink2, Point bifrication)
        {
            System.Windows.Vector u = new System.Windows.Vector(sink1.X - bifrication.X, sink1.Y - bifrication.Y);
            System.Windows.Vector v = new System.Windows.Vector(sink2.X - bifrication.X, sink2.Y - bifrication.Y);
            double angle = System.Windows.Vector.AngleBetween(u, v);

            return Math.Abs(angle);

        }

        public double Getvariance(List<AngleSimlirityEdge> X)
        {
            if (X.Count > 0)
            {
                double LEN = X.Count;
                double m = GetMeanAngle(X);
                double sum = 0;
                for (int i = 0; i < X.Count; i++)
                {
                    double xi = X[i].Angle;
                    if (!double.IsNaN(xi))
                    {
                        sum += Math.Pow((xi - m), 2);
                    }
                    else
                    {
                        return double.NaN;
                    }
                }
                return (sum / LEN);
            }
            else
            {
                return double.NaN;
            }
        }

        public double GetMeanAngle(List<AngleSimlirityEdge> X)
        {
            if (X.Count > 0)
            {
                double sum = 0;
                double length = 0; // only if the rate is not zero.
                foreach (AngleSimlirityEdge d in X)
                {
                    if (d.Angle > 0)
                    {
                        length = length + 1;
                    }
                    sum += d.Angle;
                }

                return sum / length;
            }
            else
            {
                return double.NaN;
            }
        }
    }

    public class AngleSimlirityEdge
    {
        public int ID { get; set; }

        public int StartVertextID
        {
            get
            {
                return StartVertex.ID;
            }

        }
        public string FromTo { get { return StartVertextID + "->" + EndVertextID; } }
        public int EndVertextID { get { return EndVertex.ID; } }
        public double Angle { get; set; }
        public double MeanAngle { get; set; }
        public double Variance { get; set; }

        public double StandardDeviation
        {
            get
            {
                return Math.Sqrt(Variance + 1);
            }
        }

        /// <summary>
        /// A z-score describes the position of a raw score in terms of its distance from the mean, when measured in standard deviation units. The z-score is positive if the value lies above the mean, and negative if it lies below the mean.
        /// The value of the z-score tells you how many standard deviations you are away from the mean. If a z-score is equal to 0, it is on the mean. A positive z-score indicates the raw score is higher than the mean average.For example, if a z-score is equal to +1, it is 1 standard deviation above the mean. A negative z-score reveals the raw score is below the mean average.For example, if a z-score is equal to -2, it is 2 standard deviations below the mean.  Another way to interpret z-scores is by creating a standard normal distribution (also known as the z-score distribution or probability distribution).
        ///Fig 3 illustrates the important features of any standard normal distribution(SND).
        /// </summary>
        public double StandardScor // angle from <FromID> TO <ToID>
        {
            get
            {
                return (Angle - MeanAngle) / StandardDeviation;
            }
        }

        public Sensor StartVertex { get; set; }
        public Sensor EndVertex { get; set; }


    }


    /// <summary>
    /// sort from smaller 
    /// </summary>
    public class sorter : IComparer<AngleSimlirityEdge>
    {

        public int Compare(AngleSimlirityEdge x, AngleSimlirityEdge y)
        {
            return x.StandardScor.CompareTo(y.StandardScor);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Cluster
    {
        public int ID { get; set; } // if ID=-1 then this should be deleted.
        public string MembersString
        {
            get
            {
                string str = "";
                foreach (Sensor mem in Members)
                {
                    str += mem.ID + "-";
                }

                return str;
            }
        }
        public bool IsRed { get; set; }
        public double Min { get; set; }
        public List<Sensor> Members = new List<Sensor>(); // points or sink or any objects.

        /// <summary>
        /// center of the cluster
        /// </summary>
        public Point Centeriod
        {
            get
            {
                double memLenth = Members.Count;
                double xsum = 0, ysum = 0;
                foreach (Sensor mem in Members)
                {
                    xsum += mem.CenterLocation.X;
                    ysum += mem.CenterLocation.Y;
                }

                return new Point(xsum / memLenth, ysum / memLenth);
            }
        }

        /// <summary>
        /// distance to from the source to the centeriod of the cluster.
        /// </summary>
        public double DistanceToCenteriod
        {
            get
            {
                return Operations.DistanceBetweenTwoPoints(Centeriod, SourceLocation);
            }
        }

        /// <summary>
        /// nearest to the source. from source to the cluster border.
        /// </summary>
        public double DistanceToClusterBorder
        {
            get
            {
                double minDis = 0;
                if (Members.Count > 0)
                {
                    minDis = Operations.DistanceBetweenTwoPoints(SourceLocation, Members[0].CenterLocation);
                    foreach (Sensor sink in Members)
                    {
                        double curDis = Operations.DistanceBetweenTwoPoints(SourceLocation, sink.CenterLocation);
                        if (curDis < minDis)
                        {
                            minDis = curDis;
                        }
                    }
                    return minDis;
                }
                else return double.NaN;
            }
        }

        public double MinAngle
        {
            get; set;

        }

        /// <summary>
        /// current source of 
        /// </summary>
        public Point SourceLocation { get; set; }


    }

    #endregion


    #region clusring 
    public class Clustering
    {
        Canvas mycanvas;
        private List<AngleSimlirityEdge> MinusList = new List<AngleSimlirityEdge>(); // all min scors.
        public List<AngleSimlirityEdge> GetSimlirityEdgesMinues { get { return MinusList; } }
        private List<AngleSimlirityEdge> allSimlirityEdges = new List<AngleSimlirityEdge>(); // The edges which has lower score than the average.
        public List<AngleSimlirityEdge> GetSimlirityEdgesAll { get { return allSimlirityEdges; } }
        private Point sourcePointCnterlocation;
        private List<Sensor> sinks;
        private List<Cluster> Clusters = new List<Cluster>();
        public List<Cluster> GetClusters { get { return Clusters; } }
        private double angThre;


        /// <summary>
        /// b_k= the bifuraction point
        /// bk_sinks= sinks to be clustered at the bk point.
        /// 
        /// </summary>
        /// <param name="bk_sinks"></param>
        /// <param name="b_k"></param>
        /// <param name="canvas"></param>
        public Clustering(List<Sensor> bk_sinks, Point b_k, Canvas canvas, double clustingThreshould)
        {
            angThre = clustingThreshould;
            sourcePointCnterlocation = b_k;
            sinks = bk_sinks;
            mycanvas = canvas;

            FindAngles(); // 1- find the angles.
            FindScoresForAllEdges(false); // 2- find the scores.
            FindMinusScors(); // 3- get the mius only.
            ComputeClusters1(MinusList); // 5 find the clusters
           

        }


        /// <summary>
        /// matching: https://en.wikipedia.org/wiki/Matching_(graph_theory)
        /// </summary>
        /// <param name="simlirities"></param>
        private void ComputeClusters1(List<AngleSimlirityEdge> simlirities)
        {
            //DrawLines(allSimlirityEdges, Brushes.Black); // complete graph
            //DrawLines(simlirities, Brushes.Black); // sirnked graph
            simlirities.Sort(new sorter());
            List<Cluster> LocalClusters = new List<Cluster>();

            int cid = 0;
            // more than 2 sinks:
            if (sinks.Count >= 3)
            {
                foreach (AngleSimlirityEdge edge in simlirities)
                {
                    // both are not clusters
                    bool bothAreClusterd = edge.StartVertex.IsClustered && edge.EndVertex.IsClustered;
                    bool bothNotClustered = !edge.StartVertex.IsClustered && !edge.EndVertex.IsClustered; ; // both the verticies of the the edge are not clusterd.
                    bool justOneClustered = (edge.StartVertex.IsClustered || edge.EndVertex.IsClustered) && !bothAreClusterd; // simi-saturated

                    if (bothNotClustered) // both vertecies of the edge are not clustered.
                    {
                        if (edge.Angle <= angThre) // this value need to be checked maybe you can chek if the angle is less than 45. : matching or the matximum matching
                        {
                            cid++;
                            // both verticies are not clusterd
                            Cluster cluster = new Cluster();
                            cluster.MinAngle = edge.Angle;
                            cluster.SourceLocation = sourcePointCnterlocation;
                            cluster.Min = edge.StandardScor;
                            cluster.ID = cid;
                            edge.StartVertex.IsClustered = true;
                            edge.EndVertex.IsClustered = true;
                            cluster.Members.Add(edge.StartVertex);
                            cluster.Members.Add(edge.EndVertex);
                            LocalClusters.Add(cluster);

                            // darw: optioanal
                            //DrawLines(edge,Brushes.Black, 4,0.5); // blocks: 
                        }
                    }
                    else if (justOneClustered) // simi-saturated
                    {
                        // only one vertext is clusterd. add to cluster.
                        Sensor clusterdVertex; // the one which clusterd
                        Sensor notClusterdVertex;
                        if (edge.StartVertex.IsClustered) { clusterdVertex = edge.StartVertex; notClusterdVertex = edge.EndVertex; }
                        else { clusterdVertex = edge.EndVertex; notClusterdVertex = edge.StartVertex; }

                        Cluster FoundCluster = FindClusterByVertext(clusterdVertex, LocalClusters); // 
                        if (FoundCluster != null)
                        {
                            double difAngle1 = Math.Sqrt(Math.Abs(Math.Pow(edge.Angle, 2) - Math.Pow(FoundCluster.MinAngle, 2)));
                            // double difAngle2 = Math.Abs(Math.Pow(edge.Angle, 2) - Math.Pow(FoundCluster.MinAngle, 2));
                            if (difAngle1 <= angThre) // this should be between 5 and 45.
                            {
                                FoundCluster.Members.Add(notClusterdVertex);
                                notClusterdVertex.IsClustered = true;

                                /*
                                if (FoundCluster.MinAngle > edge.Angle)
                                {
                                    FoundCluster.MinAngle = edge.Angle;
                                }*/

                                // DrawLines(edge, Brushes.Blue, 2,1);
                            }
                        }
                    }

                    else if (bothAreClusterd)
                    {
                        // both are clustered in seperate clusters.
                        Cluster startC = FindClusterByVertext(edge.StartVertex, LocalClusters);
                        Cluster endC = FindClusterByVertext(edge.EndVertex, LocalClusters);
                        if (startC != endC)
                        {
                            if (!startC.IsRed && !endC.IsRed)
                            {
                                double dif2 = Math.Abs(Math.Pow(startC.MinAngle, 2) - Math.Pow(endC.MinAngle, 2));
                                if (dif2 <= angThre)
                                {
                                    startC.Members.AddRange(endC.Members);
                                    if (startC.MinAngle > endC.MinAngle) { startC.MinAngle = endC.MinAngle; }
                                    endC.ID = -1;
                                    startC.IsRed = true;
                                    //  DrawLines(edge, Brushes.Red, 1, 7);
                                }
                            }
                        }
                    }
                }
            }
            else
            {

                //  two sinks only
                if (sinks.Count == 2)
                {
                    if (simlirities.Count == 1)
                    {
                        AngleSimlirityEdge edge = simlirities[0];
                        if (edge.Angle <= angThre)
                        {
                            Cluster cluster = new Cluster();
                            cluster.SourceLocation = sourcePointCnterlocation;
                            cluster.Min = edge.StandardScor;
                            cluster.ID = cid;
                            edge.StartVertex.IsClustered = true;
                            edge.EndVertex.IsClustered = true;
                            cluster.Members.Add(edge.StartVertex);
                            cluster.Members.Add(edge.EndVertex);
                            LocalClusters.Add(cluster);
                        }
                        else
                        {
                            Cluster cluster1 = new Cluster();
                            cluster1.SourceLocation = sourcePointCnterlocation;
                            cluster1.Min = 0;
                            cluster1.ID = cid;
                            edge.StartVertex.IsClustered = true;
                            cluster1.Members.Add(edge.StartVertex);
                            LocalClusters.Add(cluster1);

                            Cluster cluster2 = new Cluster();
                            cluster2.SourceLocation = sourcePointCnterlocation;
                            cluster2.Min = 0;
                            cluster2.ID = cid;
                            edge.EndVertex.IsClustered = true;
                            cluster2.Members.Add(edge.EndVertex);
                            LocalClusters.Add(cluster2);
                        }

                    }
                }
            }
            // reorganzied:
            int clusteredNodesCount = 0;
            foreach (Cluster c in LocalClusters)
            {
                if (c.ID != -1) // removed -1.
                {
                    clusteredNodesCount += c.Members.Count; // count how many nodes which are clustered.
                    Clusters.Add(c);

                }
            }

            // add the isolated sinks:
            if (clusteredNodesCount < sinks.Count)
            {
                foreach (Sensor sin in sinks)
                {
                    if (!sin.IsClustered)
                    {
                        Cluster cluster = new Cluster();
                        cluster.ID = Clusters.Count + 1;
                        cluster.SourceLocation = sourcePointCnterlocation;
                        cluster.Members.Add(sin);

                        Clusters.Add(cluster);
                    }
                }
            }


            // finished:
            foreach (Sensor sink in sinks)
            {
                sink.IsClustered = false;
            }

        }



        /// <summary>
        /// get the angles for all sinks.
        /// </summary>
        private void FindAngles()
        {
            int id = 0;
            for (int i = 0; i < sinks.Count; i++)
            {
                Sensor isink = sinks[i];
                for (int j = i + 1; j < sinks.Count; j++)
                {
                    if (i != j)
                    {
                        id += 1;
                        Sensor jsink = sinks[j];
                        double ang = new Angle().GetAngle(isink.Position, jsink.Position, sourcePointCnterlocation);
                        // string str = " # :  " + isink.ID + "  :" + sourcePointCnterlocation.ID + "  :" + jsink.ID + " >>>" + ang;
                        // Console.WriteLine(str);
                        AngleSimlirityEdge edge = new AngleSimlirityEdge() { Angle = ang, StartVertex = isink, EndVertex = jsink, ID = id };
                        isink.Simlirities.Add(edge);
                        allSimlirityEdges.Add(edge);
                    }
                }
            }
        }






        /// <summary>
        /// get the score for all sinks.
        /// </summary>
        private void FindScoresForAllEdges(bool difrentMeans)
        {

            Angle ang1 = new Angle();
            // all Simlirities are included:
            double mean = ang1.GetMeanAngle(allSimlirityEdges);
            double variance = ang1.Getvariance(allSimlirityEdges);
            foreach (Sensor sink in sinks)
            {
                foreach (AngleSimlirityEdge sim in sink.Simlirities)
                {
                    sim.MeanAngle = mean;
                    sim.Variance = variance;
                }

            }

        }








        /// <summary>
        /// get the cluster to which the vertex i is belonged.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="Clusters"></param>
        /// <returns></returns>
        private Cluster FindClusterByVertext(Sensor i, List<Cluster> Clusters)
        {
            foreach (Cluster cluster in Clusters)
            {
                if (cluster.ID != -1) // clusters with -1 ID are considered as deleted.
                {
                    foreach (Sensor j in cluster.Members)
                    {
                        if (i.ID == j.ID)
                        {
                            return cluster;
                        }
                    }
                }
            }
            return null;
        }




        /// <summary>
        ///  get the edges with minus scors.
        /// </summary>
        private void FindMinusScors()
        {
            foreach (AngleSimlirityEdge row in allSimlirityEdges)
            {
                if (row.Angle < angThre)
                {
                    MinusList.Add(row);
                }
            }
        }





        private void DrawLines(List<AngleSimlirityEdge> simlirities, Brush b)
        {
            foreach (AngleSimlirityEdge edge in simlirities)
            {

                try
                {
                    Point fromP = sinks[edge.StartVertextID - 1].CenterLocation;
                    Point toP = sinks[edge.EndVertextID - 1].CenterLocation;

                    Line lin = new Line();
                    lin.Stroke = b;
                    lin.StrokeThickness = 1;
                    lin.StrokeDashArray = new DoubleCollection() { 1 };
                    lin.X1 = fromP.X;
                    lin.Y1 = fromP.Y;
                    lin.X2 = toP.X;
                    lin.Y2 = toP.Y;

                    mycanvas.Children.Add(lin);
                }
                catch
                {

                }
            }
        }


        private void DrawLines(AngleSimlirityEdge sim, Brush b, double StrokeThickness, double StrokeDashArray)
        {

            try
            {

                Point fromP = sinks[sim.StartVertextID - 1].CenterLocation;
                Point toP = sinks[sim.EndVertextID - 1].CenterLocation;

                Line lin = new Line();
                lin.Stroke = b;
                lin.StrokeThickness = StrokeThickness;
                lin.StrokeDashArray = new DoubleCollection() { StrokeDashArray };
                lin.X1 = fromP.X;
                lin.Y1 = fromP.Y;
                lin.X2 = toP.X;
                lin.Y2 = toP.Y;

                mycanvas.Children.Add(lin);
            }
            catch
            {

            }

        }




    }
    #endregion

}
