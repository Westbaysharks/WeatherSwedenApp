using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WeatherSweden.Core;
using WeatherSweden.DataAccess;

namespace WeatherSweden.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            string csvPath = "../../../TempFuktData.csv";
            Console.Title = "Weather Sweden Analysis Tool";

            using (var db = new WeatherContext())
            {
                db.Database.EnsureCreated();
                if (!db.WeatherDatas.Any())
                {
                    AnsiConsole.Status()
                        .Start("Initialiserar databas...", ctx =>
                        {
                            ctx.Spinner(Spinner.Known.Star);
                            ctx.Status("Databasen är tom. Laddar CSV-fil...");
                            CsvLoader.LoadData(db, csvPath);
                        });

                    AnsiConsole.MarkupLine("[green]Klart! Tryck Enter för att starta.[/]");
                    Console.ReadLine();
                }

                bool running = true;
                while (running)
                {
                    Console.Clear();

                    AnsiConsole.Write(
                        new FigletText("WEATHER SWEDEN")
                            .Color(Spectre.Console.Color.Cyan1));

                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Välj ett alternativ från menyn:")
                            .PageSize(15)
                            .MoreChoicesText("[grey](Flytta upp/ner för fler val)[/]")
                            .AddChoices(new[] {
                                "UTOMHUS (OUTSIDE)",
                                "1. Sök medeltemperatur för valt datum",
                                "2. Sortera varmast till kallast dag",
                                "3. Sortera torrast till fuktigast dag",
                                "4. Sortera mögelrisk (Minst till Störst)",
                                "5. Visa datum för Meteorologisk Höst",
                                "6. Visa datum för Meteorologisk Vinter",
                                "INOMHUS (INSIDE)",
                                "7. Sök medeltemperatur för valt datum",
                                "8. Sortera varmast till kallast dag",
                                "9. Sortera torrast till fuktigast dag",
                                "10. Sortera mögelrisk (Minst till Störst)",
                                "UTÖKADE UPPGIFTER",
                                "11. Balkongdörr öppen (Tid)",
                                "12. Temperaturskillnad (Inne vs Ute)",
                                "Avsluta",
                                "q. Avsluta"
                            }));

                    if (choice.Contains("OUTSIDE") || choice.Contains("INSIDE") || choice.Contains("UPPGIFTER"))
                        continue;

                    AnsiConsole.WriteLine();

                    switch (choice.Split('.')[0])
                    {
                        case "1": SearchAvgTemp(db, "Ute"); break;
                        case "2": SortTemp(db, "Ute"); break;
                        case "3": SortHumidity(db, "Ute"); break;
                        case "4": SortMoldRisk(db, "Ute"); break;
                        case "5": ShowSeason(db, 10.0, "Autumn"); break;
                        case "6": ShowSeason(db, 0.0, "Winter"); break;

                        case "7": SearchAvgTemp(db, "Inne"); break;
                        case "8": SortTemp(db, "Inne"); break;
                        case "9": SortHumidity(db, "Inne"); break;
                        case "10": SortMoldRisk(db, "Inne"); break;

                        case "11": ShowBalcony(db); break;
                        case "12": ShowTempDiff(db); break;

                        case "q":
                        case "Avsluta":
                            running = false;
                            break;
                    }

                    if (running)
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[grey]Tryck Enter för att återgå till menyn...[/]");
                        Console.ReadLine();
                    }
                }
            }
        }

        // Medeltemperatur för valt datum (sökmöjlighet) 
        static void SearchAvgTemp(WeatherContext db, string location)
        {
            var input = AnsiConsole.Ask<string>($"Ange datum (yyyy-mm-dd) för [yellow]{location}[/]: ");

            if (DateTime.TryParse(input, out DateTime date))
            {
                var avgTemp = db.WeatherDatas
                    .Where(x => x.Date.Date == date.Date && x.Location == location)
                    .Average(x => (double?)x.Temp);

                if (avgTemp.HasValue)
                {
                    AnsiConsole.MarkupLine($"Medeltemperatur {location} den [cyan]{date:yyyy-MM-dd}[/]: [green]{avgTemp.Value:F1} °C[/]");
                }
                else
                    AnsiConsole.MarkupLine($"[red]Ingen data hittades för {date:yyyy-MM-dd} ({location}).[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Felaktigt datumformat.[/]");
            }
        }

        // Sortering varmast till kallast
        static void SortTemp(WeatherContext db, string location)
        {
            var table = new Spectre.Console.Table();
            table.AddColumn("Datum");
            table.AddColumn("Temp (°C)");
            table.Title($"Top 5 Varmaste dagarna ({location})");

            var list = db.WeatherDatas
                .Where(x => x.Location == location)
                .GroupBy(x => x.Date.Date)
                .Select(g => new { Date = g.Key, AvgTemp = g.Average(x => x.Temp) })
                .OrderByDescending(x => x.AvgTemp)
                .Take(5)
                .ToList();

            foreach (var item in list)
            {
                table.AddRow(item.Date.ToString("yyyy-MM-dd"), $"{item.AvgTemp:F1}");
            }
            AnsiConsole.Write(table);
        }

        // Sortering torrast till fuktigast 
        static void SortHumidity(WeatherContext db, string location)
        {
            var table = new Spectre.Console.Table();
            table.AddColumn("Datum");
            table.AddColumn("Luftfuktighet (%)");
            table.Title($"Top 5 Torraste dagarna ({location})");

            var list = db.WeatherDatas
                .Where(x => x.Location == location)
                .GroupBy(x => x.Date.Date)
                .Select(g => new { Date = g.Key, AvgHum = g.Average(x => x.Humidity) })
                .OrderBy(x => x.AvgHum)
                .Take(5)
                .ToList();

            foreach (var item in list)
            {
                table.AddRow(item.Date.ToString("yyyy-MM-dd"), $"{item.AvgHum:F1}");
            }
            AnsiConsole.Write(table);
        }

        // ALGORITMVAL: Aggregering av Mögelrisk
        // Jag väljer att använda Max() istället för Average() för dygnsrisken.
        // Orsak: Ett medelvärde döljer toppar. Om risken är hög (3) under några timmar på natten
        // men låg (0) på dagen, skulle medelvärdet bli lågt (t.ex. 0.8), vilket invaggar användaren i falsk trygghet.
        // Max-värdet avslöjar om kritiska nivåer uppnåddes någon gång under dygnet.
        static void SortMoldRisk(WeatherContext db, string location)
        {
            var table = new Spectre.Console.Table();
            table.AddColumn("Datum");
            table.AddColumn("Mögelrisk (0-3)");
            table.AddColumn("Status");
            table.Title($"Dagar med högst mögelrisk ({location})");

            var dataInMemory = db.WeatherDatas.Where(x => x.Location == location).ToList();

            var list = dataInMemory
               .GroupBy(x => x.Date.Date)
               .Select(g => new
               {
                   Date = g.Key,
                   MaxRisk = g.Max(x => x.MoldRisk)
               })
                .OrderByDescending(x => x.MaxRisk)
                .Take(5);

            foreach (var item in list)
            {
                string status = item.MaxRisk > 0 ? "[red]Risk![/]" : "[green]Ingen risk[/]";
                table.AddRow(item.Date.ToString("yyyy-MM-dd"), item.MaxRisk.ToString(), status);
            }
            AnsiConsole.Write(table);
        }

        // Meteorologisk Höst/Vinter
        static void ShowSeason(WeatherContext db, double limit, string seasonName)
        {
            var data = db.WeatherDatas.Where(x => x.Location == "Ute").ToList();

            var start = SeasonLogic.FindStartOfSeason(data, limit, true);

            if (start.HasValue)
                AnsiConsole.MarkupLine($"Meteorologisk {seasonName} startade den: [green]{start.Value:yyyy-MM-dd}[/]");
            else
                AnsiConsole.MarkupLine($"[yellow]Ingen meteorologisk {seasonName} identifierades i denna datamängd.[/]");
        }

        static void ShowBalcony(WeatherContext db)
        {
            AnsiConsole.MarkupLine("[grey]Beräknar balkongtider...[/]");

            var allData = db.WeatherDatas.ToList();
            var merged = VgAnalysis.MergeData(allData);
            var balcony = VgAnalysis.CalculateBalconyOpenTime(merged);

            var top = balcony.OrderByDescending(x => x.Value).Take(5);

            var table = new Spectre.Console.Table();
            table.AddColumn("Datum");
            table.AddColumn("Tid öppen (min)");
            table.Title("Dagar då balkongdörren var öppen mest");

            foreach (var d in top)
            {
                table.AddRow(d.Key.ToString("yyyy-MM-dd"), $"ca {d.Value}");
            }
            AnsiConsole.Write(table);
        }

        static void ShowTempDiff(WeatherContext db)
        {
            AnsiConsole.MarkupLine("[grey]Beräknar temperaturskillnader...[/]");

            var allData = db.WeatherDatas.ToList();
            var merged = VgAnalysis.MergeData(allData);
            var diffs = VgAnalysis.CalculateTempDiff(merged);

            var table = new Spectre.Console.Table();
            table.AddColumn("Datum");
            table.AddColumn("Skillnad (°C)");
            table.Title("Störst skillnad (Inne vs Ute)");

            foreach (var d in diffs.Take(5))
                table.AddRow(d.Key.ToString("yyyy-MM-dd"), $"{d.Value:F1}");

            AnsiConsole.Write(table);
        }
    }
}