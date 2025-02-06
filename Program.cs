using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using PhotinoNET;
using System.Drawing;
using System.Text;

class Program
{
    public record StockData(string Symbol, string Date, string Time, string Open, string High, string Low, string Close);

    [STAThread]
    static void Main(string[] args)
    {
        var window = new PhotinoWindow()
            .SetTitle("Assetytics (Current Prices)")
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(800, 600))
            .Center()
            .SetResizable(true)
            .SetLogVerbosity(0)
            .SetDevToolsEnabled(false)
            .SetContextMenuEnabled(false)
            .Load("wwwroot/index.html");

            window.RegisterWebMessageReceivedHandler(async (sender, message) =>
            {
                // Check if the message is to update stocks
                if (message == "updateStocks")
                    await FetchAndDisplayAllStockData(window);
                else
                    Console.WriteLine($"Received unknown message: {message}"); 
            });

        window.WaitForClose();
    }

private static async Task FetchAndDisplayAllStockData(PhotinoWindow window)
{
    Dictionary<string, string> stocks = new()
    {
        { "oil", "CL.F" },    // Crude oil
        { "gold", "GC.F" },   // Gold
        { "silver", "SI.F" }  // Silver
    };

    List<string> stockDataList = new();

    try
    {
        using HttpClient client = new();

        // Fetch data for all stocks
        foreach (KeyValuePair<string, string> stock in stocks)
        {
            string url = $"https://stooq.com/q/l/?s={stock.Value}&f=sd2t2ohlc&h&e=csv";
            string csvData = await client.GetStringAsync(url);

            // Log the raw CSV data for debugging
            //Console.WriteLine($"Raw CSV data for {stock.Key}:\n{csvData}");

            using StringReader reader = new(csvData);
            using CsvReader csv = new(reader, CultureInfo.InvariantCulture);
            List<StockData> records = csv.GetRecords<StockData>().ToList();

            if (records.Count < 1)
                throw new InvalidDataException($"CSV data for {stock.Key} is incomplete or in an unexpected format.");

            StockData priceData = records[0];
            float previousPrice = float.Parse(priceData.Open, CultureInfo.InvariantCulture);
            float currentPrice = float.Parse(priceData.Close, CultureInfo.InvariantCulture);
            float change = (float)Math.Round(currentPrice - previousPrice, 2);

            string changeText = change >= 0 ? $"+{change}" : change.ToString();

            // Collect the stock data to be sent to the JavaScript
            stockDataList.Add($@"
                document.getElementById('{stock.Key}-price').textContent = '{currentPrice:N2}';
                document.getElementById('{stock.Key}-change').textContent = '{changeText}';
            ");
        }

        // Send all updates to JavaScript at once
        window.SendWebMessage(string.Join("\n", stockDataList));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching stock data: {ex.Message}");
    }
}
}