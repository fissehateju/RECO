using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Comuting.Routing;
using System.Collections.Generic;
using System.Windows.Media;

namespace RR.RingRouting
{
    public partial class RingNodeCandidates
    {
        public Sensor Node { get; set; }
        public double Distance { get; set; }

        public RingNodeCandidates(Sensor me, double distance)
        {
            Node = me;
            Distance = distance;
        }


    }
    public class RingNodes
    {
        public Sensor Node { get; set; }
        public Sensor ClockWiseNeighbor { get; set; }
        public Sensor AntiClockWiseNeighbor { get; set; }
        public bool isRingNode { get; set; }
        public Dictionary<int, AgentsRow> AnchorNodes { get; set; }

        public RingNodes()
        {
            isRingNode = false;

           // Node.IsHightierNode = false;
        }

        public RingNodes(Sensor me, Sensor next, Sensor prev)
        {
            Node = me;
            AntiClockWiseNeighbor = next;
            ClockWiseNeighbor = prev;
            isRingNode = true;
            Node.IsHightierNode = true;
            AnchorNodes = new Dictionary<int, AgentsRow>();
            Node.Ellipse_nodeTypeIndicator.Fill = Brushes.LightSlateGray; // mark


        }

        public bool haveAllSinksPosition()
        {
            // bool haveAll = false;

            foreach (KeyValuePair<int, AgentsRow> pair in AnchorNodes)
            {
                if (pair.Value == null) return false;
            }
            return true;
        }

      

    }
}