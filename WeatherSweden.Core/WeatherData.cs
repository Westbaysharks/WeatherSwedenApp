using System;
using System.Diagnostics.Metrics;

namespace WeatherSweden.Core
{
    public class WeatherData
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; } // "Inne" eller "Ute"
        public double Temp { get; set; }
        public int Humidity { get; set; }

        // ALGORITMVAL: Mögelrisktabell
        // Jag har valt att implementera en uppslagstabell (lookup table) baserad på
        // "Penthon"-modellen istället för en linjär formel.
        // Orsak: Mögelrisk är inte linjär. Vid låga temperaturer (nära 0) krävs mycket
        // högre fuktighet än vid 20 grader. En tabell ger en mer vetenskapligt
        // korrekt bedömning (0-3) än en enkel matematisk formel
        private static readonly int[,] MoldTable = new int[,]
        {
             {0,0,0,0},      // 0°
             {0,97,98,100},  // 1°
             {0,95,97,100},  // 2°
             {0,93,95,100},  // 3°
             {0,91,93,98},   // 4°
             {0,88,92,97},   // 5°
             {0,87,91,96},   // 6°  
             {0,86,91,95},   // 7°  
             {0,84,90,95},   // 8°  
             {0,83,89,94},   // 9°  
             {0,82,88,93},   // 10°  
             {0,81,88,93},   // 11°  
             {0,81,88,92},   // 12°  
             {0,80,87,92},   // 13°  
             {0,79,87,92},   // 14°  
             {0,79,87,91},   // 15°  
             {0,79,86,91},   // 16°  
             {0,79,86,91},   // 17°  
             {0,79,86,90},   // 18°  
             {0,79,85,90},   // 19°  
             {0,79,85,90},   // 20°  
             {0,79,85,90},   // 21°  
             {0,79,85,89},   // 22°  
             {0,79,84,89},   // 23°  
             {0,79,84,89},   // 24°
             {0,79,84,89},   // 25°  
             {0,79,84,89},   // 26°  
             {0,79,83,88},   // 27°  
             {0,79,83,88},   // 28°  
             {0,79,83,88},   // 29°  
             {0,79,83,88},   // 30°  
             {0,79,83,88},   // 31°  
             {0,79,83,88},   // 32°  
             {0,79,82,88},   // 33°  
             {0,79,82,87},   // 34°  
             {0,79,82,87},   // 35°  
             {0,79,82,87},   // 36°  
             {0,79,82,87},   // 37°  
             {0,79,82,87},   // 38°  
             {0,79,82,87},   // 39°  
             {0,79,82,87},   // 40°  
             {0,79,81,87},   // 41°  
             {0,79,81,87},   // 42°  
             {0,79,81,87},   // 43°  
             {0,79,81,87},   // 44°  
             {0,79,81,86},   // 45°  
             {0,79,81,86},   // 46°  
             {0,79,81,86},   // 47°  
             {0,79,80,86},   // 48°  
             {0,79,80,86},   // 49°  
             {0,79,80,86}    // 50°
        };

        // Egenskap för mögelrisk (0-3)
        // När vi kör programmet kommer "Mögelrisk" visa siffror mellan 0 och 3.
        // 0 = Ingen risk
        // 1 = Risk för tillväxt(efter > 8 veckor)
        // 2 = Risk för tillväxt(efter 4-8 veckor)
        // 3 = Risk för tillväxt(efter 0-4 veckor)
        
        public int MoldRisk
        {
            get
            {
                int tempInt = (int)Math.Round(Temp); // Avrunda temp till närmaste heltal
                int humInt = Humidity;

                // Optimering: Om temperaturen är utanför relevant intervall, avbryt direkt.
                if (tempInt <= 0 || tempInt > 50)
                {
                    return 0;
                }

                // Sökalgoritm:
                // Jag itererar genom risknivåerna (1-3) för den aktuella temperaturen.
                // Detta är effektivare än komplexa if-satser för varje temperatur.

                for (int i = 1; i < 4; i++)
                {
                    // Tabellen: MoldTable[rad, kolumn]
                    int limit = MoldTable[tempInt, i];

                    // Om fukten är MINDRE än gränsvärdet, returnera risknivån (i - 1)
                    if (humInt < limit)
                    {
                        return i - 1;
                    }
                }

                // Om fukten är högre än alla gränsvärden är risken 3
                return 3;
            }
        }
    }
}