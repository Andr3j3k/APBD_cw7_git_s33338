using APBD_cw7_git_s33338.DTOs;

namespace APBD_cw7_git_s33338.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName);
    Task<AppointmentDetailsDto> GetByIdAsync(int idAppointment);
    Task<int> CreateAsync(CreateAppointmentRequestDto dto);
    Task UpdateAsync(int idAppointment, UpdateAppointmentRequestDto dto);
    Task DeleteAsync(int idAppointment);
}