
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BookingSystem.Domain.Enums;

namespace BookingSystem.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Only allow users to access their own data unless they are Admin or Manager
            if (currentUserId != id.ToString() &&
                currentUserRole != "Admin" &&
                currentUserRole != "Manager")
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(id);
            return user != null ? Ok(user) : NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto userDto)
        {
            try
            {
                var user = await _userService.CreateUserAsync(userDto);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto userDto)
        {
            if (id != userDto.Id)
                return BadRequest("ID mismatch");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Users can only update their own profile unless they are Admin
            // Managers can update other users but can't change them to Admin
            if (currentUserId != id.ToString() && currentUserRole != "Admin" &&
                !(currentUserRole == "Manager" && userDto.Role != UserRole.Admin))
            {
                return Forbid();
            }

            try
            {
                await _userService.UpdateUserAsync(userDto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Users can only change their own password unless they are Admin
            if (currentUserId != changePasswordDto.UserId.ToString() && currentUserRole != "Admin")
            {
                return Forbid();
            }

            try
            {
                var success = await _userService.ChangePasswordAsync(changePasswordDto);
                if (success)
                    return Ok(new { message = "Password changed successfully" });
                else
                    return BadRequest("Current password is incorrect");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetUserProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            return user != null ? Ok(user) : NotFound();
        }

        [HttpGet("by-email/{email}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            return user != null ? Ok(user) : NotFound();
        }

        [HttpGet("by-username/{username}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            return user != null ? Ok(user) : NotFound();
        }

        [HttpGet("active-ordered")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetActiveUsersOrderedByRegistrationDate()
        {
            var users = await _userService.GetActiveUsersOrderedByRegistrationDateAsync();
            return Ok(users);
        }

        [HttpGet("no-bookings")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersWithNoBookings()
        {
            var users = await _userService.GetUsersWithNoBookingsAsync();
            return Ok(users);
        }
    }
}
