using System;
using System.Collections.Generic;
using System.Linq;

namespace WeatherSweden.Core
{
    public static class SeasonLogic
    {
        // ALGORITMVAL: Identifiering av meteorologisk årstid
        // Problem: SMHI definierar årstider baserat påsekvenser av dagar (t.ex. 5 dagar i rad).
        // Lösning: Här kan jag inte använda enkel LINQ (som Where/Average) eftersom man behöver behålla
        // "state" (hur många dagar i rad man sett hittills).
        // Därför är en foreach-loop med en räknare (consecutiveDays) den mest lämpliga algoritmen.
        public static DateTime? FindStartOfSeason(List<WeatherData> data, double limitTemp, bool isBelow)
        {
            // Först aggregerar jag data till dygnsmedelvärden och sorterar kronologiskt.
            // Datastruktur: Jag använder en anonym typ i en List för att slippa skapa en ny klass enbart för detta.
            var dailyTemps = data
                .GroupBy(x => x.Date.Date)
                .Select(g => new { Date = g.Key, AvgTemp = g.Average(x => x.Temp) })
                .OrderBy(x => x.Date)
                .ToList();

            int consecutiveDays = 0;

            foreach (var day in dailyTemps)
            {
                // Kollar om tempen är under (för vinter/höst) eller över gränsvärdet
                bool match = isBelow ? (day.AvgTemp < limitTemp) : (day.AvgTemp > limitTemp);

                if (match)
                    consecutiveDays++;
                else
                    consecutiveDays = 0;

                // Om 5 dagar i rad uppfyller kravet
                if (consecutiveDays == 5)
                {
                    // Enligt SMHI är startdatumet den FÖRSTA dagen i sviten, så vi backar 4 dagar.
                    return day.Date.AddDays(-4);
                }
            }
            return null; // Ingen säsong hittades
        }
    }
}