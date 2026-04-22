using System.Data;
using APBD_cw7_git_s33338.DTOs;
using APBD_cw7_git_s33338.Mappers;
using Microsoft.Data.SqlClient;

namespace APBD_cw7_git_s33338.Repositories;

public class AppointmentsRepository(IConfiguration configuration) : IAppointmentsRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName)
    {
        var result = new List<AppointmentListDto>();

        const string sql = """
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                p.FirstName + ' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail
            FROM Appointments a
            JOIN Patients p ON p.IdPatient = a.IdPatient
            WHERE (@Status IS NULL OR a.Status = @Status)
              AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
            ORDER BY a.AppointmentDate;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value =
            string.IsNullOrWhiteSpace(status) ? DBNull.Value : status;

        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 80).Value =
            string.IsNullOrWhiteSpace(patientLastName) ? DBNull.Value : patientLastName;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(AppointmentMapper.ToListDto(reader));
        }

        return result;
    }

    public async Task<AppointmentDetailsDto?> GetByIdAsync(int idAppointment)
    {
        const string sql = """
                           SELECT
                               a.IdAppointment,
                               a.AppointmentDate,
                               a.Status,
                               a.Reason,
                               a.InternalNotes,
                               a.CreatedAt,
                               p.FirstName + ' ' + p.LastName AS PatientFullName,
                               p.Email AS PatientEmail,
                               p.PhoneNumber AS PatientPhoneNumber,
                               d.FirstName + ' ' + d.LastName AS DoctorFullName,
                               d.LicenseNumber AS DoctorLicenseNumber,
                               s.Name AS SpecializationName
                           FROM Appointments a
                           JOIN Patients p ON p.IdPatient = a.IdPatient
                           JOIN Doctors d ON d.IdDoctor = a.IdDoctor
                           JOIN Specializations s ON s.IdSpecialization = d.IdSpecialization
                           WHERE a.IdAppointment = @IdAppointment;
                           """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return AppointmentMapper.ToDetailsDto(reader);
    }

    public async Task<bool> AppointmentExistsAsync(int idAppointment)
    {
        const string sql = "SELECT COUNT(1) FROM Appointments WHERE IdAppointment = @IdAppointment;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await connection.OpenAsync();
        var count = (int)await command.ExecuteScalarAsync();

        return count > 0;
    }

    public async Task<string?> GetAppointmentStatusAsync(int idAppointment)
    {
        const string sql = "SELECT Status FROM Appointments WHERE IdAppointment = @IdAppointment;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        return result as string;
    }

    public async Task<DateTime?> GetAppointmentDateAsync(int idAppointment)
    {
        const string sql = "SELECT AppointmentDate FROM Appointments WHERE IdAppointment = @IdAppointment;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        return result == null ? null : (DateTime)result;
    }

    public async Task<bool> PatientExistsAndIsActiveAsync(int idPatient)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM Patients
            WHERE IdPatient = @IdPatient AND IsActive = 1;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = idPatient;

        await connection.OpenAsync();
        var count = (int)await command.ExecuteScalarAsync();

        return count > 0;
    }

    public async Task<bool> DoctorExistsAndIsActiveAsync(int idDoctor)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM Doctors
            WHERE IdDoctor = @IdDoctor AND IsActive = 1;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;

        await connection.OpenAsync();
        var count = (int)await command.ExecuteScalarAsync();

        return count > 0;
    }

    public async Task<bool> DoctorHasConflictAsync(int idDoctor, DateTime appointmentDate, int? ignoredAppointmentId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM Appointments
            WHERE IdDoctor = @IdDoctor
              AND AppointmentDate = @AppointmentDate
              AND (@IgnoredAppointmentId IS NULL OR IdAppointment <> @IgnoredAppointmentId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = appointmentDate;
        command.Parameters.Add("@IgnoredAppointmentId", SqlDbType.Int).Value =
            ignoredAppointmentId.HasValue ? ignoredAppointmentId.Value : DBNull.Value;

        await connection.OpenAsync();
        var count = (int)await command.ExecuteScalarAsync();

        return count > 0;
    }

    public async Task<int> CreateAsync(CreateAppointmentRequestDto dto)
    {
        const string sql = """
            INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
            VALUES (@IdPatient, @IdDoctor, @AppointmentDate, 'Scheduled', @Reason);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = dto.IdPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = dto.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = dto.AppointmentDate;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = dto.Reason;

        await connection.OpenAsync();
        return (int)await command.ExecuteScalarAsync();
    }

    public async Task UpdateAsync(int idAppointment, UpdateAppointmentRequestDto dto)
    {
        const string sql = """
            UPDATE Appointments
            SET
                IdPatient = @IdPatient,
                IdDoctor = @IdDoctor,
                AppointmentDate = @AppointmentDate,
                Status = @Status,
                Reason = @Reason,
                InternalNotes = @InternalNotes
            WHERE IdAppointment = @IdAppointment;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;
        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = dto.IdPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = dto.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = dto.AppointmentDate;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = dto.Status;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = dto.Reason;
        command.Parameters.Add("@InternalNotes", SqlDbType.NVarChar, 500).Value =
            string.IsNullOrWhiteSpace(dto.InternalNotes) ? DBNull.Value : dto.InternalNotes;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int idAppointment)
    {
        const string sql = "DELETE FROM Appointments WHERE IdAppointment = @IdAppointment;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}