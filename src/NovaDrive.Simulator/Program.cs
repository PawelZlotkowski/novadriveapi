// Simulator/Program.cs
using NovaDrive.Simulator;

Console.WriteLine("=== Nova Drive Vehicle Simulator ===");
Console.WriteLine("Generating telemetry and sensor data...\n");

var mongoConnectionString = args.Length > 0
    ? args[0]
    : "mongodb://localhost:27017";

var apiBaseUrl = args.Length > 1
    ? args[1]
    : "http://localhost:5000";

var apiKey = args.Length > 2
    ? args[2]
    : "nd-vehicle-key-change-me-in-production";

var simulator = new VehicleSimulator(mongoConnectionString, apiBaseUrl, apiKey);
await simulator.RunAsync(vehicleCount: 10, durationSeconds: 300, intervalMs: 5000);