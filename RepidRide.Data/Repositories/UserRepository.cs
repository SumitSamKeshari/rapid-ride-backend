using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RepidRide.Core.Entities;
using RepidRide.Core.Interfaces;
using System;
using System.Data;
using System.Threading.Tasks;

namespace RepidRide.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<UserRepository>? _logger;

        public UserRepository(string connectionString, ILogger<UserRepository>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public async Task<UserDto?> AuthenticateUserAsync(string email, string password)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_connectionString);
                var parameters = new { Email = email, Password = password };

                var user = await connection.QueryFirstOrDefaultAsync<UserDto>(
                    "sp_AuthenticateUser",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                if (user != null)
                {
                    _logger?.LogInformation("User authenticated successfully: {Email} (Role: {Role})", email, user.Role);
                }
                else
                {
                    _logger?.LogWarning("Authentication failed for email: {Email}", email);
                }

                return user;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL Exception while authenticating user {Email}", email);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected Exception while authenticating user {Email}", email);
                throw;
            }
        }
    }
}