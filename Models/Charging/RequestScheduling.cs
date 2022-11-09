using RR.Computations;
using RR.Dataplane;
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
using RR.Cluster;

namespace RR.Models.Charging
{
        public class TobeChargedSorters : IComparer<ChargingPriorityEntry>
        {
            public int Compare(ChargingPriorityEntry y, ChargingPriorityEntry x)
            {
                return y.Priority.CompareTo(x.Priority);
            }
        }
    public class ChargingPriorityEntry
    {
        public double Priority { get; set; }
        public double distance { get; set; }
        public double angle { get; set; }
        public int SensorID { get { return Request.Source.ID; } }
        public Packet Request { get; set; }

    }
    public class RequestScheduling
    {
        private BaseStation BStation;
        private Sink TheSink;
        private List<Packet> RequestList;
        private Queue<Packet> SortedReqs = new Queue<Packet>();
        public RequestScheduling()
        {

        }
        public RequestScheduling(BaseStation Bs, Sink Sk, List<Packet> Reqs)
        {
            BStation = Bs;
            TheSink = Sk;   
            RequestList = Reqs;
        }

        public Metrics NWmetrics;
        public Queue<Packet> reOrdering(ClusteringForRECO terr)
        {            
            if (terr == null)
            {
                //// 
                /// Travelling Salesman algorithm to find the efficient path for Mobile charger traveling

                var tsmRout = new StartTSM();
                List<int> orderdID = tsmRout.Startit(RequestList);
                int indexOFzero;
                List<int> temp;
                for (int i = 0; i < orderdID.Count; i++)
                {
                    if (orderdID[0] != 0)  // rearrange the list if indexzero doesnt contain ID of node zero.
                    {
                        temp = new List<int>();
                        indexOFzero = orderdID.IndexOf(0);
                        temp.AddRange(orderdID.GetRange(0, indexOFzero));
                        orderdID.RemoveRange(0, indexOFzero);
                        orderdID.AddRange(temp);
                        break;
                    }
                }

                orderdID.Remove(0); // node zero is not needed
                while (orderdID.Count > 0)
                {
                    foreach (Packet p in RequestList)
                    {
                        if (p.Source.ID == orderdID[0])
                        {
                            SortedReqs.Enqueue(p);
                            orderdID.RemoveAt(0);
                            break;
                        }
                    }
                }

                //////System.Console.WriteLine("\n ordered requests using TSM.");
                //////foreach (int Id in orderdID)
                //////{
                //////    System.Console.Write(Id + " , ");
                //////}
                ///
            }
            else
            {
                Dictionary<string, List<Packet>> TwoLanePaths = SideWalk(terr.name);
                bool isreturning = false;
                Packet closestPack;
                Point MCP = TheSink.CenterLocation;

                foreach ( var twoLanePath in TwoLanePaths )
                {
                    int counter = 0;
                    while (twoLanePath.Value.Count > 0)
                    {
                        counter += 1;
                        closestPack = getNextSojourn2(MCP, twoLanePath.Value, terr, isreturning, counter);
                        SortedReqs.Enqueue(closestPack);
                        MCP = closestPack.Source.CenterLocation;
                        RequestList.Remove(closestPack);
                        twoLanePath.Value.Remove(closestPack);
                    }
                    isreturning = true;
                }
                
            }

            return SortedReqs;
        }

        private Dictionary<string, List<Packet>> SideWalk(string terr_name)
        {
            Point NetCenter = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);
            Dictionary<string, List<Packet>> keyValuePairs = new Dictionary<string, List<Packet>>();
            List<Packet> travellaneValue = new List<Packet>();
            List<Packet> returnlaneValue = new List<Packet>();
            string RoadLane = null;
            double fx;

            foreach (var pak in RequestList)
            {
                if (terr_name == "topLeft" || terr_name == "bottomRight")
                {
                    fx = pak.Source.CenterLocation.X;  // line fuction is f(x) = x
                    if (pak.Source.CenterLocation.Y < fx)
                    {
                        RoadLane = "travellane";
                        travellaneValue.Add(pak);
                    }
                    else
                    {
                        RoadLane = "returnlane";
                        returnlaneValue.Add(pak);
                    }
                }
                else if (terr_name == "bottomLeft" || terr_name == "topRight")
                {
                    fx = PublicParamerters.NetworkSquareSideLength - pak.Source.CenterLocation.X; // line fuction is f(x) = L - x
                    if (pak.Source.CenterLocation.Y < fx)
                    {
                        RoadLane = "travellane";
                        travellaneValue.Add(pak);
                    }
                    else
                    {
                        RoadLane = "returnlane";
                        returnlaneValue.Add(pak);
                    }
                }               
            }
            keyValuePairs.Add("travellane", travellaneValue);
            keyValuePairs.Add("returnlane", returnlaneValue);

