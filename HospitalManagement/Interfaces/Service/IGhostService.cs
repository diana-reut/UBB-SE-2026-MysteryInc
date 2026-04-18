using System;

namespace HospitalManagement.Interfaces.Service;

internal interface IGhostService
{
    public event EventHandler? ExorcismTriggered;

    public void SawAGhost();

    public bool IsExorcismTriggered();
}
