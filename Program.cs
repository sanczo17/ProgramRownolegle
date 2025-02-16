using System.Collections.Concurrent;
using System.Diagnostics;

class Program
{
    private readonly int NumbersOfCities = 99;
    private readonly int Days = 356;
    private readonly int MinTemp = -10;
    private readonly int MaxTemp = 40;
    private bool _processing = true;
    private bool _generateData = true;
    private bool _printingData;
    private ConcurrentDictionary<string, (double[], double)> _cities = new();
    private ThreadLocal<Random> _rnd = new(() => new Random(Guid.NewGuid().GetHashCode()));
    private Stopwatch _sw = new();

    public async Task Run()
    {
        _sw.Start();
        Thread showStateThread = new Thread(ShowState);
        showStateThread.IsBackground = true;
        showStateThread.Start();

        GenerateData();
        PrintData();
        Thread.Sleep(100);

        var results = await Task.WhenAll(
            Task.Run(() => _cities.Values.SelectMany(temp => temp.Item1).Min()),
            Task.Run(() => _cities.Values.SelectMany(temp => temp.Item1).Max()),
            Task.Run(() => Math.Round(_cities.Values.SelectMany(temp => temp.Item1).Average(), 2))
        );

        PrintResults(results);

        _processing = false;
        showStateThread.Join();
        _sw.Stop();
        Console.WriteLine($"Elapsed time: {_sw.ElapsedMilliseconds} ms");
    }

    void GenerateData()
    {
        Parallel.For(0, NumbersOfCities, i =>
        {
            double[] temperatures = new double[Days];
            for (int j = 0; j < Days; j++)
            {
                temperatures[j] = Math.Round(MinTemp + _rnd.Value.NextDouble() * (MaxTemp - MinTemp), 2);
            }
            double median = CalculateMedian(temperatures);
            _cities.TryAdd($"Miasto{i + 1}", (temperatures, median));
        });
        _generateData = false;
    }

    double CalculateMedian(double[] temperatures)
    {
        double[] sortedTemp = temperatures.OrderBy(t => t).ToArray();
        return (sortedTemp.Length % 2 == 0)
            ? (sortedTemp[sortedTemp.Length / 2 - 1] + sortedTemp[sortedTemp.Length / 2]) / 2.0
            : sortedTemp[sortedTemp.Length / 2];
    }

    void PrintData()
    {
        _printingData = true;
        int count = 0;
        foreach (var city in _cities)
        {
            if (count < 5)
            {
                Thread.Sleep(100);
                Console.WriteLine($"{city.Key}: {string.Join(", ", city.Value.Item1.Take(5))}...");
                Console.WriteLine($"Median: {city.Value.Item2}");
                Console.WriteLine(new string('-', 50));
                count++;
            }
            else break;
        }
        Console.WriteLine($"{_cities.First().Key}: {string.Join(", ", _cities.First().Value.Item1)}");
        _printingData = false;
    }

    void ShowState()
    {
        while (_processing)
        {
            if (_generateData)
                Console.WriteLine("Generating data...");
            else if (_printingData)
                Console.WriteLine("Analyzing data...");
            else
                Console.WriteLine("Generating global temperature data...");

            Thread.Sleep(100);
        }
    }

    void PrintResults(double[] results)
    {
        string[] labels = { "Minimal", "Maximal", "Average" };
        for (int i = 0; i < results.Length; i++)
        {
            Console.WriteLine($"{labels[i]} global temperature: {results[i]}");
        }
    }

    static async Task Main(string[] args)
    {
        var program = new Program();
        await program.Run();
    }
}
