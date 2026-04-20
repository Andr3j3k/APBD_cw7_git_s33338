namespace APBD_cw7_git_s33338.Models;

public class Appointment
{
    public int IdAppointment { get; set; }
    public int IdPatient { get; set; }
    public int IdDoctor { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; }=string.Empty;
    public string Reason { get; set; }=string.Empty;
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}