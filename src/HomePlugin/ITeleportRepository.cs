using System.Collections.Generic;

namespace HomePlugin;

public interface ITeleportRepository
{
    void AddPoint(TeleportPoint point);
    int RemovePoint(string userId, string name);
    IEnumerable<TeleportPoint> GetPoints(string userId);
    bool TryGetPoint(string userId, string name, out TeleportPoint point);
    TeleportPoint GetPoint(string userId, string name);
    int GetPointsCount(string userId);
}
