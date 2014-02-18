using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using System.Configuration;

namespace FreightOptimizer
{
    class Program
    {
        public static Random rand = new Random();
        public static OleDbConnection CONN = new OleDbConnection(ConfigurationSettings.AppSettings["Conn"]);
        public static List<Pallet> SOURCE = new List<Pallet>();
        
        public static List<Population> population = new List<Population>();
        public static Dictionary<string, Dictionary<string, double>> costCFtoEPzip = new Dictionary<string, Dictionary<string, double>>();
        public static Dictionary<String, double> costTruckToCF = new Dictionary<string, double>();
        public static Dictionary<String, double> minWeightToEPZip = new Dictionary<string, double>();

        //---------------------------------------------------------------------------------------------------//
        public static void getCFtoEPZipCostConstants()
        {
            string qry = "SELECT DISTINCT [CF],[EPZIP],[CostperCWT],[MinWeight] FROM [Postal].[dbo].[PriceDetail] ORDER BY CF";
            OleDbCommand select = new OleDbCommand(qry, CONN);
            CONN.Open();
            OleDbDataReader reader = select.ExecuteReader();
            while (reader.Read())
            {
                if (!costCFtoEPzip.ContainsKey(reader.GetString(0)))
                {
                    costCFtoEPzip.Add(reader.GetString(0), new Dictionary<string, double> { { reader.GetString(1), Convert.ToDouble(reader.GetValue(2)) } });
                }
                else
                {
                    if (!costCFtoEPzip[reader.GetString(0)].ContainsKey(reader.GetString(1)))
                    {
                        costCFtoEPzip[reader.GetString(0)].Add(reader.GetString(1), Convert.ToDouble(reader.GetValue(2)));
                    }
                }
            }
            CONN.Close();
        }

        public static void getTruckToCFCostConstants()
        {
            string qry = "SELECT [CF],[TruckRate] FROM [Postal].[dbo].[PriceDetIB]";
            OleDbCommand select = new OleDbCommand(qry, CONN);
            CONN.Open();
            OleDbDataReader reader = select.ExecuteReader();
            while (reader.Read())
            {
                if (!costTruckToCF.ContainsKey(reader.GetString(0)))
                {
                    costTruckToCF.Add(reader.GetString(0), Convert.ToDouble(reader.GetValue(1)));
                }
            }
            CONN.Close();
        }

        public static List<List<Pallet>> selectBestPopulation()
        {
            Population bestPop = new Population();
            Population temp = population[0];
            Pallet[] tempList = temp.PalletList.ToArray();
            bestPop.SumTrucks = temp.SumTrucks;
            bestPop.TotalOverallCost = temp.TotalOverallCost;
            bestPop.PalletList = tempList.ToList();
            population.Clear();
            temp = null;
            population = null;

            return prepForLoad(bestPop.PalletList);
        }

        public static List<List<Pallet>> prepForLoad(List<Pallet> bestPop)
        {
            List<List<Pallet>> readyToLoad = bestPop
                .GroupBy(p => new { p.AssignedCF, p.ShipDate })
                .Select(g => g.ToList())
                .ToList();

            return readyToLoad;
        }

        public static void printChosen(Population pop)
        {
            DateTime current = DateTime.Now;
            String filePath = "C:\\Users\\Sean\\Desktop\\Output\\FO_v8_output" + current.ToString() + ".csv";
            System.IO.StreamWriter writer = new System.IO.StreamWriter("C:\\Users\\Sean\\Desktop\\Output\\FO_v8_output" + current.ToString() + ".csv", true);

            writer.WriteLine("PalletID,ShipDate,CF,Height,Weight");
            foreach (Pallet p in pop.PalletList)
            {
                writer.WriteLine(p.PalletId + "," + p.ShipDate.Day + "," + p.AssignedCF + "," + p.Height + "," + p.Weight);
            }
            writer.WriteLine("Total Cost: " + pop.TotalOverallCost);
            writer.WriteLine("Num trucks: " + pop.SumTrucks);
            writer.Close();
            Console.WriteLine("Done!");
        }

