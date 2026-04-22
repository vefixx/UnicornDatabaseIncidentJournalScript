using System.ComponentModel.DataAnnotations.Schema;

namespace UnicornDatabaseIncidentJournalScript.Entities;

/// <summary>
/// Представление опасных сценариев view_danger_energy_with_no_motion_scen
/// </summary>
[Table("view_danger_energy_with_no_motion_scen")]
public class DangerEnergyNoMotionScen
{
    public string Ts { get; set; }
    public int BuildingId { get; set; }
    public string Building { get; set; }
    public string Complex { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentNo { get; set; }
    public double DeltaPercent { get; set; }
    public bool HasMotion { get; set; }
    public bool IsDanger { get; set; }
}