using System.Collections.Generic;

namespace HomePlugin;

public interface ITeleportRepository
{
    void AddPoint(TeleportPoint point);
    void RemovePoint(string userId, string name);
    IEnumerable<TeleportPoint> GetPoints(string userId);
    TeleportPoint GetPoint(string userId, string name);
}
