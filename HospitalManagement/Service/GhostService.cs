using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagement.Service
{
    public class GhostService
    {
        private static readonly GhostService _instance = new GhostService();
        public static GhostService Instance => _instance;
        private GhostService() { }

        private readonly List<DateTime> _sightings = new();
        public event EventHandler? ExorcismTriggered;

        public void SawAGhost()
        {
            CleanOldSightings();
            _sightings.Add(DateTime.Now);
            if (IsExorcismTriggered())
                ExorcismTriggered?.Invoke(this, EventArgs.Empty);
        }

        public bool IsExorcismTriggered()
        {
            CleanOldSightings();
            return _sightings.Count > 3;
        }

        private void CleanOldSightings()
        {
            _sightings.RemoveAll(s => s < DateTime.Now.AddHours(-24));
        }
    }
}