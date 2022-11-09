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
using RR.Cluster;

namespace RR.Comuting.Routing
{
    class ShareSinkPosition
    {
        private LoopMechanizimAvoidance loopMechan = new LoopMechanizimAvoidance();
        private NetworkOverheadCounter counter;
        private Queue<Packet> queue = new Queue<Packet>();
        private AgentsRow agentNodes { get; set; }
        public Sensor source = PublicParamerters.MainWindow.myNetWork[0];
        public List<NetCluster> clusters = new List<NetCluster>();
        public ShareSinkPosition(AgentsRow agentsRow, List<NetCluster> _clusters)
        {
            counter = new NetworkOverheadCounter();
            agentNodes = agentsRow;
            clusters = _clusters;

            Packet packt;

            packt = GeneragtePacket(source);
            DetermineDestination(source, packt);
        }

        public ShareSinkPosition()
        {
            counter = new NetworkOverheadCounter();
        }

        public void HandelInQueuPacket(Sensor currentNode, Packet InQuepacket)
        {
            SendPacket(currentNode, InQuepacket);
        }

        private Packet GeneragtePacket(Sensor sender)
        {
            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = sender;
            pck.Path = "" + sender.ID;
            pck.PacketType = PacketType.ReportSojournPoints;
            pck.agentsRowT = agentNodes;
            pck.PID = PublicParamerters.NumberofGeneratedPackets;
            counter.DisplayRefreshAtGenertingPacket(pck.Source, PacketType.ReportSojournPoints);
            return pck;
        }


        private void DetermineDestination(Sensor sender, Packet Pack)
        {

            foreach (NetCluster dest in clusters)
            {
                Packet newpack = Pack.Clone() as Packet;
                newpack.Destination = dest.Header;
                newpack.Hops = 0;
                newpack.ReTransmissionTry = 0;
                double DIS = Operations.DistanceBetweenTwoPoints(newpack.Source.CenterLocation, newpack.Destination.CenterLocation);
                newpack.TimeToLive = 3 + Convert.ToInt16(DIS / PublicParamerters.CommunicationRangeRadius);

                SendPacket(sender, newpack);
            }
        }
        private void SendPacket(Sensor sender, Packet pck)
        {
            if (pck.Destination.ID != sender.ID)
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
            else
            {
                //Drop the packet something went wrong
                MessageBox.Show("Sending to self");
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
                if (Reciver == packt.Destination)
                {
                    packt.isDelivered = true;
                    counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                    counter.DisplayRefreshAtReceivingPacket(packt.Source);

                    Reciver.agentRow = packt.agentsRowT;

                    if (Settings.Default.SavePackets)
                        PublicParamerters.FinishedRoutedPackets.Add(packt);
                    else
                        packt.Dispose();

                }
                else
                {
                    if (packt.Hops <= packt.TimeToLive)
                    {
                        counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
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
            sj = counter.CoordinateGetMinForSojourn(coordinationEntries, packet, sum);

            return sj; ;
        }

    }
}

