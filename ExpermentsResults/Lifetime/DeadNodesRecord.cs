using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.ExpermentsResults.Lifetime 
{
    public class DeadNodesRecord
    {

        public int DeadOrder { get; set; } // ترتيب الوفاه في الشبككه . ماتت رقم كم؟

        public int DeadNodeID { get; set; }
        public long DeadAfterPackets { get; set; } // deade after sending xx packets. the whole number of packets.

        public double DeadAtSecond { get; set; }
    }
}
