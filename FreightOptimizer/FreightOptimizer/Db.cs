using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using System.Configuration;

namespace FreightOptimizer
{
    class Db
    {
        private static OleDbDataReader dbReader;
        
        public static List<Pallet> getSource()
        { 
            List<Pallet> source = new List<Pallet>();
            String qry = "";
            //OleDbDataReader reader = null;
            
            DateTime from = Convert.ToDateTime(ConfigurationSettings.AppSettings["startDate"]);
            DateTime thru = Convert.ToDateTime(ConfigurationSettings.AppSettings["endDate"]);
            String FType = ConfigurationSettings.AppSettings["Type"];
            int unique = 0; //testing
            Console.WriteLine("Connecting to Database");
            Program.CONN.Open();
            Console.WriteLine("Connection Open, retrieving Pallets");

            for (DateTime current = from; current.Date <= thru; current = current.Date.AddDays(1))
            {
                qry = "SELECT distinct PalletId, Height, GrossWeight, CF, ShippingData.EPZip FROM Postal.dbo.ShippingData JOIN Postal.dbo.PriceDetail ON PriceDetail.EPZIP = ShippingData.EPZIP Where PriceDetail.Carrier = ShippingData.Carrier AND  ShippingData.MinDropDate <= '" + current + "' AND  '" + current + "' <= ShippingData.MaxDropDate AND ShippingData.FacilityType = '" + FType + "' AND ShippingData.FacilityType = PriceDetail.FacilityType Order By PalletId, CF";
                OleDbCommand select = new OleDbCommand(qry, Program.CONN);
                dbReader = select.ExecuteReader();
                while (dbReader.Read())//get ALL pallets for the day
                {
                    Console.Write("\r{0}    ", source.Count());
                    if (!source.Exists(item => item.PalletId == dbReader.GetValue(0).ToString()))//if the palletId doesn't exist in the source list, add it
                    {
                        Pallet p = new Pallet(dbReader.GetValue(0).ToString(), Convert.ToDouble(dbReader.GetValue(1)), Convert.ToDouble(dbReader.GetValue(2)), current, dbReader.GetValue(3).ToString(), dbReader.GetValue(4).ToString());

                        source.Add(p);
                        unique++;
                    }
                    else //if it does exist, find it, update potential ShipDates and CFs
                    {
                        foreach (Pallet p in source)
                        {
                            if (p.PalletId == dbReader.GetValue(0).ToString())
                            {
                                if (!p.PotentialCF.Exists(item => item == dbReader.GetValue(3).ToString()))
                                {
                                    String cf = dbReader.GetValue(3).ToString();
                                    p.PotentialCF.Add(cf);
                                }

                                if (!p.PotentialShipDate.Exists(item => item == current))
                                {
                                    p.PotentialShipDate.Add(current);
                                }
                            }
                        }
                    }
                }
            }
            Program.CONN.Close();
            Console.WriteLine("Done!\nConnection Closed");

            return source;
        }
    }
}