        public static Population sortPopulation()
        {
            population = population.OrderBy(x => x.TotalOverallCost).ToList();
            population[0].PalletList.OrderBy(x => x.PotentialCF).ThenBy(x => x.ShipDate);

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Total Cost: " + population[i].TotalOverallCost);
                Console.WriteLine("Num trucks: " + population[i].SumTrucks);
            }
            Console.ReadLine();

            return population[0];
        }

        public static double calcCFtoEPZipCost(Population pop)
        {
            double CFtoEPZipcost = 0.0;
            double EPZipCWT = 0.0;


            foreach (Pallet p in pop.PalletList)
            {
                var CFtoEPZipDic = costCFtoEPzip[p.AssignedCF];
                EPZipCWT = CFtoEPZipDic[p.EPZIP];
                CFtoEPZipcost += (p.Weight / 100.0) * EPZipCWT;
            }

            return CFtoEPZipcost;
        }

        public static Population calcPopulationStats(Population pop)
        {
            //for each CF per day we really only need the number of trucks going to that site and then the sum of the trucks for the day
            //we'll use a dictionary to store the name of the cf and the number of trucks to it...
            //now to calculate this... we'll use a separate dictionary to iterate through the list of pallets for the day and store the Sum(weight)
            double sumWeight = 0.0;
            double superSum = 0.0;
            double countTrucks = 0;
            double trucksByCF = 0;
            //double truckToCFcost = 0.0;
            //double palletToEPZipcost = 0.0;
            double truckRate = 0.0;

            pop.PalletList.OrderBy(x => x.ShipDate).ThenBy(x => x.AssignedCF);
            var q = pop.PalletList
              .ToLookup(i => i.ShipDate) // first key
              .ToDictionary(
                i => i.Key,
                i => i.ToLookup(
                  j => j.AssignedCF,       // second key
                  j => j.Weight));     // value


            //Console.WriteLine("Num Pallets: " + pop.PalletList.Count());
            foreach (var date in q)
            {
                //Console.WriteLine("{0}: ", date.Key);
                foreach (var cf in date.Value)
                {
                    sumWeight = 0.0;
                    //Console.WriteLine("  {0}: ", cf.Key);
                    foreach (var palletweight in cf)
                    {
                        sumWeight += palletweight;
                        superSum += palletweight;
                    }
                    trucksByCF = Math.Ceiling(sumWeight / 44000);
                    truckRate = costTruckToCF[cf.Key];
                    //Console.WriteLine("truck rate: " + truckRate);
                    //Console.WriteLine("Cost to cf: " + trucksByCF * truckRate);
                    pop.TotalOverallCost += truckRate * trucksByCF;

                    //Console.WriteLine("    Num Trucks (by weight): {0} Weight: {1}", trucksByCF, sumWeight);
                    countTrucks += trucksByCF;
                }
                //Console.ReadLine();
            }
            pop.SumTrucks = countTrucks;

            //Console.WriteLine("Sum weight: " + superSum + " Num trucks by weight: " + Math.Ceiling(superSum/44000));
            //Console.WriteLine("Sum trucks from assignments: " + countTrucks);
            //Console.WriteLine("Cost from truck rate: " + pop.TotalOverallCost);
            pop.TotalOverallCost += calcCFtoEPZipCost(pop);
            //Console.WriteLine("Cost of trucks + CF->EpZip: " + pop.TotalOverallCost);

            return pop;
        }


        public static int getRandom(int max)
        {
            double p = 0.25;
            int selected = 0;
            selected = (int)(Math.Log(1 - rand.NextDouble()) / Math.Log(p));
            while (selected >= max)
            {
                selected = (int)(Math.Log(1 - rand.NextDouble()) / Math.Log(p));
            }
            return selected;
        }

