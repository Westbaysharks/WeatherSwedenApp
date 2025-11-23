using System;
using System.Collections.Generic;
using System.Linq;

namespace WeatherSweden.Core
{
    // Hjälpklass för att slå ihop inne och ute på samma rad
    public class MergedData
    {
        public DateTime Date { get; set; }
        public double TempIn { get; set; }
        public double TempOut { get; set; }
    }

    public static class VgAnalysis
    {
        // DATABEHANDLING: Data-matchning
        // Eftersom databasen lagrar "Inne" och "Ute" på olika rader måste man pivotera datan.
        // Jag använder LINQ GroupBy på datumet för att slå ihop rader som hör till samma tidpunkt.
        public static List<MergedData> MergeData(List<WeatherData> allData)
        {
            return allData
                .GroupBy(x => x.Date)
                .Where(g => g.Count() >= 2) // Måste finnas data för både Inne och Ute
                .Select(g => new MergedData
                {
                    Date = g.Key,
                    // Hämtar temp för Inne, annars 0
                    TempIn = g.FirstOrDefault(x => x.Location == "Inne")?.Temp ?? 0,
                    // Hämtar temp för Ute, annars 0
                    TempOut = g.FirstOrDefault(x => x.Location == "Ute")?.Temp ?? 0
                })
                .OrderBy(x => x.Date)
                .ToList();
        }

        // ALGORITMVAL: Detektion av öppen balkongdörr
        // Jag använder en heuristisk algoritm baserad på termodynamik:
        // När dörren öppnas sker ett snabbt luftutbyte. Om det är kallare ute än inne,
        // kommer innetemperaturen sjunka snabbt, samtidigt som utegivaren (som sitter nära dörren)
        // påverkas av den utströmmande varmluften och visar en ökning.
        // Denna samtidiga förändring (Inne NER, Ute UPP) är signaturen man letar efter.
        public static Dictionary<DateTime, int> CalculateBalconyOpenTime(List<MergedData> data)
        {
            // Datastruktur: Dictionary<DateTime, int> är optimalt här för att snabbt
            // kunna slå upp och öka räknaren för ett specifikt datum utan att behöva loopa.
            var result = new Dictionary<DateTime, int>();

            for (int i = 1; i < data.Count; i++)
            {
                var prev = data[i - 1];
                var curr = data[i];

                // Antagande: Om Inne-temp sjunker OCH Ute-temp stiger -> dörren är öppen
                if (curr.TempIn < prev.TempIn && curr.TempOut > prev.TempOut)
                {
                    var day = curr.Date.Date;
                    if (!result.ContainsKey(day)) result[day] = 0;

                    result[day]++; // Plussa på 1 minut
                }
            }
            return result;
        }

        // Räknar ut temperaturskillnaden mellan inne och ute
        public static List<KeyValuePair<DateTime, double>> CalculateTempDiff(List<MergedData> data)
        {
            // Använder LINQ för att deklarativt beräkna genomsnittlig absolut skillnad per dag.
            // Math.Abs() används eftersom "stor skillnad" är oberoende av riktning (varmare/kallare).
            return data
                .GroupBy(x => x.Date.Date)
                .Select(g => new KeyValuePair<DateTime, double>(
                    g.Key,
                    // Absolutbelopp av skillnaden (spelar ingen roll vilket som är varmast)
                    g.Average(x => Math.Abs(x.TempIn - x.TempOut))
                ))
                .OrderByDescending(x => x.Value) // Sortera så störst skillnad hamnar först
                .ToList();
        }
    }
}