            return keyValuePairs;
        }

        private Packet starterNode(List<Packet> reqs, ClusteringForRECO terr)
        {
            Packet holder = null;
            double minDs = double.MaxValue;
            foreach (Packet reqPacket in reqs)
            {
                double Ds_MC = Operations.DistanceBetweenTwoPoints(terr.EndPoint, reqPacket.Source.CenterLocation);
                if(Ds_MC < minDs)
                {
                    minDs = Ds_MC;
                    holder = reqPacket;
                }
            }
            return holder;
        }
        public Packet getNextSojourn2(Point MC_pos, List<Packet> reqs, ClusteringForRECO terr, bool isreturing, int counter)
        {                     

            if (reqs.Count == 1)
            {
                return reqs[0];
            }

            Point endPoint;
            Point NetCenter = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);

            if (isreturing)
            {
                endPoint = NetCenter;
                if (counter == 1)
                {
                    Packet hold = starterNode(reqs, terr);
                    if (hold != null)
                    {
                        return hold;
                    }
                }
            }
            else
            {
                endPoint = terr.EndPoint;
            }

            Packet holder;
            double Dsum = 0, Asum = 0;
            List<ChargingPriorityEntry> PriorityEntries = new List<ChargingPriorityEntry>();

            foreach (Packet reqPacket in reqs)
            {
                double Ds_MC = Operations.DistanceBetweenTwoPoints(MC_pos, reqPacket.Source.CenterLocation);
                double Angle = Operations.AngleDotProdection(MC_pos, reqPacket.Source.CenterLocation, endPoint);
                Dsum += Ds_MC;
                Asum += Angle;

                PriorityEntries.Add(new ChargingPriorityEntry() { angle = Angle, distance = Ds_MC, Request = reqPacket });

            }

            holder = MostImportantRequest(PriorityEntries, Dsum, Asum);

