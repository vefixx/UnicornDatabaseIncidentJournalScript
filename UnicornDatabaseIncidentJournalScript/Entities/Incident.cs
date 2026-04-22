namespace UnicornDatabaseIncidentJournalScript.Entities;

/// <summary>
/// Объект в таблице incidents_journal
/// </summary>
public class Incident
{
    public int IncidentId { get; set; }
    public string Ts { get; set; }
    public int BuildingId { get; set; }
    public string Building { get; set; }
    public string Complex { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentNo { get; set; }
    public string Category { get; set; }
    public string Message { get; set; }
}