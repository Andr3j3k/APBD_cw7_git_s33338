using APBD_cw7_git_s33338.DTOs;
using APBD_cw7_git_s33338.Exceptions;
using APBD_cw7_git_s33338.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_cw7_git_s33338.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentsService service):ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? patientLastName)
    {
        return Ok(await service.GetAllAsync(status, patientLastName));
    }

    [HttpGet("{idAppointment:int}")]
    public async Task<IActionResult> GetById([FromRoute] int idAppointment)
    {
        try
        {
            return Ok(await service.GetByIdAsync(idAppointment));
        }
        catch (AppointmentNotFoundException e)
        {
            return NotFound(new ErrorResponseDto { Message = e.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequestDto dto)
    {
        try
        {
            var id = await service.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { idAppointment = id }, new { IdAppointment = id });
        }
        catch (BusinessRuleException e)
        {
            return Conflict(new ErrorResponseDto { Message = e.Message });
        }
    }

    [HttpPut("{idAppointment:int}")]
    public async Task<IActionResult> Update([FromRoute] int idAppointment, [FromBody] UpdateAppointmentRequestDto dto)
    {
        try
        {
            await service.UpdateAsync(idAppointment, dto);
            return Ok();
        }
        catch (AppointmentNotFoundException e)
        {
            return NotFound(new ErrorResponseDto { Message = e.Message });
        }
        catch (BusinessRuleException e)
        {
            return Conflict(new ErrorResponseDto { Message = e.Message });
        }
    }

    [HttpDelete("{idAppointment:int}")]
    public async Task<IActionResult> Delete([FromRoute] int idAppointment)
    {
        try
        {
            await service.DeleteAsync(idAppointment);
            return NoContent();
        }
        catch (AppointmentNotFoundException e)
        {
            return NotFound(new ErrorResponseDto { Message = e.Message });
        }
        catch (BusinessRuleException e)
        {
            return Conflict(new ErrorResponseDto { Message = e.Message });
        }
    }
}