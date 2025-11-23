using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WeatherSweden.Core;

namespace WeatherSweden.DataAccess
{
    public static class CsvLoader
    {
        public static void LoadData(WeatherContext db, string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Could not find file: " + Path.GetFullPath(path));
                return;
            }

            var lines = File.ReadAllLines(path);
            var list = new List<WeatherData>();

            // Loopar igenom filen, startar på index 1 för att hoppa över rubriken
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length >= 4)
                {
                    try
                    {
                        var wd = new WeatherData
                        {
                            Date = DateTime.Parse(cols[0]),
                            Location = cols[1],
                            // InvariantCulture krävs för att tolka punkten i "10.5" som decimaltecken
                            Temp = double.Parse(cols[2], CultureInfo.InvariantCulture),
                            Humidity = int.Parse(cols[3])
                        };
                        list.Add(wd);
                    }
                    catch
                    {
                        // Ignorerar rader som är trasiga
                    }
                }
            }

            // Sparar allt till databasen i ett svep (snabbare än att ta en och en)
            db.WeatherDatas.AddRange(list);
            db.SaveChanges();
            Console.WriteLine($"Successfully imported {list.Count} rows.");
        }
    }
}