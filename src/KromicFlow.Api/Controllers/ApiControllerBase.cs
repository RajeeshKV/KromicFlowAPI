using KromicFlow.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResult(Result result) => result.Succeeded ? Ok() : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    protected IActionResult FromResult<T>(Result<T> result) => result.Succeeded ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
}
