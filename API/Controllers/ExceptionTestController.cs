using API.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExceptionTestController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound()
    {
        throw new NotFoundException("The requested test resource was not found.");
    }

    [HttpGet("validation")]
    public IActionResult ThrowValidation()
    {
        throw new ValidationException("The request failed validation.");
    }

    [HttpGet("data-annotation-validation")]
    public IActionResult ThrowDataAnnotationValidation()
    {
        throw new System.ComponentModel.DataAnnotations.ValidationException("The model state is invalid.");
    }

    [HttpGet("unauthorized")]
    public IActionResult ThrowUnauthorized()
    {
        throw new UnauthorizedAccessException("The request is not authorized.");
    }

    [HttpGet("generic")]
    public IActionResult ThrowGeneric()
    {
        throw new Exception("This is a generic unhandled exception.");
    }
}
