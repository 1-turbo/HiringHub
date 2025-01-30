﻿using CV.Filtation.System.API.DTO;
using CV_Filtation_System.Core.Entities;
using CV_Filtation_System.Services.Models;
using CV_Filtation_System.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Response = CV_Filtation_System.Services.Models.Response;

namespace CV.Filtation.System.API.Controllers
{
    public class AccountController : APIBaseController
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;


        public AccountController(UserManager<User> userManager,
            SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager,
            IConfiguration configuration, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailService = emailService;
        }
        [HttpPost("Register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterUserDTO model, string role)
        {
            // Check if user already exists
            var userExist = await _userManager.FindByEmailAsync(model.Email);
            if (userExist != null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Response { Status = "Error", Message = "User already exists!" });
            }

            // Validate Password
            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest(new { Message = "Password and Confirm Password do not match." });
            }

            // Ensure the role exists
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                        new Response { Status = "Error", Message = "This Role Does Not Exist." });
            }

            // Create User Object
            var user = new User
            {
                FName = model.FName,
                LName = model.LName,
                Address = model.Address,
                City = model.City,
                Email = model.Email,
                PhoneNumber = model.Phone,
                EmailConfirmed = true,
                UserName = model.Email,
                TwoFactorEnabled = true
            };

            // Attempt to Create User
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                // Log Errors (Optional)
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = $"User Failed to Create. Errors: {errors}" });
            }

            // Assign Role to User
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = "Failed to assign role to user." });
            }

            // Generate Email Confirmation Token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { token, email = user.Email }, Request.Scheme);

            if (string.IsNullOrEmpty(confirmationLink))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = "Failed to generate email confirmation link." });
            }

            try
            {
                var message = new Message(new string[] { user.Email! }, "Confirmation Email Link", confirmationLink);
                _emailService.SendEmail(message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = $"User created, but failed to send email. Error: {ex.Message}" });
            }

            return Ok(new Response { Status = "Success", Message = $"User created & Email Sent to {user.Email} Successfully" });
        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginDTO model)
        {
            // Find User by Email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new LoginResponse { Status = "Error", Message = "Invalid Email or Password" });
            }

            // Check Password Validity
            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new LoginResponse { Status = "Error", Message = "Invalid Email or Password" });
            }

            // Handle Two-Factor Authentication (2FA)
            if (user.TwoFactorEnabled)
            {
                try
                {
                    var otpToken = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                    var message = new Message(new string[] { user.Email! }, "OTP Confirmation", otpToken);
                    _emailService.SendEmail(message);

                    return Ok(new LoginResponse
                    {
                        Status = "Success",
                        Message = $"We have sent an OTP to your email {user.Email}",
                        Token = null,
                        Expiration = null
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new LoginResponse { Status = "Error", Message = $"Failed to send OTP. Error: {ex.Message}" });
                }
            }

            // Generate Claims for JWT Token
            var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(JwtRegisteredClaimNames.Sub, user.Email)
    };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Generate JWT Token
            var jwtToken = GenerateToken(user, authClaims);

            return Ok(new LoginResponse
            {
                Status = "Success",
                Message = "Login successful.",
                Token = jwtToken,
                Expiration = DateTime.UtcNow.AddHours(1)
            });
        }

        [HttpPost("login-2FA")]
        public async Task<IActionResult> LoginWithOTP([FromBody] TwoFactorLoginDTO model)
        {
            if (string.IsNullOrEmpty(model.Code) || string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new Response { Status = "Error", Message = "Email and OTP code are required." });
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound(new Response { Status = "Error", Message = "User not found." });
            }

            // Check if Two-Factor Authentication (2FA) is enabled for the user
            if (!user.TwoFactorEnabled)
            {
                return BadRequest(new Response { Status = "Error", Message = "Two-factor authentication is not enabled for this user." });
            }

            // Attempt to sign in with the OTP code
            var signInResult = await _signInManager.TwoFactorSignInAsync("Email", model.Code, false, false);
            if (!signInResult.Succeeded)
            {
                // You can also inspect signInResult.FailedAttempts or other properties for more info
                return Unauthorized(new Response { Status = "Error", Message = "Invalid OTP code." });
            }

            // Generate Claims for JWT Token
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email)
            };

            // Get user roles and add them as claims
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Generate JWT Token
            var jwtToken = GenerateToken(user, authClaims);

            // Return token with expiration time
            return Ok(new LoginResponse
            {
                Status = "Success",
                Message = "Login successful.",
                Token = jwtToken,
                Expiration = DateTime.UtcNow.AddHours(1) // Token expiration
            });
        }

        private string GenerateToken(User user, List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token, [FromQuery] string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest(new Response { Status = "Error", Message = "Token and email are required." });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new Response { Status = "Error", Message = "User does not exist." });
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(new Response { Status = "Error", Message = "Email is already confirmed." });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = "Invalid or expired token." });
            }

            return Ok(new Response { Status = "Success", Message = "Email verified successfully." });
        }


        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Generate Password Reset Token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Generate the Reset Password Link
                var resetPasswordLink = Url.Action(nameof(ResetPassword), "Authentication",
                    new { token, email = user.Email }, Request.Scheme);

                // Send the link via email
                var message = new Message(new string[] { user.Email }, "Forgot Password Link", resetPasswordLink);
                _emailService.SendEmail(message);

                return Ok(new Response { Status = "Success", Message = $"Password reset request has been sent to {user.Email}. Please check your email and click the link to reset your password." });
            }

            return BadRequest(new Response { Status = "Error", Message = "Could not send reset link. Please try again." });
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest(new Response { Status = "Error", Message = "Invalid token or email." });
            }

            var model = new ResetPassword { Token = token, Email = email };
            return Ok(new { model });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPassword resetPassword)
        {
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user != null)
            {
                // Reset the user's password
                var resetPasswordResult = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);

                if (!resetPasswordResult.Succeeded)
                {
                    // Add errors to ModelState
                    foreach (var error in resetPasswordResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return BadRequest(ModelState); // Return errors to the client
                }

                return Ok(new Response { Status = "Success", Message = "Password has been successfully reset." });
            }

            return BadRequest(new Response { Status = "Error", Message = "Could not reset the password. Please try again." });
        }

    }
}
    