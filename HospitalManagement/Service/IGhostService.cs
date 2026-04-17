using System;

namespace HospitalManagement.Service;
public interface IGhostService
{
    static abstract GhostService Instance { get; }

    event EventHandler? ExorcismTriggered;

    bool IsExorcismTriggered();
    void SawAGhost();
}