using RR.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.Computations
{
    class RandomvariableStream
    {
        public static class Basics
        {
            private static uint m_w = 521288629;
            private static uint m_z = 362436069;
            public static double  RandU01()
            {
                // 0 <= u < 2^32
                uint u = GetUint();
                // The magic number below is 1/(2^32 + 2).
                // The result is strictly between 0 and 1.
                return (u + 1.0) * 2.328306435454494e-10;
            }

            private static uint GetUint()
            {

                m_z = 36969 * (m_z & 65535) + (m_z >> 16);
                m_w = 18000 * (m_w & 65535) + (m_w >> 16);
                return (m_z << 16) + m_w;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public static class UniformRandomVariable
        {
            public static double GetDoubleValue(double min, double max)
            {
                double v = min + RandomeNumberGenerator.GetUniform() * (max - min);
                bool IsAntithetic = true; // check this.
                if (IsAntithetic)
                {
                    return min + (max - v);
                }
                else
                {
                    return v;
                }
            }

            public static int GetIntValue(int min, int max)
            {
                return Convert.ToInt32(GetDoubleValue(min, max));
            }
        }

        /// <summary>
        /// NormalRandomVariable:https://www.nsnam.org/doxygen/random-variable-stream_8cc_source.html
        /// </summary>
        public static class NormalRandomVariable
        {
            public static double GetValue(double m_mean, double m_variance)
            {
                return RandomeNumberGenerator.GetNormal(m_mean, m_variance);
            }
        }





    }
}
