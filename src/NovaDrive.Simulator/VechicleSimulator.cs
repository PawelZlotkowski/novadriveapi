// Simulator/VehicleSimulator.cs
namespace NovaDrive.Simulator;

using System.Net.Http.Json;
using System.Text.Json;

public class VehicleSimulator
{
    private readonly string _mongoConnectionString;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();

    // Simulated vehicles (Brussels area coordinates)
    private readonly List<SimulatedVehicle> _vehicles = new();

    public VehicleSimulator(string mongoConnectionString, string apiBaseUrl, string apiKey)
    {
        _mongoConnectionString = mongoConnectionString;
        _httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task RunAsync(int vehicleCount, int durationSeconds, int intervalMs)
    {
        // Initialize simulated vehicles
        for (int i = 0; i < vehicleCount; i++)
        {
            _vehicles.Add(new SimulatedVehicle
            {
                VehicleId = Guid.NewGuid(),
                Latitude = 51.05 + (_random.NextDouble() * 0.1 - 0.05),   // Brussels area
                Longitude = 3.72 + (_random.NextDouble() * 0.1 - 0.05),
                SpeedKmh = _random.Next(0, 60),
                BatteryPercentage = 60 + _random.NextDouble() * 40,
                HardwareTemp = 35 + _random.NextDouble() * 15
            });

            Console.WriteLine($"  Vehicle {i + 1}: {_vehicles[i].VehicleId}");
        }

        Console.WriteLine($"\nSimulating {vehicleCount} vehicles for {durationSeconds}s at {intervalMs}ms intervals\n");

        var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
        int tickCount = 0;

        while (DateTime.UtcNow < endTime)
        {
            tickCount++;
            var batch = new List<object>();

            foreach (var vehicle in _vehicles)
            {
                // Update vehicle state
                vehicle.Latitude += (_random.NextDouble() - 0.5) * 0.001;
                vehicle.Longitude += (_random.NextDouble() - 0.5) * 0.001;
                vehicle.SpeedKmh = Math.Max(0, vehicle.SpeedKmh + _random.Next(-10, 11));
                vehicle.BatteryPercentage = Math.Max(5, vehicle.BatteryPercentage - _random.NextDouble() * 0.2);
                vehicle.HardwareTemp = Math.Clamp(vehicle.HardwareTemp + (_random.NextDouble() - 0.5) * 2, 20, 80);

                batch.Add(new
                {
                    vehicleId = vehicle.VehicleId,
                    latitude = vehicle.Latitude,
                    longitude = vehicle.Longitude,
                    speedKmh = vehicle.SpeedKmh,
                    batteryPercentage = vehicle.BatteryPercentage,
                    hardwareTemperatureCelsius = vehicle.HardwareTemp
                });

                // Randomly generate sensor diagnostics (5% chance per tick)
                if (_random.NextDouble() < 0.05)
                {
                    await SendSensorDiagnostic(vehicle);
                }
            }

            // Send telemetry batch
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/vehicle/telemetry/batch", batch);
                if (response.IsSuccessStatusCode)
                    Console.Write($"\r  Tick {tickCount}: Sent {batch.Count} telemetry readings");
                else
                    Console.Write($"\r  Tick {tickCount}: FAILED ({response.StatusCode})          ");
            }
            catch (Exception ex)
            {
                Console.Write($"\r  Tick {tickCount}: ERROR: {ex.Message[..Math.Min(50, ex.Message.Length)]}");
            }

            await Task.Delay(intervalMs);
        }

        Console.WriteLine($"\n\nSimulation complete. Sent {tickCount * vehicleCount} telemetry readings.");
    }

    private async Task SendSensorDiagnostic(SimulatedVehicle vehicle)
    {
        var sensorTypes = new[] { "Lidar", "Radar", "Camera" };
        var severities = new[] { "Info", "Info", "Warning", "Warning", "Critical" }; // weighted toward Info
        var errorCodes = new Dictionary<string, string[]>
        {
            ["Lidar"] = new[] { "LID-1001", "LID-2003", "LID-4021", "LID-5000" },
            ["Radar"] = new[] { "RAD-1001", "RAD-2010", "RAD-3055", "RAD-4002" },
            ["Camera"] = new[] { "CAM-1001", "CAM-2020", "CAM-3001", "CAM-4015" }
        };

        var sensorType = sensorTypes[_random.Next(sensorTypes.Length)];
        var severity = severities[_random.Next(severities.Length)];

        var diagnostic = new
        {
            vehicleId = vehicle.VehicleId,
            sensorType,
            errorCode = errorCodes[sensorType][_random.Next(errorCodes[sensorType].Length)],
            severity,
            rawSensorData = new
            {
                signalStrength = _random.NextDouble() * 100,
                noiseLevel = _random.NextDouble() * 10,
                calibrationOffset = (_random.NextDouble() - 0.5) * 2,
                frameRate = sensorType == "Camera" ? _random.Next(15, 61) : (int?)null,
                rangeMeters = sensorType != "Camera" ? _random.Next(10, 200) : (int?)null
            }
        };

        try
        {
            await _httpClient.PostAsJsonAsync("/api/vehicle/diagnostics", diagnostic);
        }
        catch { /* Silently ignore diagnostic send failures */ }
    }
}

public class SimulatedVehicle
{
    public Guid VehicleId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKmh { get; set; }
    public double BatteryPercentage { get; set; }
    public double HardwareTemp { get; set; }
}