            return holder;
        }

        private Packet MostImportantRequest(List<ChargingPriorityEntry> priorityMatrics, double Dsum, double Asum)
        {
            double DAvg = Dsum / priorityMatrics.Count;
            double par = 0.5;

            foreach (var value in priorityMatrics)
            {
                if (value.angle > par)
                {
                    par = value.angle;
                }
            }

            foreach (var value in priorityMatrics)
            {
                if (par > 0.5)
                {
                    double prior = par * (value.angle / Asum) + (1 - par) * (DAvg / value.distance); // bigger aggregate value has high priority
                    value.Priority = prior;
                }
                else
                {
                    double prior = value.angle / Asum + DAvg / value.distance; // bigger aggregate value has high priority
                    value.Priority = prior;
                }
            }

            priorityMatrics.Sort(new TobeChargedSorters()); // it sorts from small to big 
            priorityMatrics.Reverse();
            return priorityMatrics[0].Request;
        }

        //public Packet getNextSojourn(Point MC_pos, List<Packet> reqs, string terr_name)
        //{
        //    double lowest = double.MaxValue;
        //    Packet holder = reqs[0];
        //    double ang = 0.0;
        //    Packet holder2 = reqs[0];
            
        //    string RoadLane = SideWalk(MC_pos, terr_name);
        //    Point endPoint;
        //    Point NetCenter = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);

        //    switch (terr_name)
        //    {
        //        case "topLeft":
        //            endPoint = new Point(0, 0);
        //            break;
        //        case "bottomRight":
        //            endPoint = new Point(PublicParamerters.NetworkSquareSideLength, PublicParamerters.NetworkSquareSideLength);
        //            break;
        //        case "bottomLeft":
        //            endPoint = new Point(0, PublicParamerters.NetworkSquareSideLength);
        //            break;
        //        case "topRight":
        //            endPoint = new Point(PublicParamerters.NetworkSquareSideLength, 0);
        //            break;
        //        default:
        //            endPoint = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);
        //            break;
        //    }

        //    foreach (Packet reqPacket in reqs)
        //    {
        //        double Ds_MC = Operations.DistanceBetweenTwoPoints(MC_pos, reqPacket.Source.CenterLocation);
        //        double Angle = Operations.AngleDotProdection(MC_pos, reqPacket.Source.CenterLocation, endPoint);

        //        if (SideWalk(reqPacket.Source.CenterLocation, terr_name) != RoadLane)
        //        {
        //            if (MC_pos != NetCenter && SideWalkFinished(RoadLane, MC_pos, reqs, terr_name))
        //            {
        //                RoadLane = RoadLane == "above" ? "bellow" : "above";
        //                endPoint = NetCenter;
        //                Angle = Operations.AngleDotProdection(MC_pos, reqPacket.Source.CenterLocation, endPoint);
        //            }
        //            else { continue; }
        //        }

        //        if (Ds_MC < lowest)
        //        {

        //            if (isthereBoderNei(MC_pos, reqs, reqPacket.Source.CenterLocation))
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                lowest = Ds_MC;
        //                holder = reqPacket;
        //            }
        //        }
        //        if (Angle > ang && Ds_MC <= PublicParamerters.CommunicationRangeRadius)
        //        {
        //            ang = Angle;
        //            holder2 = reqPacket;
        //        }

        //    }

        //    return ang > 0.3 ? holder2 : holder;
        //}
        

        //private bool SideWalkFinished(string Lane, Point MCLoc, List<Packet> remPacks, string terr)
        //{
        //    bool finished = true;
        //    foreach (Packet remPack in remPacks)
        //    {
        //        if (SideWalk(remPack.Source.CenterLocation, terr) == Lane)
        //        {
        //            finished = false;
        //        }
        //    }
        //    return finished;
        //}

        public Packet getClosestNode(Point MC_loc, List<Packet> reqs)
        {
            double closer = double.MaxValue;
            Packet holder = reqs[0];

            foreach (Packet reqPacket in reqs)
            {
                double Ds_next = Operations.DistanceBetweenTwoPoints(MC_loc, reqPacket.Source.CenterLocation);

                if (Ds_next < closer)
                {
                    closer = Ds_next;
                    holder = reqPacket;
                }
            }
            return holder;
        }

        private bool isthereBoderNei(Point reference, List<Packet> reqs, Point current)
        {
            int count = 0;

            double x_val = reference.X > 100 ? reference.X : 0;
            double y_val = reference.Y > 100 ? reference.Y : 0;
            string border;
            double Nside = PublicParamerters.NetworkSquareSideLength;
            if (x_val == 0 && y_val == 0)
            {
                border = reference.X <= reference.Y ? "left" : "top";
            }
            else if (x_val != 0 && y_val != 0)
            {
                border = reference.X >= reference.Y ? "right" : "bottom";
            }
            else if (x_val == 0 && y_val != 0)
            {
                border = reference.X <= Math.Abs(reference.Y - Nside) ? "left" : "bottom";
            }
            else //if (x_val != 0 && y_val == 0)
            {
                border = Math.Abs(reference.X - Nside) <= reference.Y ? "right" : "top";
            }

            Point point;
            foreach (Packet reqPacket in reqs)
            {
                switch (border)
                {
                    case "left":
                        point = new Point(0, reqPacket.Source.CenterLocation.Y);
                        break;
                    case "right":
                        point = new Point(Nside, reqPacket.Source.CenterLocation.Y);
                        break;
                    case "top":
                        point = new Point(reqPacket.Source.CenterLocation.X, 0);
                        break;
                    case "bottom":
                        point = new Point(reqPacket.Source.CenterLocation.X, Nside);
                        break;
                    default:
                        point = new Point(0, 0);
                        break;
                }

                double Ds1 = Operations.DistanceBetweenTwoPoints(point, reqPacket.Source.CenterLocation);
                double Ds2 = Operations.DistanceBetweenTwoPoints(reference, reqPacket.Source.CenterLocation);
                double Ds3 = Operations.DistanceBetweenTwoPoints(point, current);
                if (Operations.DistanceBetweenTwoPoints(point, reference) > 100)
                {
                    return false;
                }

                if (Ds2 <= PublicParamerters.CommunicationRangeRadius && Ds1 < Ds3)
                {
                    count++;
                }
            }
            return count > 0;
        }
    }
}
