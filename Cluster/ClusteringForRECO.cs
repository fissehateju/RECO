using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RR.ui;
using RR.Properties;
using System.Windows.Threading;
using RR.ui.conts;
using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Comuting.computing;
using RR.NetAnimator;
using RR.Comuting.Routing;
using RR.Comuting.SinkClustering;
using RR.RingRouting;
using RR.Models.Energy;
using MP.MergedPath.Routing;

namespace RR.Cluster
{
    public class ClusteringForRECO
    {
        public int Id { get; set; }        
        public Point TopLeft = new Point();
        public Point BottomRight = new Point();
        public Point BottomLeft;
        public Point TopRight;
        
        public List<Sensor> MemberNodes = new List<Sensor>();
        public double height { get; set; }
        public double width { get; set; }
        public List<Packet> Requests = new List<Packet>();
        public Sink VistingSink { get; set; }
        public Point centerPoint
        {
            get
            {
                return new Point(TopLeft.X + (width / 2), TopLeft.Y + (height / 2));
            }

        }
        public string name
        {
            get
            {
                int xx = (int)centerPoint.X - (int)PublicParamerters.NetworkSquareSideLength / 2;
                int yy = (int)centerPoint.Y - (int)PublicParamerters.NetworkSquareSideLength / 2;
                if (xx < 0 && yy < 0) { return "topLeft"; }
                if (xx > 0 && yy > 0) { return "bottomRight"; }
                if (xx < 0 && yy > 0) { return "bottomLeft"; }
                if (xx > 0 && yy < 0) { return "topRight"; }
                return null;
            }
        }


        public Point EndPoint
        {
            get
            {
                Point NetCenter = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);

                double TLDis = Operations.DistanceBetweenTwoPoints(NetCenter, TopLeft);
                double BRDis = Operations.DistanceBetweenTwoPoints(NetCenter, BottomRight);
                double BLDis = Operations.DistanceBetweenTwoPoints(NetCenter, BottomLeft);
                double TRDis = Operations.DistanceBetweenTwoPoints(NetCenter, TopRight);
                if(name == "topLeft" || name == "bottomRight")
                {
                    return TLDis > BRDis ? TopLeft : BottomRight;
                }
                else
                {
                    return BLDis > TRDis ? BottomLeft : TopRight;
                }
                
            }
        }

        public double AverageRemainingEnergy
        {
            get
            {
                double total = 0;
                foreach (Packet packet in Requests)
                {
                    //total += packet.remainingEnergy_Joule; // this doesnt give updated value so better consider the request order or information directly from sensor
                    total += packet.Source.ResidualEnergy;
                }
                return total / Requests.Count;
            }
        }

        public int numberOfRequest
        {
            get
            {
                return Requests.Count;
            }
        }


        public ClusteringForRECO(int id, Point tl, Point br)
        {
            Id = id;
            TopLeft = tl;
            BottomRight = br;
            BottomLeft = new Point(TopLeft.X, BottomRight.Y);
            TopRight = new Point(BottomRight.X, TopLeft.Y);
        }

        
    }
}
