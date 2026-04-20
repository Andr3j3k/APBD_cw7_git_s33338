using APBD_cw7_git_s33338.DTOs;
using APBD_cw7_git_s33338.Exceptions;
using APBD_cw7_git_s33338.Repositories;

namespace APBD_cw7_git_s33338.Services;

public class AppointmentsService(IAppointmentsRepository repository) : IAppointmentsService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName)
    {
        return await repository.GetAllAsync(status, patientLastName);
    }

    public async Task<AppointmentDetailsDto> GetByIdAsync(int idAppointment)
    {
        var appointment = await repository.GetByIdAsync(idAppointment);

        if (appointment is null)
            throw new AppointmentNotFoundException($"Appointment with id {idAppointment} was not found.");

        return appointment;
    }

    public async Task<int> CreateAsync(CreateAppointmentRequestDto dto)
    {
        if (dto.AppointmentDate < DateTime.Now)
            throw new BusinessRuleException("Appointment date cannot be in the past.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BusinessRuleException("Reason cannot be empty.");

        if (!await repository.PatientExistsAndIsActiveAsync(dto.IdPatient))
            throw new BusinessRuleException("Patient does not exist or is inactive.");

        if (!await repository.DoctorExistsAndIsActiveAsync(dto.IdDoctor))
            throw new BusinessRuleException("Doctor does not exist or is inactive.");

        if (await repository.DoctorHasConflictAsync(dto.IdDoctor, dto.AppointmentDate))
            throw new BusinessRuleException("Doctor already has an appointment at this time.");

        return await repository.CreateAsync(dto);
    }

    public async Task UpdateAsync(int idAppointment, UpdateAppointmentRequestDto dto)
    {
        if (!await repository.AppointmentExistsAsync(idAppointment))
            throw new AppointmentNotFoundException($"Appointment with id {idAppointment} was not found.");

        if (!await repository.PatientExistsAndIsActiveAsync(dto.IdPatient))
            throw new BusinessRuleException("Patient does not exist or is inactive.");

        if (!await repository.DoctorExistsAndIsActiveAsync(dto.IdDoctor))
            throw new BusinessRuleException("Doctor does not exist or is inactive.");

        var currentStatus = await repository.GetAppointmentStatusAsync(idAppointment);
        var currentDate = await repository.GetAppointmentDateAsync(idAppointment);

        if (currentStatus == "Completed" && currentDate.HasValue && currentDate.Value != dto.AppointmentDate)
            throw new BusinessRuleException("Completed appointment date cannot be changed.");

        if (await repository.DoctorHasConflictAsync(dto.IdDoctor, dto.AppointmentDate, idAppointment))
            throw new BusinessRuleException("Doctor already has another appointment at this time.");

        await repository.UpdateAsync(idAppointment, dto);
    }

    public async Task DeleteAsync(int idAppointment)
    {
        if (!await repository.AppointmentExistsAsync(idAppointment))
            throw new AppointmentNotFoundException($"Appointment with id {idAppointment} was not found.");

        var status = await repository.GetAppointmentStatusAsync(idAppointment);

        if (status == "Completed")
            throw new BusinessRuleException("Completed appointment cannot be deleted.");

        await repository.DeleteAsync(idAppointment);
    }
}