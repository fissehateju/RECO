using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Energy;
using RR.Intilization;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RR.Models.Charging
{
    internal class StartTSM
    {
        public List<Location> reqLocat = new List<Location>();
        public List<int> orderedID = new List<int>();
        public List<int> Startit(List<Packet> requests)
        {
            bool zeroisthere = false;

            foreach (Packet packet in requests)
            {
                reqLocat.Add(new Location(packet.Source.ID, packet.Source.CenterLocation));

                if (packet.Source.ID == 0)
                { 
                    zeroisthere = true; 
                }
            }
            if (!zeroisthere)
            {
                reqLocat.Insert(0, new Location(0, new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2)));
            }

            var problem = new TravellingSalesmanAlg(reqLocat); 
           
            var route = problem.Solve();
            foreach (Location loc in route.Locations)
            {
                orderedID.Add(loc.ID);
            }
            return orderedID;
        }
    }
}
