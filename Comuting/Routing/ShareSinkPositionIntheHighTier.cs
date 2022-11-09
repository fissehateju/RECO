using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.computing;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RR.Comuting.Routing
{
    public enum PacketDirection { OmiDirection, Left, Right }

    /// <summary>
    /// This selection mechanism is repeated until the packet that holds the new position of the mobile sink is received by an DVL node, say n_v which is the closest to the point  V on the diagonal. Then, n_v shares the new position with all DVL nodes via one-hop or multiple hops.
    /// </summary>
    class ShareSinkPositionIntheHighTier
    {
        private LoopMechanizimAvoidance loopMechan = new LoopMechanizimAvoidance();
        private NetworkOverheadCounter counter;
        public ShareSinkPositionIntheHighTier(Sensor ringNodeGateway, AgentsRow reportSinkPositionRow)
        {
            if (ringNodeGateway.IsHightierNode)
            {
                ringNodeGateway.AddSinkRecordInHighTierNode(reportSinkPositionRow);


                counter = new NetworkOverheadCounter();
                Packet clock = GeneragtePacket(ringNodeGateway, reportSinkPositionRow);
                clock.PacketDirection = PacketDirection.Left;
                Packet anticlock = GeneragtePacket(ringNodeGateway, reportSinkPositionRow);
                anticlock.PacketDirection = PacketDirection.Right;
                SendPacket(ringNodeGateway, clock);
                SendPacket(ringNodeGateway, anticlock);


                //: SAVE Sink positions.// this is not agent record. becarful here
                

            }
        }

        public ShareSinkPositionIntheHighTier()
        {
            counter = new NetworkOverheadCounter();
        }

        public void HandelInQueuPacket(Sensor currentNode, Packet InQuepacket)
        {
            SendPacket(currentNode, InQuepacket);
        }

        private Packet GeneragtePacket(Sensor firstRingNode, AgentsRow reportSinkPositionRow )
        {

            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = firstRingNode;
            pck.agentsRowT = reportSinkPositionRow;
            pck.Path = "" + firstRingNode.ID;
            pck.Destination = null;
            pck.PacketType = PacketType.ShareSinkPosition;
            pck.PID = PublicParamerters.NumberofGeneratedPackets;
            counter.DisplayRefreshAtGenertingPacket(firstRingNode, PacketType.ShareSinkPosition);
            return pck;
        }

        private void SendPacket(Sensor sender, Packet pck)
        {
            if (pck.PacketType == PacketType.ShareSinkPosition)
            {
                Sensor destination;


                if (pck.PacketDirection == PacketDirection.Right)
                {
                    destination = sender.RingNodesRule.AntiClockWiseNeighbor;
                }
                else
                {
                    destination = sender.RingNodesRule.ClockWiseNeighbor;
                }

                if (destination != null)
                {
                    if (!Operations.isInMyComunicationRange(sender, destination))
                    {
                        pck.Destination = destination;
                        SendANPISWithRelay(sender, pck);
                        return;
                    }
                    else
                    {
                        counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, destination);
                        counter.Animate(sender, destination, pck);
                        RecivePacket(destination, pck);
                    }
                }
                else
                {
                    //Drop the packet something went wrong
                    counter.DropPacket(pck, sender, PacketDropedReasons.RingNodesError);
                }
            }
        }

        private void SendANPISWithRelay(Sensor sender, Packet pck)
        {
           Sensor Reciver = SelectNextHop(sender, pck);
            if (Reciver != null)
            {
                sender.SwichToActive(); // switch on me.
                counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, Reciver);
                counter.Animate(sender, Reciver, pck);
                RecivePacket(Reciver, pck);
            }
            else
            {
                // wait:
                counter.SaveToQueue(sender, pck);
            }
        }

        private void RecivePacket(Sensor Reciver, Packet packt)
        {
            packt.Path += ">" + Reciver.ID;
            if (loopMechan.isLoop(packt))
            {
                counter.DropPacket(packt, Reciver, PacketDropedReasons.Loop);
            }
            else
            {
                packt.ReTransmissionTry = 0;                

                if (Reciver.IsHightierNode)
                {
                    if (Reciver.AlreadyRecievedAgentInfo(packt.agentsRowT)) // packet is recived.
                    {
                        // Console.WriteLine("****Success ANPIS : " + Reciver.ID);
                        counter.SuccessedDeliverdPacket(packt);
                        counter.DisplayRefreshAtReceivingPacket(packt.Source);

                    }
                    else
                    {
                        Reciver.AddSinkRecordInHighTierNode(packt.agentsRowT); // keep track of the sink position
                        Reciver.agentRow = packt.agentsRowT;
                        counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                        SendPacket(Reciver, packt);
                    }
                }
                else
                {

                    if (packt.Hops <= packt.TimeToLive)
                    {
                        SendPacket(Reciver, packt);
                    }
                    else
                    {
                        counter.DropPacket(packt, Reciver, PacketDropedReasons.TimeToLive);
                    }
                }
            }
          
        }


        private Sensor SelectNextHop(Sensor ni, Packet packet)
        {
            Point endPoint = (packet.Destination != null) ? packet.Destination.CenterLocation : packet.DestinationPoint;

            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            Sensor sj = null;
            double sum = 0;
            foreach (Sensor nj in ni.NeighborsTable)
            {
                double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);
                double aggregatedValue = dj;
                coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                sum += aggregatedValue;
            }
            // coordination"..... here
            sj = counter.CoordinateGetMin(coordinationEntries, packet, sum);

            return sj; ;
        }
    }
}
