using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreightOptimizer
{
    class Population
    {
        List<Pallet> palletList = new List<Pallet>();
        double sumTrucks = 0;
        double totalOverallCost = 0.0; //summation of the entire costs of this potential population (truck -> CF flat rate and CF -> EPZip per Pallet)

        internal List<Pallet> PalletList
        {
            get { return palletList; }
            set { palletList = value; }
        }

        public double SumTrucks
        {
            get { return sumTrucks; }
            set { sumTrucks = value; }
        }

        public double TotalOverallCost
        {
            get { return totalOverallCost; }
            set { totalOverallCost = value; }
        }
    }
}