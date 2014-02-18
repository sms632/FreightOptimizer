using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreightOptimizer
{
    class Calc
    {
        public static int getRandom(int max)
        {
            double p = 0.25;
            int selected = 0;
            selected = (int)(Math.Log(1 - Program.rand.NextDouble()) / Math.Log(p));
            while (selected >= max)
            {
                selected = (int)(Math.Log(1 - Program.rand.NextDouble()) / Math.Log(p));
            }
            return selected;
        }

        
    }
}
