using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Intilization;
using RR.Comuting.computing;
using RR.Comuting.Routing;
using System;
using System.Collections.Generic;

namespace MP.MergedPath.Routing
{
    public class RecoveryRow
    {
        public Sensor PrevAgent { get; set; }
        public AgentsRow RecoveryAgentList;
        public double ObtiantedTime { get; set; } // the time that the recovery row is obtianted. 

        /// <summary>
        /// if the recored is obtainted within 15s.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (PublicParamerters.SimulationTime - ObtiantedTime >= 10)
                    return true;
                else return false;
            }
        }

        public int ObtainPacketsRetry { get; set; }



    }

    /// <summary>
    /// recovery algorithm:
    /// the previose agent sends obtian new sink position and wait for 10 sinks. 
    /// if the new position is arrived to old gent then, the old agent will send the packet to new agent.
    /// if 10s are past, the packet should be droped.
    /// </summary>
    class RecoveryMessage
    {
       
        
        /// <summary>
        /// preAgent: the packet will arrived to this  agent. This agent did not know the location of the new agent for the SinksIDsRequiredRecovery.
        /// </summary>
        /// <param name="preAgent"></param>
        /// <param name="SinksIDsRequiredRecovery"></param>
        public RecoveryMessage(Sensor preAgent, Packet packet)
        {
            // preAgent: if it has no record, then we obtian the sink position first.
            // if the preAgent did not sent an obtian message to the hightier node
            if (preAgent.RecoveryRow == null)
            {
                // obtian the recovery. obtian the sink location.
                //new ObtainSinkFreshPositionMessage(preAgent, packet.SinkIDsNeedsRecovery);  //  
            }
            else
            {
                // it has record. no need to resend an obtian: but you should wait.
                if (!preAgent.RecoveryRow.IsExpired)
                {
                    // not expired
                    List<Sensor> NewAents = new List<Sensor>(); // new agent for the recovery.
                    //foreach (SinksAgentsRow row in preAgent.RecoveryRow.RecoveryAgentList) // get the agents
                    //{
                        //if (preAgent.RecoveryRow.RecoveryAgentList.AgentNode.ID != preAgent.ID) // no the same agent. the new one should had diffrent ID
                        //{
                        //    bool isFound = Operations.FindInAlistbool(preAgent.RecoveryRow.RecoveryAgentList.AgentNode, NewAents); // no repeatation
                        //    if (!isFound)
                        //    {
                        //        packet.Destination = preAgent.RecoveryRow.RecoveryAgentList.AgentNode; // update the packet destination.
                        //        NewAents.Add(preAgent.RecoveryRow.RecoveryAgentList.AgentNode);
                        //    }
                        //}
                    //}

                    if (NewAents.Count > 0)
                    {

                        //Console.WriteLine("RecoveryMessage. Source ID=" + packet.Source.ID + " PID=" + packet.PID +" Path "+ packet.Path);
                        packet.SinksAgentsList = preAgent.RecoveryRow.RecoveryAgentList;
                        if (packet.Hops <= packet.TimeToLive)
                        {
                            new DataPacketMessages(preAgent, packet);
                        }
                        else
                        {
                            NetworkOverheadCounter counter=new NetworkOverheadCounter();
                            counter.DropPacket(packet, preAgent, PacketDropedReasons.Loop);
                        }
                    }
                    else
                    {
                        if (preAgent.RecoveryRow.ObtainPacketsRetry >= 3)
                        {
                            // in case no new agent is found.
                            //Console.WriteLine("RecoveryMessage. No agent is found during the recovery.Source ID = " + packet.Source.ID + " PID = " + packet.PID + " Path " + packet.Path);
                            NetworkOverheadCounter counter = new NetworkOverheadCounter();
                            counter.DropPacket(packet, preAgent, PacketDropedReasons.RecoveryNoNewAgentFound);
                        }
                        else
                        {
                            //Console.WriteLine("RecoveryMessage. Recovery period is expired. Old Agent is sending new obtain packet!" + " Path " + packet.Path);
                            //new ObtainSinkFreshPositionMessage(preAgent, packet.SinkIDsNeedsRecovery); // obtain
                        }
                    }

                }
                else
                {

                    // resent the obtian packet. 2 times.
                    if (preAgent.RecoveryRow.ObtainPacketsRetry <= 3)
                    {
                        //Console.WriteLine("RecoveryMessage. Recovery period is expired. Old Agent is sending new obtain packet!" + " Path " + packet.Path);
                        //new ObtainSinkFreshPositionMessage(preAgent, packet.SinkIDsNeedsRecovery); // obtain
                    }
                    else
                    {
                        //Console.WriteLine("RecoveryMessage. Recovery period is expired. we tryied to re-sent the obtian packet for three times and faild. The packet will be droped." + " Path " + packet.Path);
                        // drop the packet:
                        NetworkOverheadCounter counter = new NetworkOverheadCounter();
                        counter.DropPacket(packet, preAgent, PacketDropedReasons.RecoveryPeriodExpired);
                        preAgent.RecoveryRow = null;
                    }

                }
            }
        }
    }
}
