using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.computing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RR.Comuting.Routing
{
    class GreedyRoutingMechansims
    {
        private NetworkOverheadCounter counter;

        public GreedyRoutingMechansims()
        {
            counter = new NetworkOverheadCounter();
        }

        /// <summary>
        /// good perforamce
        /// </summary>
        /// <param name="ni"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public Sensor Greedy1(Sensor ni, Packet packet) 
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            Point endPoint = (packet.Destination != null) ? packet.Destination.CenterLocation : packet.DestinationPoint;

            Sensor sj = null;
            double sum = 0;
            foreach (Sensor nj in ni.NeighborsTable)
            {
                if (nj.ResidualEnergyPercentage > 0)
                {
                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, endPoint);
                    double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);
                    if (Norangle < 0.5)
                    {
                        double aggregatedValue = dj * Norangle;
                        sum += aggregatedValue;
                        coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                    }
                }
            }
            // coordination"..... here
            sj = counter.CoordinateGetMin(coordinationEntries, packet, sum);
            return sj;
        }

         
        /// <summary>
        /// worsre performance
        /// </summary>
        /// <param name="ni"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public Sensor Greedy2(Sensor ni, Packet packet)
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            Point endPoint = (packet.Destination != null) ? packet.Destination.CenterLocation : packet.DestinationPoint;
            Sensor sj = null;
            double sum = 0;
            foreach (Sensor nj in ni.NeighborsTable)
            {
                if (nj.ResidualEnergyPercentage > 0)
                {
                    double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);
                    double aggregatedValue = dj;
                    sum += aggregatedValue;
                    coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                }
            }
            // coordination"..... here
            sj = counter.CoordinateGetMin(coordinationEntries, packet, sum);
            return sj;
        }

        /// <summary>
        /// showed the best performance.
        /// </summary>
        /// <param name="ni"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public Sensor Greedy3(Sensor ni, Packet packet) 
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            Point endPoint = (packet.Destination != null) ? packet.Destination.CenterLocation : packet.DestinationPoint;

            Sensor sj = null;
            double sum = 0;
            foreach (Sensor nj in ni.NeighborsTable)
            {
                if (nj.ResidualEnergyPercentage > 0)
                {
                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, endPoint);
                    double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);
                    if (Norangle < 0.5)
                    {
                        double aggregatedValue = dj;
                        sum += aggregatedValue;
                        coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                    }
                }
            }
            // coordination"..... here
            sj = counter.CoordinateGetMin(coordinationEntries, packet, sum);
            return sj;
        }



        public Sensor Greedy4(Sensor ni, Packet packet)
        {
            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            Point endPoint = (packet.Destination != null) ? packet.Destination.CenterLocation : packet.DestinationPoint;

            Sensor sj = null;
            double sum = 0;

            switch (packet.PacketType)
            {
                case PacketType.ObtainSinkPosition:
                    {
                        foreach (Sensor nj in ni.NeighborsTable)
                        {
                            if (nj.ResidualEnergyPercentage > 0)
                            {
                                if (nj.IsHightierNode)
                                {

                                    double aggregatedValue = 1;
                                    sum += aggregatedValue;
                                    coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                                    //return nj;
                                }
                                else
                                {
                                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, endPoint);
                                    double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);
                                    if (Norangle < 0.5)
                                    {
                                        double aggregatedValue = dj;
                                        sum += aggregatedValue;
                                        coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                                    }
                                    else if (Norangle == 0 || Double.IsNaN(Norangle))
                                    {
                                        double aggregatedValue = 1;
                                        sum += aggregatedValue;
                                        coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                                    }
                                }
                            }
                        }
                    }
                    break;
                case PacketType.ReportSinkPosition:
                    {
                        foreach (Sensor nj in ni.NeighborsTable)
                        {
                            if (nj.ResidualEnergyPercentage > 0)
                            {
                                if (nj.IsHightierNode) return nj;
                                else
                                {
                                    double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, endPoint);
                                    double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);
                                    if (Norangle == 0 || Double.IsNaN(Norangle))
                                    {
                                        double aggregatedValue = 1;
                                        sum += aggregatedValue;
                                        coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                                    }
                                    else if (Norangle < 0.5 )
                                    {

                                        double aggregatedValue = dj;
                                        sum += aggregatedValue;
                                        coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                                    }
                                }
                            }
                        }
                    }
                    break;
                default:
                    {
                        // defaul greedy:
                        foreach (Sensor nj in ni.NeighborsTable)
                        {
                            if (nj.ResidualEnergyPercentage > 0)
                            {
                                double Norangle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, endPoint);
                                double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);

                                if (Norangle == 0 || Double.IsNaN(Norangle))
                                {
                                    double aggregatedValue = 1;
                                    sum += aggregatedValue;
                                    coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                                }
                                else if (Norangle < 0.5)
                                {
                                    double aggregatedValue = dj;
                                    sum += aggregatedValue;
                                    coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                                }
                            }
                        }
                    }
                    break;
            }

            // coordination"..... here
            sj = counter.CoordinateGetMin1(coordinationEntries, packet, sum);
            return sj;
        }

        public Sensor RrGreedy(Sensor ni, Packet packet)
        {
            return Greedy4(ni, packet);
        }

    }
}
