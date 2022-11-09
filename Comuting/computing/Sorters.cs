using RR.Dataplane.PacketRouter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.Comuting.computing
{
    class Sorters
    {
        public class CoordinationEntrySorter : IComparer<CoordinationEntry>
        {
            public int Compare(CoordinationEntry y, CoordinationEntry x)
            {
                return y.Priority.CompareTo(x.Priority);
            }
        }
    }
}
