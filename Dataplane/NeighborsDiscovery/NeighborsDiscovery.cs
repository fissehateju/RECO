using System.Collections.Generic;
using RR.Dataplane;
using System.Windows;
using RR.Intilization;
using RR.Dataplane.PacketRouter;

namespace RR.DataPlane.NeighborsDiscovery
{
    public class NeighborsDiscovery
    {
       private List<Sensor> Network;
       public NeighborsDiscovery(List<Sensor> _NetworkNodes)
       {
           Network=_NetworkNodes; 
       }
        /// <summary>
        /// only those nodes which follow within the range of i.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private void DiscoverMyNeighbors(Sensor i)
        {
            i.NeighborsTable = new List<Sensor>();
            // get the overlapping nodes:
            if (Network != null)
            {
                if (Network.Count > 0)
                {
                    foreach (Sensor node in Network)
                    {
                        if (i.ID != node.ID)
                        {
                            bool isOverlapped = Operations.isInMyComunicationRange(i, node);
                            if (isOverlapped)
                            {
                                i.NeighborsTable.Add(node);
                            }
                        }
                    }
                }
            }
        }

       /// <summary>
       /// for all nodes inside the newtwork find the overllapping nodes.
       /// </summary>
       public void GetOverlappingForAllNodes()
       {
           foreach(Sensor node in Network)
           {
               DiscoverMyNeighbors(node);
           }
       }









    }
}
