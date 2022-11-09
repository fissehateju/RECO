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
    public class NetCluster
    {
        public int Id { get; set; }
        public Point centerPoint
        {
            get
            {
                return new Point(TopLeft.X + (width / 2), TopLeft.Y + (height / 2));
            }

        }
        public Point TopLeft = new Point();
        public Point BottomRight = new Point();
        public List<Sensor> MemberNodes = new List<Sensor>();
        public Sensor Header { get; set; }
        public double height { get; set; }
        public double width { get; set; }

        public NetCluster()
        {

        }
        public NetCluster(int id, Point tl, Point br)
        {
            Id = id;

            TopLeft = tl;
            BottomRight = br;
        }

    }
}
