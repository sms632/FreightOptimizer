using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreightOptimizer
{
    public class Pallet
    {
        string palletId = "";
        int pos = 0;
        double height = 0.0;
        double weight = 0.0;
        String EPZip = "";
        List<DateTime> potentialShipDate = new List<DateTime>();
        DateTime shipDate = DateTime.MinValue;
        List<String> potentialCF = new List<String>();
        String assignedCF = "";

        public Pallet(string id, double h, double w, List<DateTime> pSD, List<String> pCF, String ep)
        {
            palletId = id;
            height = h;
            weight = w;
            EPZip = ep;

            foreach (DateTime d in pSD)
            {
                potentialShipDate.Add(d);
            }

            foreach (String s in pCF)
            {
                potentialCF.Add(s);
            }
        }

        public Pallet(string id, double h, double w, DateTime d, String c, String ep)
        {
            palletId = id;
            height = h;
            weight = w;
            potentialShipDate.Add(d);
            potentialCF.Add(c);
            EPZip = ep;
        }

        public Pallet(string id, double h, double w, int p)
        {
            palletId = id;
            pos = p;
            height = h;
            weight = w;
        }

        public Pallet()
        {
            // TODO: Complete member initialization
        }

        public string PalletId
        {
            get { return palletId; }
            set { palletId = value; }
        }

        public int Pos
        {
            get { return pos; }
            set { pos = value; }
        }

        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        public double Weight
        {
            get { return weight; }
            set { weight = value; }
        }

        public List<DateTime> PotentialShipDate
        {
            get { return potentialShipDate; }
            set { potentialShipDate = value; }
        }

        public List<String> PotentialCF
        {
            get { return potentialCF; }
            set { potentialCF = value; }
        }

        public DateTime ShipDate
        {
            get { return shipDate; }
            set { shipDate = value; }
        }

        public String AssignedCF
        {
            get { return assignedCF; }
            set { assignedCF = value; }
        }

        public String EPZIP
        {
            get { return EPZip; }
            set { EPZip = value; }
        }
    }
}
