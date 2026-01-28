using Microsoft.AspNetCore.Mvc;
using Wellmeet.DTO;
using Wellmeet.Exceptions;
using Wellmeet.Services.Interfaces;

namespace Wellmeet.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        public AuthController(IApplicationService applicationService)
            : base(applicationService)
        {
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<UserReadOnlyDTO>> Register([FromBody] UserRegisterDTO dto)
        {
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(e => e.Value!.Errors.Any())
                    .Select(e => new {
                        Field = e.Key,
                        Errors = e.Value!.Errors.Select(er => er.ErrorMessage).ToArray()
                    });

                throw new InvalidRegistrationException(
                    "Invalid registration data: " +
                    System.Text.Json.JsonSerializer.Serialize(errors)   //again serilization not sure if needed
                );
            }

            var createdUser = await ApplicationService.UserService.RegisterAsync(dto);

            // Return 201 Created with body
            return StatusCode(201, createdUser);
        }


        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<JwtTokenDTO>> Login([FromBody] UserLoginDTO dto)
        {
            var tokenDto = await ApplicationService.UserService.LoginAsync(dto);
            return Ok(tokenDto);
        }
    }
}
