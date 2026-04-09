// Application/Services/SensorDiagnosticService.cs
namespace NovaDrive.Application.Services;

using NovaDrive.Application.DTOs.Requests;
using NovaDrive.Application.DTOs.Responses;
using NovaDrive.Application.DTOs;
using NovaDrive.Application.Mappings;
using NovaDrive.Domain.Documents;
using NovaDrive.Infrastructure.MongoDb;

public interface ISensorDiagnosticService
{
    Task RecordDiagnosticAsync(CreateSensorDiagnosticRequest request);
    Task<PaginatedResponse<SensorDiagnosticResponse>> GetByVehicleAsync(Guid vehicleId, int page, int pageSize);
    Task<IEnumerable<SensorDiagnosticResponse>> GetCriticalAlertsAsync(int limit = 50);
    Task<PaginatedResponse<SensorDiagnosticResponse>> GetBySeverityAsync(string severity, int page, int pageSize);
}

public class SensorDiagnosticService : ISensorDiagnosticService
{
    private readonly ISensorDiagnosticRepository _sensorRepo;

    public SensorDiagnosticService(ISensorDiagnosticRepository sensorRepo)
    {
        _sensorRepo = sensorRepo;
    }

    public async Task RecordDiagnosticAsync(CreateSensorDiagnosticRequest request)
    {
        var diagnostic = new SensorDiagnostic
        {
            VehicleId = request.VehicleId.ToString(),
            SensorType = request.SensorType,
            ErrorCode = request.ErrorCode,
            Severity = request.Severity,
            Timestamp = DateTime.UtcNow,
            RawSensorData = request.RawSensorData
        };

        await _sensorRepo.InsertAsync(diagnostic);
    }

    public async Task<PaginatedResponse<SensorDiagnosticResponse>> GetByVehicleAsync(Guid vehicleId, int page, int pageSize)
    {
        var diagnostics = await _sensorRepo.GetByVehicleIdAsync(vehicleId.ToString(), page, pageSize);
        var total = await _sensorRepo.GetCountByVehicleAsync(vehicleId.ToString());

        return new PaginatedResponse<SensorDiagnosticResponse>
        {
            Items = diagnostics.Select(d => d.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = (int)total
        };
    }

    public async Task<IEnumerable<SensorDiagnosticResponse>> GetCriticalAlertsAsync(int limit = 50)
    {
        var diagnostics = await _sensorRepo.GetRecentCriticalAsync(limit);
        return diagnostics.Select(d => d.ToResponse());
    }

    public async Task<PaginatedResponse<SensorDiagnosticResponse>> GetBySeverityAsync(string severity, int page, int pageSize)
    {
        var diagnostics = await _sensorRepo.GetBySeverityAsync(severity, page, pageSize);
        return new PaginatedResponse<SensorDiagnosticResponse>
        {
            Items = diagnostics.Select(d => d.ToResponse()),
            Page = page, PageSize = pageSize, TotalCount = 0 // simplified
        };
    }
}