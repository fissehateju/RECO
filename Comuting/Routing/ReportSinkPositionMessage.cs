using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.computing;
using RR.Comuting.SinkClustering;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using static RR.Comuting.computing.Sorters;

namespace RR.Comuting.Routing
{
    public class SinksAgentsRow
    {
        public Sink Sink { get; set; }
        public Sensor AgentNode { get; set; }
        public Point RingAccessPointDestination { get; set; } ////Given a mobile sink m_j located at U(x ̇_j,y ̇_j), the point on diagonal which is the closest to  U(x ̇_j,y ̇_j) has coordinates: V(x ̇_j+y ̇_j/2,x ̇_j+y ̇_j/2), 

        internal ICloneable Clone()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// The sink continuously selects one of its neighbors as an agent to delegate the communication between DVL nodes and other sensor nodes. More details on how the sink selects its agent are explained in [7]. 
    /// Here in this sub-section we focused on discovering the reporting path to advertise the new sink’s position. 
    /// Given a mobile sink m_j located at U(x ̇_j,y ̇_j), the point on diagonal which is the closest to  U(x ̇_j,y ̇_j) has coordinates: V(x ̇_j+y ̇_j/2,x ̇_j+y ̇_j/2), and this constructs the shortest distance from the sink to diagonal line. Accordingly, shortest routing path to advertise the sink’s fresh position is computed as follows. The sink’s agent node n_i picks up one its one neighbor n_k, located at (x_k,y_k ), as a relay node or next-hop if n_k   meets two conditions.  First, n_k has the shortest perpendicular distance to (UV) ⃡. The  perpendicular distance from n_k to the line segment (UV) ⃡, denoted by ψ ̂_(i,k), is given by Eq.(14). Second, n_k should be the closest to the point V. The proximity of n_k to the point V, denoted by θ ̂_(i,k) , is expressed by the cosine angle between two Euclidean vectors a ⃗=(x_k-x_i,y_k-y_i) and   c ⃗=((x ̇_j+y ̇_j)⁄2-x_i,(x ̇_j+y ̇_j)⁄2-y_i). These two conditions are aggregated by Eq.(13) such that a higher priority is assigned to the nodes that satisfy the two condition mentioned above. This selection mechanism is repeated until the packet that holds the new position of the mobile sink is received by an DVL node, say n_v which is the closest to the point  V on the diagonal. Then, n_v shares the new position with all DVL nodes via one-hop or multiple hops.
    /// </summary>
    public class ReportSinkPositionMessage
    {
        private LoopMechanizimAvoidance loopMechan = new LoopMechanizimAvoidance();
        private NetworkOverheadCounter counter;
        public ReportSinkPositionMessage(AgentsRow reportSinkPosition)
        {
            counter = new NetworkOverheadCounter();
            reportSinkPosition.RingAccessPointDestination = reportSinkPosition.Sink.getDestinationForRingAccess();
            Packet ANPI = GeneratePacket(reportSinkPosition);
            SendPacket(reportSinkPosition.MainAgentNode, ANPI);
          
        }

        public ReportSinkPositionMessage()
        {
            counter = new NetworkOverheadCounter();
        }

        /// <summary>
        ///  here the sender should not be the agent.
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="InQuepacket"></param>
        public void HandelInQueuPacket(Sensor currentNode, Packet InQuepacket)
        {
            SendPacket(currentNode, InQuepacket);
        }





        private Packet GeneratePacket(AgentsRow reportSinkPosition)
        {

            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = reportSinkPosition.MainAgentNode;
            pck.Path = "" + reportSinkPosition.MainAgentNode.ID;
            pck.Destination = null; // has no destination.
            pck.PacketType = PacketType.ReportSinkPosition;
            pck.DestinationPoint = reportSinkPosition.RingAccessPointDestination;
            pck.PID = PublicParamerters.NumberofGeneratedPackets;
            pck.agentsRowT = reportSinkPosition;
            pck.TimeToLive = Convert.ToInt16((Operations.DistanceBetweenTwoPoints(reportSinkPosition.MainAgentNode.CenterLocation, reportSinkPosition.RingAccessPointDestination) / (PublicParamerters.CommunicationRangeRadius / 3)));
            pck.TimeToLive += PublicParamerters.HopsErrorRange;
            counter.DisplayRefreshAtGenertingPacket(reportSinkPosition.MainAgentNode, PacketType.ReportSinkPosition);
            return pck;
        }

       

        private void SendPacket(Sensor sender, Packet pck)
        {
            if (pck.PacketType == PacketType.ReportSinkPosition)
            {
                sender.agentRow = pck.agentsRowT;
                // neext hope:
                Sensor Reciver;
                sender.SwichToActive(); // switch on me.
                if (sender.RingNeighborRule.isNeighbor)
                {
                    if(sender.RingNeighborRule.NearestRingNode != null)
                    {
                        RingNeighborSendPacket(sender, pck);
                        return;
                    }
                }


                Reciver = SelectNextHop(sender, pck);
                if (Reciver != null)
                {
                    counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, Reciver);
                    counter.Animate(sender, Reciver, pck);

                    Reciver.numOfPacketsPassingThrough += 1;
                    RecivePacket(Reciver, pck);
                }
                else
                {
                    // wait:
                    counter.SaveToQueue(sender, pck);
                }
            }
        }


        private void RingNeighborSendPacket(Sensor sender, Packet pck){
             Sensor Reciver;
             if (Operations.isInMyComunicationRange(sender, sender.RingNeighborRule.NearestRingNode))
             {
                Reciver = sender.RingNeighborRule.NearestRingNode;
                if(Reciver.CurrentSensorState == SensorState.Active)
                {
                    counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, Reciver);
                    counter.Animate(sender, Reciver, pck);
                    RecivePacket(Reciver, pck);
                }
                else
                {
                    counter.SaveToQueue(sender, pck);
                }
            }
            else
            {
                pck.Destination = sender.RingNeighborRule.NearestRingNode;
                Reciver = SelectNextHop(sender, pck);
                if (Reciver != null)
                {
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
                if (Reciver.RingNodesRule.isRingNode) // packet is recived.
                {

                    packt.Destination = Reciver;
                    Reciver.agentRow = packt.agentsRowT;

                    counter.SuccessedDeliverdPacket(packt);
                    counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                    counter.DisplayRefreshAtReceivingPacket(Reciver);
                    // share:

                    ShareSinkPositionIntheHighTier xma = new ShareSinkPositionIntheHighTier(Reciver, packt.agentsRowT);

                }
                else
                {

                    counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
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
            return new GreedyRoutingMechansims().RrGreedy(ni, packet);
        }




    }
}