        public static void setPopulation()
        {
            int randDate = 0;
            int randCF = 0;
            int numPopulations = 2000;
            Console.WriteLine("Randomizing {0} potential populations.", numPopulations);
            for (int i = 0; i < numPopulations; i++)
            {
                Population newPop = new Population();
                foreach (Pallet p in SOURCE)
                {
                    Pallet copy = new Pallet(p.PalletId, p.Height, p.Weight, p.PotentialShipDate, p.PotentialCF, p.EPZIP);
                    randDate = getRandom(p.PotentialShipDate.Count());
                    randCF = rand.Next(p.PotentialCF.Count());
                    copy.ShipDate = p.PotentialShipDate[randDate];
                    copy.AssignedCF = p.PotentialCF[randCF];
                    newPop.PalletList.Add(copy);
                }
                newPop.PalletList.OrderBy(x => x.ShipDate).ThenBy(x => x.AssignedCF);
                newPop.PalletList.Sort((x, y) => String.Compare(x.AssignedCF, y.AssignedCF));
                newPop.PalletList.Sort((x, y) => DateTime.Compare(x.ShipDate, y.ShipDate));
                newPop = calcPopulationStats(newPop);
                //newPop = calcPopulationCost(newPop);
                population.Add(newPop);
                Console.Write("\r{0}%     ", Math.Floor((double)i / (numPopulations - 1) * 100));
            }
        }

        //Connects to database and retrieves a list of Pallets that can ship in the specified date range with the specified Type.
        public static void getSource()
        {
            String qry = "";
            OleDbDataReader reader = null;
            DateTime from = Convert.ToDateTime(ConfigurationSettings.AppSettings["startDate"]);
            DateTime thru = Convert.ToDateTime(ConfigurationSettings.AppSettings["endDate"]);
            String FType = ConfigurationSettings.AppSettings["Type"];
            int unique = 0; //testing
            Console.WriteLine("Connecting to Database");
            CONN.Open();
            Console.WriteLine("Connection Open, retrieving Pallets");

            for (DateTime current = from; current.Date <= thru; current = current.Date.AddDays(1))
            {

                qry = "SELECT distinct PalletId, Height, GrossWeight, CF, ShippingData.EPZip FROM Postal.dbo.ShippingData JOIN Postal.dbo.PriceDetail ON PriceDetail.EPZIP = ShippingData.EPZIP Where PriceDetail.Carrier = ShippingData.Carrier AND  ShippingData.MinDropDate <= '" + current + "' AND  '" + current + "' <= ShippingData.MaxDropDate AND ShippingData.FacilityType = '" + FType + "' AND ShippingData.FacilityType = PriceDetail.FacilityType Order By PalletId, CF";
                OleDbCommand select = new OleDbCommand(qry, CONN);
                reader = select.ExecuteReader();
                while (reader.Read())//get ALL pallets for the day
                {
                    Console.Write("\r{0}    ", SOURCE.Count());
                    if (!SOURCE.Exists(item => item.PalletId == reader.GetValue(0).ToString()))//if the palletId doesn't exist in the source list, add it
                    {
                        Pallet p = new Pallet(reader.GetValue(0).ToString(), Convert.ToDouble(reader.GetValue(1)), Convert.ToDouble(reader.GetValue(2)), current, reader.GetValue(3).ToString(), reader.GetValue(4).ToString());

                        SOURCE.Add(p);
                        unique++;
                    }
                    else //if it does exist, find it, update potential ShipDates and CFs
                    {
                        foreach (Pallet p in SOURCE)
                        {
                            if (p.PalletId == reader.GetValue(0).ToString())
                            {
                                if (!p.PotentialCF.Exists(item => item == reader.GetValue(3).ToString()))
                                {
                                    String cf = reader.GetValue(3).ToString();
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
            CONN.Close();
            Console.WriteLine("Done!\nConnection Closed");
        }

        static void Main(string[] args)
        {
            getTruckToCFCostConstants();
            getCFtoEPZipCostConstants();
            SOURCE = Db.getSource();

            setPopulation();
            sortPopulation();
            Console.WriteLine(population[0].PalletList.Count());
        }
    }
}
