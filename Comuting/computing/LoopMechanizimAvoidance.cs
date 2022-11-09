using RR.Dataplane.NOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.Comuting.computing
{
    class LoopMechanizimAvoidance
    {

        /// <summary>
        /// return true if loop is discovred.
        /// </summary>
        public bool isLoop(Packet _pck)
        {
            //25>86>72>15"
            string[] spliter = _pck.Path.Split('>');
            if (spliter.Length >= 4)
            {
                string last1 = spliter[spliter.Length - 1];
                string last2 = spliter[spliter.Length - 2];
                string last3 = spliter[spliter.Length - 3];
                string last4 = spliter[spliter.Length - 4];

                if (last1 == last3 && last4 == last2) return true;
            }
            return false;
        }


    }
}
