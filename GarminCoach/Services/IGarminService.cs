using GarminCoach.Models;

namespace GarminCoach.Services;

public interface IGarminService
{
    Task<CoachSnapshot> FetchSnapshotAsync(int days, CancellationToken ct = default);
}
