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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RR.Comuting.Routing
{
    /// <summary>
    /// Taking the diagonal line y-x=0 with two known points W(x_1,y_1 ) and Q(x_2,y_2), the DVL nodes are decentrally found by the following mechanism. Given the closest node, denoted by n_i, to the diagonal point (x_1,y_1 ). The node n_i nominates one of its neighbors, say n_j, if n_j satisfies two conditions, expressed by Eq.(12). First, the node n_j has the smallest point-to-line distance, denoted by ψ_(i,j), to the diagonal line. Second, The normalized angle from n_i to n_j with respect to point W, denoted by χ_(i,j), is smaller or equal to 〖0.5〗^°. The node n_j with χ_(i,j)>〖0.5〗^°are ignored since it is located behind the node n_i. We used L to denote the set of DVL nodes. The lower bound of |L| is computed through dividing the diagonal’s length by the half of communication range, |L|=(2√2 s)/δ.
    /// </summary>
    public class RingConstructionMessage
    {
        private LoopMechanizimAvoidance loopMechan = new LoopMechanizimAvoidance();
        private Canvas can;
        private NetworkOverheadCounter counter;
        /// <summary>
        /// Sensor anchor1, Sensor anchor2 are the two points of the diagonal line.
        /// </summary>
        /// <param name="anchor1"></param>
        /// <param name="anchor2"></param>
        /// <param name="canvas"></param>
        public RingConstructionMessage(Sensor anchor1, Sensor anchor2, Canvas canvas)
        {
            if (anchor1 != null && anchor2 != null)
            {
                can = canvas;
                counter = new NetworkOverheadCounter();
                Packet pck = GeneratePacket(anchor1, anchor2); // 
                SendPacket(anchor1, pck); // start from the node 1
            }
        }

        /// <summary>
        /// generate a DiagonalVirtualLineConstruction packet.
        /// </summary>
        /// <param name="scr">anchor1</param>
        /// <param name="des">anchor2</param>
        /// <returns></returns>
        private Packet GeneratePacket(Sensor scr, Sensor des)
        {
            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = scr;
            pck.Path = "" + scr.ID;
            pck.Destination = des;
            pck.PacketType = PacketType.RingConstruction;
            pck.PID = PublicParamerters.NumberofGeneratedPackets;
            pck.TimeToLive = System.Convert.ToInt16((Operations.DistanceBetweenTwoPoints(scr.CenterLocation, des.CenterLocation) / (PublicParamerters.CommunicationRangeRadius / 3)));
            counter.DisplayRefreshAtGenertingPacket(scr, PacketType.RingConstruction);

            return pck;
        }


        private void SendPacket(Sensor sender, Packet pck) 
        {
            if (pck.PacketType == PacketType.RingConstruction)
            {
                sender.SwichToActive(); // switch on me.
                // neext hope:
                Sensor Reciver = SelectNextHop(sender,pck.Destination);
                // overhead:
                counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, Reciver);
                // make the indicate:
                sender.Ellipse_nodeTypeIndicator.Fill = Brushes.LightSlateGray; // 
                sender.IsHightierNode = true;
                sender.RightVldNeighbor = Reciver;
                Reciver.LeftVldNeighbor = sender;
                // Operations.DrawLine(can,sender.CenterLocation, Reciver.CenterLocation);
                counter.Animate(sender, Reciver, pck);

                RecivePacket(Reciver, pck);
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

                    counter.SuccessedDeliverdPacket(packt);
                    counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                    Reciver.Ellipse_nodeTypeIndicator.Fill = Brushes.LightSlateGray; // mark
                    Reciver.IsHightierNode = true;

                    counter.DisplayRefreshAtReceivingPacket(Reciver);
                }
                else
                {
                    if (packt.Hops <= packt.TimeToLive)
                    {
                        // compute the overhead:
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

        /// <summary>
        /// Eq.(12)
        /// </summary>
        /// <param name="ni"></param>
        /// <returns></returns>
        public Sensor SelectNextHop(Sensor ni, Sensor des)
        {
            Sensor sj = null;
            double minDis = double.MaxValue;
            foreach (Sensor nj in ni.NeighborsTable)
            {

                nj.VLDNodesNeighbobre.Add(ni); // just add the

                double s = PublicParamerters.NetworkSquareSideLength;
                double xj = nj.CenterLocation.X;
                double yj = nj.CenterLocation.Y;

                double angle = Operations.AngleDotProdection(ni.CenterLocation, nj.CenterLocation, des.CenterLocation);
                if (Double.IsNaN(angle))
                {
                    sj = nj;
                    return sj;
                }
                else if (angle <= 0.5)
                {
                    double pointToLineDistance = Math.Abs(s * (xj - yj)) / (Math.Sqrt(2 * s * s)); // Eq.12
                    if (pointToLineDistance < minDis)
                    {
                        minDis = pointToLineDistance;
                        sj = nj;
                    }
                }
            }

            return sj;
        }


        

    }
}
