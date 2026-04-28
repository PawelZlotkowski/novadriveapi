namespace NovaDrive.Simulator;

using System.Net.Http.Json;
using System.Text.Json;

public enum RidePhase { Idle, GoingToPickup, GoingToDestination }

public class VehicleSimulator
{
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();
    private readonly List<SimulatedVehicle> _vehicles = new();

    public VehicleSimulator(string mongoConnectionString, string apiBaseUrl, string apiKey)
    {
        _ = mongoConnectionString;
        _httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task RunAsync(int intervalMs = 5000)
    {
        Console.WriteLine("Fetching real vehicle IDs from API…");
        await LoadVehiclesAsync();

        if (_vehicles.Count == 0)
        {
            Console.WriteLine("No active vehicles found. Retrying in 30 s…");
            await Task.Delay(30_000);
            await LoadVehiclesAsync();
        }

        Console.WriteLine($"Simulating {_vehicles.Count} vehicles (interval {intervalMs} ms). Press Ctrl+C to stop.\n");

        _ = Task.Run(RideDispatchLoopAsync);

        int tick = 0;
        while (true)
        {
            tick++;
            var batch = new List<object>(_vehicles.Count);

            foreach (var v in _vehicles)
            {
                UpdateVehiclePosition(v, intervalMs);

                batch.Add(new
                {
                    vehicleId                  = v.VehicleId,
                    latitude                   = v.Latitude,
                    longitude                  = v.Longitude,
                    speedKmh                   = v.SpeedKmh,
                    batteryPercentage          = v.BatteryPercentage,
                    hardwareTemperatureCelsius = v.HardwareTemp,
                });

                if (_random.NextDouble() < 0.04)
                    await SendDiagnosticAsync(v);
            }

            try
            {
                var resp = await _httpClient.PostAsJsonAsync("/api/vehicle/telemetry/batch", batch);
                Console.Write($"\r  Tick {tick}: {(resp.IsSuccessStatusCode ? $"OK — {batch.Count} readings" : $"FAILED ({resp.StatusCode})")}   ");
            }
            catch (Exception ex)
            {
                Console.Write($"\r  Tick {tick}: ERROR {ex.Message[..Math.Min(60, ex.Message.Length)]}");
            }

            await Task.Delay(intervalMs);
        }
    }

    // ── Movement ──────────────────────────────────────────────────────────────────

    private void UpdateVehiclePosition(SimulatedVehicle v, int intervalMs)
    {
        v.HardwareTemp = Math.Clamp(v.HardwareTemp + (_random.NextDouble() - 0.5) * 1.5, 20, 75);

        if (v.Phase != RidePhase.Idle)
        {
            // Directed movement: drive toward the current target at city speed
            double speedKmh = 30 + _random.NextDouble() * 25; // 30–55 km/h
            double stepKm   = speedKmh * (intervalMs / 1000.0) / 3600.0;
            double stepDeg  = stepKm / 111.0; // approx degrees per step

            var dLat    = v.TargetLat - v.Latitude;
            var dLng    = v.TargetLng - v.Longitude;
            var distDeg = Math.Sqrt(dLat * dLat + dLng * dLng);

            v.SpeedKmh          = speedKmh;
            v.BatteryPercentage = Math.Max(5, v.BatteryPercentage - _random.NextDouble() * 0.20);

            const double ArrivalDeg = 0.0004; // ~44 m

            if (distDeg <= ArrivalDeg)
            {
                // Snap to target
                v.Latitude  = v.TargetLat;
                v.Longitude = v.TargetLng;
                v.SpeedKmh  = 0;

                if (v.Phase == RidePhase.GoingToPickup)
                {
                    var rideId = v.ActiveRideId!.Value;
                    Console.WriteLine($"\n  [move] Arrived at pickup — starting ride {rideId}");
                    // Transition to next phase before firing async so no double-trigger
                    v.Phase     = RidePhase.GoingToDestination;
                    v.TargetLat = v.DestLat;
                    v.TargetLng = v.DestLng;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var r = await _httpClient.PostAsync($"/api/vehicle/rides/{rideId}/start", null);
                            Console.WriteLine($"  [move] Start → {r.StatusCode}");
                        }
                        catch (Exception ex) { Console.WriteLine($"  [move] Start error: {ex.Message}"); }
                    });
                }
                else if (v.Phase == RidePhase.GoingToDestination)
                {
                    var rideId   = v.ActiveRideId!.Value;
                    var dist     = HaversineKm(v.PickupLat, v.PickupLng, v.DestLat, v.DestLng);
                    var duration = (int)Math.Round(dist / 35.0 * 60.0);
                    Console.WriteLine($"\n  [move] Arrived at destination — completing ride {rideId} ({dist:F1} km, {duration} min)");

                    v.Phase        = RidePhase.Idle;
                    v.ActiveRideId = null;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var body = JsonContent.Create(new { distanceKm = Math.Round(dist, 1), durationMinutes = duration });
                            var r    = await _httpClient.PostAsync($"/api/vehicle/rides/{rideId}/complete", body);
                            Console.WriteLine($"  [move] Complete → {r.StatusCode}");
                        }
                        catch (Exception ex) { Console.WriteLine($"  [move] Complete error: {ex.Message}"); }
                    });
                }
            }
            else
            {
                // Move one step toward target
                var fraction = Math.Min(stepDeg / distDeg, 1.0);
                v.Latitude  += dLat * fraction;
                v.Longitude += dLng * fraction;
            }
        }
        else
        {
            // Idle: gentle random walk so vehicles spread around Kortrijk
            v.Latitude  = Math.Clamp(v.Latitude  + (_random.NextDouble() - 0.5) * 0.0008, 50.75, 50.92);
            v.Longitude = Math.Clamp(v.Longitude + (_random.NextDouble() - 0.5) * 0.0008, 3.10,  3.40);
            v.SpeedKmh  = Math.Clamp(v.SpeedKmh  + _random.Next(-5, 6), 0, 25);
            v.BatteryPercentage = Math.Max(5, v.BatteryPercentage - _random.NextDouble() * 0.05);
        }
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ── Ride dispatch ─────────────────────────────────────────────────────────────

    private async Task RideDispatchLoopAsync()
    {
        await Task.Delay(10_000);

        while (true)
        {
            try
            {
                var resp = await _httpClient.GetFromJsonAsync<PendingRidesDto>("/api/vehicle/rides/pending");
                if (resp is null) { await Task.Delay(10_000); continue; }

                // Assign Requested rides — vehicle drives to pickup autonomously
                foreach (var r in resp.Requested.Where(r => r.VehicleId.HasValue))
                {
                    var vehicle = _vehicles.FirstOrDefault(v => v.VehicleId == r.VehicleId!.Value);
                    if (vehicle is null || vehicle.ActiveRideId.HasValue) continue;

                    Console.WriteLine($"\n  [dispatch] Ride {r.Id} — heading to pickup ({r.DepartureLatitude:F4}, {r.DepartureLongitude:F4})");
                    vehicle.ActiveRideId = r.Id;
                    vehicle.Phase        = RidePhase.GoingToPickup;
                    vehicle.TargetLat    = r.DepartureLatitude;
                    vehicle.TargetLng    = r.DepartureLongitude;
                    vehicle.PickupLat    = r.DepartureLatitude;
                    vehicle.PickupLng    = r.DepartureLongitude;
                    vehicle.DestLat      = r.DestinationLatitude;
                    vehicle.DestLng      = r.DestinationLongitude;
                }

                // Resume EnRoute rides if simulator restarted mid-ride
                foreach (var r in resp.EnRoute.Where(r => r.VehicleId.HasValue))
                {
                    var vehicle = _vehicles.FirstOrDefault(v => v.VehicleId == r.VehicleId!.Value);
                    if (vehicle is null || vehicle.Phase == RidePhase.GoingToDestination) continue;

                    Console.WriteLine($"\n  [dispatch] Resuming ride {r.Id} → destination ({r.DestinationLatitude:F4}, {r.DestinationLongitude:F4})");
                    vehicle.ActiveRideId = r.Id;
                    vehicle.Phase        = RidePhase.GoingToDestination;
                    vehicle.TargetLat    = r.DestinationLatitude;
                    vehicle.TargetLng    = r.DestinationLongitude;
                    vehicle.PickupLat    = r.DepartureLatitude;
                    vehicle.PickupLng    = r.DepartureLongitude;
                    vehicle.DestLat      = r.DestinationLatitude;
                    vehicle.DestLng      = r.DestinationLongitude;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n  [dispatch] ERROR: {ex.Message[..Math.Min(80, ex.Message.Length)]}");
            }

            await Task.Delay(12_000);
        }
    }

    // ── Vehicle loading ───────────────────────────────────────────────────────────

    private async Task LoadVehiclesAsync()
    {
        try
        {
            var list = await _httpClient.GetFromJsonAsync<List<VehicleDto>>("/api/vehicle/vehicles");
            if (list is null) return;

            _vehicles.Clear();
            foreach (var v in list)
            {
                _vehicles.Add(new SimulatedVehicle
                {
                    VehicleId         = v.Id,
                    Latitude          = v.Latitude  + (_random.NextDouble() - 0.5) * 0.005,
                    Longitude         = v.Longitude + (_random.NextDouble() - 0.5) * 0.005,
                    SpeedKmh          = _random.Next(0, 20),
                    BatteryPercentage = 50 + _random.NextDouble() * 50,
                    HardwareTemp      = 35 + _random.NextDouble() * 10,
                });
                Console.WriteLine($"  + {v.Model} ({v.LicensePlate}) — {v.Id}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Vehicle fetch failed: {ex.Message}");
        }
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────────

    private async Task SendDiagnosticAsync(SimulatedVehicle v)
    {
        var sensorTypes = new[] { "Lidar", "Radar", "Camera" };
        var severities  = new[] { "Info", "Info", "Warning", "Warning", "Critical" };
        var errorCodes  = new Dictionary<string, string[]>
        {
            ["Lidar"]  = ["LID-1001", "LID-2003", "LID-4021", "LID-5000"],
            ["Radar"]  = ["RAD-1001", "RAD-2010", "RAD-3055", "RAD-4002"],
            ["Camera"] = ["CAM-1001", "CAM-2020", "CAM-3001", "CAM-4015"],
        };

        var sensor   = sensorTypes[_random.Next(sensorTypes.Length)];
        var severity = severities[_random.Next(severities.Length)];

        var diag = new
        {
            vehicleId  = v.VehicleId,
            sensorType = sensor,
            errorCode  = errorCodes[sensor][_random.Next(errorCodes[sensor].Length)],
            severity,
            rawSensorData = new
            {
                signalStrength    = _random.NextDouble() * 100,
                noiseLevel        = _random.NextDouble() * 10,
                calibrationOffset = (_random.NextDouble() - 0.5) * 2,
                frameRate         = sensor == "Camera" ? (int?)_random.Next(15, 61) : null,
                rangeMeters       = sensor != "Camera" ? (int?)_random.Next(10, 200) : null,
            },
        };

        try { await _httpClient.PostAsJsonAsync("/api/vehicle/diagnostics", diag); }
        catch { /* silently ignore */ }
    }
}

// ── Domain types ──────────────────────────────────────────────────────────────

public class SimulatedVehicle
{
    public Guid   VehicleId         { get; set; }
    public double Latitude          { get; set; }
    public double Longitude         { get; set; }
    public double SpeedKmh          { get; set; }
    public double BatteryPercentage { get; set; }
    public double HardwareTemp      { get; set; }

    // Active ride state
    public Guid?     ActiveRideId { get; set; }
    public RidePhase Phase        { get; set; } = RidePhase.Idle;
    public double    TargetLat    { get; set; }
    public double    TargetLng    { get; set; }
    public double    PickupLat    { get; set; }
    public double    PickupLng    { get; set; }
    public double    DestLat      { get; set; }
    public double    DestLng      { get; set; }
}

public class VehicleDto
{
    public Guid   Id           { get; set; }
    public string Model        { get; set; } = "";
    public string LicensePlate { get; set; } = "";
    public double Latitude     { get; set; }
    public double Longitude    { get; set; }
}

public class PendingRidesDto
{
    public List<PendingRideItem> Requested { get; set; } = [];
    public List<EnRouteRideItem> EnRoute   { get; set; } = [];
}

public class PendingRideItem
{
    public Guid     Id                   { get; set; }
    public Guid?    VehicleId            { get; set; }
    public DateTime RequestedAt          { get; set; }
    public double   DepartureLatitude    { get; set; }
    public double   DepartureLongitude   { get; set; }
    public double   DestinationLatitude  { get; set; }
    public double   DestinationLongitude { get; set; }
}

public class EnRouteRideItem
{
    public Guid      Id                   { get; set; }
    public Guid?     VehicleId            { get; set; }
    public DateTime? StartedAt            { get; set; }
    public double    DepartureLatitude    { get; set; }
    public double    DepartureLongitude   { get; set; }
    public double    DestinationLatitude  { get; set; }
    public double    DestinationLongitude { get; set; }
}
