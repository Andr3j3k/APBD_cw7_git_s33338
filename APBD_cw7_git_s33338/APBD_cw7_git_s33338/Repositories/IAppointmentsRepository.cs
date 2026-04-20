using APBD_cw7_git_s33338.DTOs;

namespace APBD_cw7_git_s33338.Repositories;

public interface IAppointmentsRepository
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName);
    Task<AppointmentDetailsDto?> GetByIdAsync(int idAppointment);
    Task<bool> AppointmentExistsAsync(int idAppointment);
    Task<string?> GetAppointmentStatusAsync(int idAppointment);
    Task<DateTime?> GetAppointmentDateAsync(int idAppointment);

    Task<bool> PatientExistsAndIsActiveAsync(int idPatient);
    Task<bool> DoctorExistsAndIsActiveAsync(int idDoctor);
    Task<bool> DoctorHasConflictAsync(int idDoctor, DateTime appointmentDate, int? ignoredAppointmentId = null);

    Task<int> CreateAsync(CreateAppointmentRequestDto dto);
    Task UpdateAsync(int idAppointment, UpdateAppointmentRequestDto dto);
    Task DeleteAsync(int idAppointment);
}