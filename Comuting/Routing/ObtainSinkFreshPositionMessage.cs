using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.computing;
using RR.Properties;
using RR.RingRouting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RR.Comuting.Routing
{
    /// <summary>
    /// Requesting Sink Position from the Diagonal Nodes. 
    /// The source nodes (i.e., low-tier nodes) that have available data request the sink position from the ring nodes. The requesting mechanism is similar to Reporting the Sink Position, explained in the previous sub-section. Given a source node n_s located at S(x_s,y_s), the point on diagonal which is closest to S(x_s,y_s) has coordinates: Z(x_s+y_s/2,x_s+y_s/2), and this constructs the shortest distance from the source node to diagonal line. Therefore, the relay nodes are selected aligned with line segment (SZ) ⃡. This can be implemented as in Eq.(13). Also, the response path from a DVL node to the source node is computed by the same mechanism.
    /// </summary>
    class ObtainSinkFreshPositionMessage
    {
        private LoopMechanizimAvoidance loopMechan=new LoopMechanizimAvoidance();
        private NetworkOverheadCounter counter;
        /// <summary>
        /// obtian the position for all sinks.
        /// </summary>
        /// <param name="sourceNode"></param>
        public ObtainSinkFreshPositionMessage(Sensor sourceNode)
        {

            counter = new NetworkOverheadCounter();

            // the high tier node has data. 
            if (sourceNode.RingNodesRule.isRingNode)
            {
                //Directly send data to the access nodes that u have 
                //No need to send a query request and wait for response 
                Packet pack= GeneratePacket(sourceNode,true);
                pack.SinkIDsNeedsRecovery = null;
                counter.SuccessedDeliverdPacket(pack); // considere the obtian packet as delivred.
                new ResonseSinkPositionMessage(sourceNode, sourceNode);
            }
            else 
            {
                //Regular sensor node disseminating data start with QReq
                sourceNode.Ellipse_nodeTypeIndicator.Fill = Brushes.Yellow;
                Packet QReq = GeneratePacket(sourceNode,true);
                QReq.SinkIDsNeedsRecovery = null;
                SendPacket(sourceNode, QReq);
            }
        }       

        /// <summary>
        /// just to handel the in queue packets.
        /// </summary>
        public ObtainSinkFreshPositionMessage() 
        {
            counter = new NetworkOverheadCounter();
        }

        public void HandelInQueuPacket(Sensor currentNode, Packet InQuepacket)
        {
            SendPacket(currentNode, InQuepacket);
        }



        private Packet GeneratePacket(Sensor sourceNode, bool IncreasePid)
        {
            //This is for the query request
            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = sourceNode;
            pck.Path = "" + sourceNode.ID;
            pck.Destination = null;
            pck.DestinationPoint = sourceNode.getDestinationForRingAccess();
            pck.PacketType = PacketType.ObtainSinkPosition;
            pck.PID = PublicParamerters.NumberofGeneratedPackets;
            pck.TimeToLive = Convert.ToInt16((Operations.DistanceBetweenTwoPoints(sourceNode.CenterLocation, pck.DestinationPoint) / (PublicParamerters.CommunicationRangeRadius / 3)));
            counter.DisplayRefreshAtGenertingPacket(sourceNode, PacketType.ObtainSinkPosition);
            return pck;
        }


        private void SendPacket(Sensor sender, Packet pck)
        {
            if (pck.PacketType == PacketType.ObtainSinkPosition)
            {

                if (sender.RingNeighborRule.isNeighbor)
                {
                    if (sender.RingNeighborRule.NearestRingNode != null)
                    {
                        RingNeighborSendPacket(sender, pck);
                        return;
                    }
                }
                sender.SwichToActive(); // switch on me.
                // neext hope:
                Sensor Reciver = SelectNextHop(sender, pck);
                if (Reciver != null)
                {
                    // overhead:
                    counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, Reciver);
                    counter.Animate(sender, Reciver, pck);
                    RecivePacket(Reciver, pck);
                }
                else
                {
                    counter.SaveToQueue(sender, pck);
                }
            }
        }

        private void RingNeighborSendPacket(Sensor sender, Packet pck)
        {
            Sensor Reciver;
            if (Operations.isInMyComunicationRange(sender, sender.RingNeighborRule.NearestRingNode))
            {
                Reciver = sender.RingNeighborRule.NearestRingNode;
                if (Reciver.CurrentSensorState == SensorState.Active)
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
                if (Reciver.IsHightierNode) // packet is recived.
                {
                    packt.Destination = Reciver;
                    counter.SuccessedDeliverdPacket(packt);
                    counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                    counter.DisplayRefreshAtReceivingPacket(Reciver);
                    // response to the source:
                    if (packt.SinkIDsNeedsRecovery == null) // nomal
                    {
                        new ResonseSinkPositionMessage(Reciver, packt.Source);
                    }
                    else
                    {
                        // recovery packet.
                        new ResonseSinkPositionMessage(Reciver, packt.Source, packt.SinkIDsNeedsRecovery);
                    }

                    if (Reciver.ReachedBatterThresh)
                    {
                        RingNodeChange rch = new RingNodeChange();
                        rch.ChangeRingNode(Reciver.RingNodesRule);
                    }
                }
                else
                {
                    // compute the overhead:
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
