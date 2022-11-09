using RR.Dataplane;
using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.RingRouting
{
    public class RingNeighbor
    {
        public List<Sensor> NeighboringRingNodes = new List<Sensor>();
        public Sensor NearestRingNode { get; set; }
        public Sensor Node { get; set; }
        public bool isNeighbor { get; set; }

        public RingNeighbor(Sensor me)
        {
            Node = me;
            isNeighbor = false;
        }

        public void getNearestRingNode()
        {
            double min = 100;
            if (NeighboringRingNodes.Count == 1)
            {
                NearestRingNode = NeighboringRingNodes[0];
                return;
            }
            else
            {
                Sensor holder = null;
                foreach (Sensor nei in NeighboringRingNodes)
                {
                    double distance = Operations.DistanceBetweenTwoSensors(Node, nei);
                    if (distance < min)
                    {
                        min = distance;
                        holder = nei;
                    }
                }
                NearestRingNode = holder;
            }
        }


    }
}
