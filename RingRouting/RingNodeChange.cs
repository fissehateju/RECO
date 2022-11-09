using RR.Dataplane;
using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RR.RingRouting
{
    public class RingNodeChange
    {
        private List<RingNodeCandidates> ClosestNodestoRN = new List<RingNodeCandidates>();
        private List<Sensor> Candidates = new List<Sensor>();
        private List<int> RingNodeNeighborsIDS = new List<int>();

        #region Operations
        private static bool isClockwise(Point a, Point b, Point c)
        {


            double value = ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));

            if (value >= 0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        private static void ShouldNodeExpand(Sensor node)
        {

            double distanceToCenter = Operations.DistanceBetweenTwoPoints(node.CenterLocation, PublicParamerters.networkCenter);
            if (distanceToCenter < (PublicParamerters.clusterRadius))
            {
                node.isExpanding = true;
            }

        }
        private void markNeighboringRingNods()
        {
            foreach (RingNodes ringNode in PublicParamerters.RingNodes)
            {
                foreach (Sensor entry in ringNode.Node.NeighborsTable)
                {

                    Sensor nei = entry;
                    if (!nei.RingNodesRule.isRingNode)
                    {
                        nei.RingNeighborRule.isNeighbor = true;
                        nei.RingNeighborRule.NeighboringRingNodes.Add(ringNode.Node);
                        if (!RingNodeNeighborsIDS.Contains(nei.ID))
                        {
                            RingNodeNeighborsIDS.Add(nei.ID);
                        }
                    }

                }
            }

            foreach (int id in RingNodeNeighborsIDS)
            {
                PublicParamerters.MainWindow.myNetWork[id].RingNeighborRule.getNearestRingNode();
            }
        }
        private static void ClearPrevData()
        {
            foreach (Sensor sen in PublicParamerters.MainWindow.myNetWork)
            {

                sen.RingNeighborRule.NeighboringRingNodes.Clear();
                sen.RingNeighborRule.isNeighbor = false;
            }
        }
        private static List<Sensor> NeiNodesHolder = new List<Sensor>();

        private static void UpdateInformation()
        {
            ClearPrevData();
            foreach (RingNodes rn in PublicParamerters.RingNodes)
            {

                foreach (Sensor ringNeiEntry in rn.Node.NeighborsTable)
                {

                    if (!(ringNeiEntry.RingNodesRule.isRingNode))
                    {
                        ringNeiEntry.RingNeighborRule.NeighboringRingNodes.Add(rn.Node);
                        if (!(NeiNodesHolder.Contains(ringNeiEntry)))
                        {
                            NeiNodesHolder.Add(ringNeiEntry);
                        }
                    }
                }

            }
            GetNearestRingNodeFromMe();
        }
        private static void GetNearestRingNodeFromMe()
        {

            foreach (Sensor sen in NeiNodesHolder)
            {
                sen.RingNeighborRule.getNearestRingNode();
                sen.RingNeighborRule.isNeighbor = true;
                foreach (Sensor neiEntry in sen.NeighborsTable)
                {
                    Sensor node = neiEntry;
                    if (!(node.RingNodesRule.isRingNode))
                    {
                        if (!(node.RingNeighborRule.NeighboringRingNodes.Count > 0))
                        {
                            // node.RingNeighborRule.NeighboringRingNodes.Add(sen.RingNeighborRule.NeighboringRingNodes);
                            node.RingNeighborRule.isNeighbor = true;
                            node.RingNeighborRule.NearestRingNode = sen.RingNeighborRule.NearestRingNode;
                            node.RingNeighborRule.NeighboringRingNodes.Add(node.RingNeighborRule.NearestRingNode);
                        }
                    }
                    foreach (Sensor neiNeiEntry in node.NeighborsTable)
                    {
                        Sensor node2 = neiEntry;
                        if (!(node2.RingNodesRule.isRingNode))
                        {
                            if (!(node2.RingNeighborRule.NeighboringRingNodes.Count > 0))
                            {
                                // node.RingNeighborRule.NeighboringRingNodes.Add(sen.RingNeighborRule.NeighboringRingNodes);
                                node2.RingNeighborRule.isNeighbor = true;
                                node2.RingNeighborRule.NearestRingNode = node.RingNeighborRule.NearestRingNode;
                                node2.RingNeighborRule.NeighboringRingNodes.Add(node2.RingNeighborRule.NearestRingNode);
                            }
                        }
                    }

                }
            }


        }
        private static void AddRemoveArrows(RingNodes oldNode, RingNodes newNode)
        {
            Sensor newSen = newNode.Node;
            Sensor oldSen = oldNode.Node;
            Sensor clockSen = oldNode.ClockWiseNeighbor;
            Sensor antiClockSen = oldNode.AntiClockWiseNeighbor;
            Ring.removeOldLine(clockSen, oldSen);
            Ring.removeOldLine(oldSen, antiClockSen);

            Ring.addNewLine(newSen, antiClockSen);
            Ring.addNewLine(clockSen, newSen);
        }
        #endregion
        private static void RemoveArrow(RingNodes oldNode)
        {
            Sensor oldSen = oldNode.Node;
            Sensor clockSen = oldNode.ClockWiseNeighbor;
            Sensor antiClockSen = oldNode.AntiClockWiseNeighbor;
            Ring.removeOldLine(clockSen, oldSen);
            Ring.removeOldLine(oldSen, antiClockSen);
        }
        private static void AddArrows(Sensor newSen, Sensor clockSen)
        {
            Ring.addNewLine(clockSen, newSen);
        }
        private void ClearOldRingNodeData(RingNodes node)
        {
            Sensor sen = node.Node;
            sen.RingNodesRule = new RingNodes();
            sen.NotHighTierNode();
            RemoveArrow(node);

        }


        private Sensor FindNewRingNodeReplacement(RingNodes RN)
        {

            Sensor cand = null;
            /*   if (!RN.Node.isExpanding)
               {
                   ShouldNodeExpand(RN.Node);
               }*/
            GetTheSetofClosestNodes(RN);
            for (int i = 0; i < 4; i++)
            {
                if (!PublicParamerters.IsExanding)
                {
                    i = 1;
                    cand = GetNewRingNode(RN, i);
                    if (cand != null)
                    {
                        return cand;
                    }
                    else
                    {
                        i++;
                        cand = GetNewRingNode(RN, i);
                        return cand;
                    }
                }
                else
                {
                    cand = GetNewRingNode(RN, i);
                    if (cand != null)
                    {
                        return cand;
                    }
                }


            }

            return cand;
        }


        private void GetTheSetofClosestNodes(RingNodes RN)
        {
            ClosestNodestoRN.Clear();
            List<RingNodeCandidates> holder = new List<RingNodeCandidates>();
            Sensor anti = RN.AntiClockWiseNeighbor;
            Sensor clock = RN.ClockWiseNeighbor;
            foreach (Sensor nei in RN.Node.NeighborsTable)
            {
                Sensor node = nei;
                double distance = 0;
                if (Operations.isInMyComunicationRange(node, anti))
                {
                    distance += Operations.DistanceBetweenTwoSensors(node, clock);
                }
                else if (Operations.isInMyComunicationRange(node, clock))
                {
                    distance += Operations.DistanceBetweenTwoSensors(node, anti);
                }
                else
                {
                    distance = Operations.DistanceBetweenTwoSensors(nei, RN.AntiClockWiseNeighbor);
                    distance += Operations.DistanceBetweenTwoSensors(nei, RN.ClockWiseNeighbor);
                }
                RingNodeCandidates cand = new RingNodeCandidates(nei, distance);
                holder.Add(cand);
            }
            ClosestNodestoRN = holder.OrderBy(i => i.Distance).ToList();
            //Console.WriteLine("ASd");
        }



        private Sensor GetNewRingNode(RingNodes RN, int i)
        {
            Sensor clockWiseNei = RN.ClockWiseNeighbor;
            Sensor AntiClockWiseNei = RN.AntiClockWiseNeighbor;
            Sensor candi = null;
            double oldDistance = Operations.DistanceBetweenTwoPoints(RN.Node.CenterLocation, PublicParamerters.networkCenter);
            foreach (RingNodeCandidates nei in ClosestNodestoRN)
            {
                double candiDistance = Operations.DistanceBetweenTwoPoints(nei.Node.CenterLocation, PublicParamerters.networkCenter);
                bool conditionOne = (Operations.Orientation(AntiClockWiseNei.CenterLocation, nei.Node.CenterLocation, clockWiseNei.CenterLocation) > 1);
                //int  orientMe = (Operations.Orientation(RN.AntiClockWiseNeighbor.CenterLocation, RN.Node.CenterLocation, RN.ClockWiseNeighbor.CenterLocation));
                int orientCandi = (Operations.Orientation(AntiClockWiseNei.CenterLocation, nei.Node.CenterLocation, clockWiseNei.CenterLocation));
                bool conditionTwo;//= false;
                bool notRingNode = (!nei.Node.RingNodesRule.isRingNode);
                bool notAgentNode = (!nei.Node.isSinkAgent);
                if (i == 0)
                {
                    conditionTwo = !(nei.Node.isInsideRing);
                }
                else if (i == 1)
                {
                    //  conditionOne = true;
                    conditionTwo = nei.Node.isInsideRing;
                }
                else
                {
                    if (PublicParamerters.IsExanding)
                    {
                        conditionTwo = !(nei.Node.isInsideRing);
                    }
                    else
                    {
                        conditionTwo = nei.Node.isInsideRing;
                        conditionOne = true;
                    }

                }

                if (conditionOne && conditionTwo && notRingNode && notAgentNode)
                {
                    candi = nei.Node;
                    return candi;
                }
            }


            return candi;
        }



        public List<Sensor> myNewRingNodes = new List<Sensor>();
        private void ReplaceTwoNodes(RingNodes currentRN, List<Sensor> b)
        {
            //  currentRN.AntiClockWiseNeighbor.RingNodesRule.ClockWiseNeighbor = b.Node;
            //   currentRN.ClockWiseNeighbor.RingNodesRule.AntiClockWiseNeighbor = b.Node;
            try
            {
                RingNodes bring = PublicParamerters.RingNodes.Where(i => i.Node.ID == currentRN.Node.ID).First();
                int index = PublicParamerters.RingNodes.IndexOf(bring);
                PublicParamerters.RingNodes.RemoveAt(index);
                if (index != -1)
                {
                    foreach (Sensor rn in b)
                    {
                        PublicParamerters.RingNodes.Insert(index, rn.RingNodesRule);
                        //rn.Node.RingNodesRule.AnchorNode = currentRN.AnchorNode;
                        //PublicParameters.RingNodes[index] = rn;
                        index++;
                    }

                }

            }
            catch
            {
                Console.WriteLine("Replace Two Nodes returned exception");
            }


        }

        //Neeed to add the arrows, anchor node, fix the global RIngNodes list


        private void AddExtraInfo(List<Sensor> antiClockSet)
        {
            int startPoint = 0;
            if (antiClockSet.Count > 3)
            {
                startPoint++;
            }
            int b = startPoint;
            for (int i = (startPoint + 1); i < antiClockSet.Count; i++)
            {
                Sensor one = antiClockSet[b];
                Sensor two = antiClockSet[i];
                if (one.RingNodesRule.isRingNode)
                {
                    one.RingNodesRule.AntiClockWiseNeighbor = two;
                }
                else if (!(one.RingNodesRule.isRingNode))
                {
                    one.RingNodesRule = new RingNodes(one, two, null);
                }
                if (two.RingNodesRule.isRingNode)
                {
                    two.RingNodesRule.ClockWiseNeighbor = one;
                }
                else if (!(two.RingNodesRule.isRingNode))
                {
                    two.RingNodesRule = new RingNodes(two, null, one);
                }
                AddArrows(two, one);
                b++;
                // Console.WriteLine("***");
                //Console.WriteLine("From {0} to {1}", one.ID, two.ID);
            }

        }
        private void AggregateNewInfo()
        {
            List<Sensor> GoingAnti = new List<Sensor>();
            List<Sensor> GoingClock = new List<Sensor>();
            do
            {
                GoingAnti.Add(Neighbors.Pop());
            } while (Neighbors.Count > 0);


            for (int i = (GoingAnti.Count - 1); i >= 0; i--)
            {
                GoingClock.Add(GoingAnti[i]);
            }

            AddExtraInfo(GoingAnti);


        }
        private static int MainCounter { get; set; }
        public void ChangeRingNode(RingNodes CurrentRN)
        {
            MainCounter++;
            //First we find a new node (Expand or Collapse we start with expand)
            //Change this node with the other and change the clockwise and anticlockwise
            //Determine poisition of nodes
            //Sensor oldRingNode = CurrentRN.Node;
            myNewRingNodes.Clear();
            Sensor newRingNode = FindNewRingNodeReplacement(CurrentRN);
            Sensor clock = CurrentRN.ClockWiseNeighbor;
            Sensor anti = CurrentRN.AntiClockWiseNeighbor;

            if (newRingNode != null)
            {
                if (PublicParamerters.IsExanding)
                {
                    double distance = Operations.DistanceBetweenTwoPoints(newRingNode.CenterLocation, PublicParamerters.networkCenter);
                    if (distance >= (PublicParamerters.clusterRadius * 1.6))
                    {
                        PublicParamerters.IsExanding = false;
                    }
                }
                else
                {
                    double distance = Operations.DistanceBetweenTwoPoints(newRingNode.CenterLocation, PublicParamerters.networkCenter);
                    if (distance <= (PublicParamerters.clusterRadius))
                    {
                        PublicParamerters.IsExanding = true;
                    }
                }
                //me,anti,clock
                //Now i have myNewRingNodes loop through there and fix everything 
                RingNodes newNo = new RingNodes(newRingNode, CurrentRN.AntiClockWiseNeighbor, CurrentRN.ClockWiseNeighbor);
                myNewRingNodes.Add(newRingNode);
                CheckNewRingNode(newNo);
                ReplaceTwoNodes(CurrentRN, myNewRingNodes);
                foreach (Sensor newRN in myNewRingNodes)
                {
                    if (newRN.RingNodesRule.AntiClockWiseNeighbor.ID == newRN.RingNodesRule.ClockWiseNeighbor.ID)
                    {
                        Console.WriteLine("Error in Change RingNode");
                    }

                    newRN.RingNodesRule.AnchorNodes = CurrentRN.AnchorNodes;
                    newRingNode.IsHightierNode = true;
                    newRingNode.SetSinksAgentsForNewHighTierNode(CurrentRN.Node.GetAnchorNodesFromHighTierNodes);

                }
                // RingNodes newNo = new RingNodes(newRingNode, CurrentRN.AntiClockWiseNeighbor, CurrentRN.ClockWiseNeighbor);
                // newNo.AnchorNode = CurrentRN.AnchorNode;
                // newRingNode.RingNodesRule = newNo;
                //   ReplaceTwoNodes(CurrentRN, newNo);
                if (!myNewRingNodes.Contains(CurrentRN.Node))
                {
                    ClearOldRingNodeData(CurrentRN);
                }

                //AddRemoveArrows(CurrentRN, newNo);
                RingNodesFunctions r = new RingNodesFunctions();
                //markNeighboringRingNods();
                UpdateInformation();
                r.DeterminePositionofAllNodes();
                // Console.WriteLine("Change {0} to {1}",CurrentRN.Node.ID,newRingNode.ID);

            }
            else
            {
                Console.WriteLine("Failed to change node {0}", CurrentRN.Node.ID);
                //Failed
            }

        }
        private int totalNumberOfNewNodes { get; set; }
        private void CheckNewRingNode(RingNodes newNode)
        {
            // AntiNeighbors.Clear();
            Neighbors.Clear();
            Candidates.Clear();

            Sensor me = newNode.Node;
            Sensor clock = newNode.ClockWiseNeighbor;
            Sensor antiClock = newNode.AntiClockWiseNeighbor;

            Neighbors.Push(antiClock);
            /*if (!Operations.isInMyComunicationRange(me, antiClock))
            {
                if (findPottentialNeighbors(me, antiClock))
                {
                    totalNumberOfNewNodes = Candidates.Count;
                    //Neighbors.Push(antiClock);
                    foreach (Sensor sen in Candidates)
                    {
                        Neighbors.Push(sen);
                        myNewRingNodes.Add(sen);
                    }
                    Candidates.Clear();
                }  
            }*/
            Neighbors.Push(me);
            /* if (!Operations.isInMyComunicationRange(me, clock))
             {
                 if (findPottentialNeighbors(me,clock))
                 {
                     totalNumberOfNewNodes += Candidates.Count;
                     foreach (Sensor sen in Candidates)
                     {
                         Neighbors.Push(sen);
                         myNewRingNodes.Add(sen);
                     }
                    // Neighbors.Push(clock);
                     Candidates.Clear();
                 }
             }*/
            Neighbors.Push(clock);
            AggregateNewInfo();

        }

        //  private static Stack<Sensor> AntiNeighbors = new Stack<Sensor>();
        private Stack<Sensor> Neighbors = new Stack<Sensor>();

        private bool findPottentialNeighbors(Sensor one, Sensor two)
        {
            Candidates.Clear();
            // They will have common neighbors
            List<Sensor> potentialNeighbors = new List<Sensor>();
            List<Sensor> commonNeighbors = new List<Sensor>();
            bool found = false;
            foreach (Sensor entryS1 in one.NeighborsTable)
            {
                if (!(entryS1.RingNodesRule.isRingNode))
                {
                    foreach (Sensor entryS2 in two.NeighborsTable)
                    {
                        if (entryS1.ID == entryS2.ID)
                        {
                            commonNeighbors.Add(entryS1);
                            found = true;
                        }
                    }
                }
            }

            if (found)
            {
                Candidates = findCommonNeighbor(one, two, commonNeighbors);
                foreach (Sensor sen in Candidates)
                {
                    if (myNewRingNodes.Contains(sen))
                    {
                        return false;
                    }
                }
                if (Candidates.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                /* foreach (NeighborsTableEntry entryS1 in one.NeighborsTable)
                 {
                     if (!(entryS1.NeiNode.RingNodesRule.isRingNode))
                     {
                         foreach (NeighborsTableEntry entryS1Nei in two.NeighborsTable)
                         {
                             if (two.NeighborsTable.Contains(entryS1Nei))
                             {
                                 commonNeighbors.Add(entryS1Nei.NeiNode);
                                 found = true;
                             }
                         }
                     }
                 }
                 if (found)
                 {
                     Candidates = findCommonNeighbor(one, two, commonNeighbors);
                     return true;
                 }
                 else
                 {*/
                return false;

            }
        }


        private List<Sensor> findCommonNeighbor(Sensor one, Sensor two, List<Sensor> Common)
        {
            List<Sensor> se = new List<Sensor>();
            double max = 100;
            Sensor holder = null;
            foreach (Sensor candi in Common)
            {
                if (!candi.RingNodesRule.isRingNode)
                {
                    double pirDist = Operations.GetPerpindicularDistance(one.CenterLocation, two.CenterLocation, candi.CenterLocation);
                    if (pirDist < max)
                    {
                        max = pirDist;
                        holder = candi;
                    }
                }

            }
            //  se.Add(one);        
            se.Add(holder);
            return se;
        }
    }
}
