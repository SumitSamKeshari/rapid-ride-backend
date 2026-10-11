using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RepidRide.Api.Models;
using RepidRide.Core.Entities;
using RepidRide.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RepidRide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, IUserRepository userRepository, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt rejected: missing email or password.");
                return BadRequest("Email and Password are required.");
            }

            try
            {
                _logger.LogInformation("Processing login request for: {Email}", request.Email);
                var user = await _userRepository.AuthenticateUserAsync(request.Email.Trim(), request.Password);

                if (user == null)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                    return Unauthorized("Invalid email or password.");
                }

                var tokenString = GenerateJwtToken(user);
                _logger.LogInformation("Login successful for user {UserId} with role {Role}", user.Id, user.Role);

                return Ok(new
                {
                    Token = tokenString,
                    Role = user.Role,
                    UserId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login for email: {Email}", request.Email);
                return StatusCode(500, "An internal server error occurred during authentication.");
            }
        }

        private string GenerateJwtToken(UserDto user)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var expirationDays = Convert.ToInt32(_configuration["JwtSettings:ExpirationInDays"] ?? "7");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expirationDays),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}