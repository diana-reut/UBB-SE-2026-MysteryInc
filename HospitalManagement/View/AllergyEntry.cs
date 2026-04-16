using Microsoft.UI.Xaml.Controls;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HospitalManagement.View;

// Simple wrapper class for allergy entries
internal class AllergyEntry
{
    public Allergy Allergy { get; set; } = null!;

    public string Severity { get; set; } = null!;
}
