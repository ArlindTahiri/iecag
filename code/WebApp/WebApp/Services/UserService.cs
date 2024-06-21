using Azure;
using Azure.Data.Tables;
using Microsoft.ApplicationInsights;
using WebApp.HelperClasses;
using WebApp.Models.Entities;

namespace WebApp.Services
{
    public class UserService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<UserService> _logger;
        private readonly TelemetryClient _telemetryClient;

        public UserService(string connectionString, ILogger<UserService> logger, TelemetryClient telemetryClient)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("users");
            _tableClient.CreateIfNotExists();
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        public async Task CreateUserAsync(string userId, string password)
        {
            _logger.LogInformation("Creating user with ID {UserId}", userId);
            _telemetryClient.TrackEvent("CreateUser", new Dictionary<string, string> { { "UserId", userId } });

            var (hash, salt) = PasswordHelper.HashPassword(password);

            var user = new User
            {
                PartitionKey = userId,
                RowKey = "user",
                PasswordHash = hash,
                PasswordSalt = salt,
                Timestamp = DateTimeOffset.UtcNow,
                ETag = ETag.All
            };

            try
            {
                await _tableClient.AddEntityAsync(user);
                _logger.LogInformation("User with ID {UserId} created successfully", userId);
                _telemetryClient.TrackEvent("UserCreated", new Dictionary<string, string> { { "UserId", userId } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with ID {UserId}", userId);
                _telemetryClient.TrackException(ex);
                throw;
            }
        }

        public async Task<bool> ValidateUserAsync(string userId, string enteredPassword)
        {
            _logger.LogInformation("Validating user with ID {UserId}", userId);
            _telemetryClient.TrackEvent("ValidateUser", new Dictionary<string, string> { { "UserId", userId } });

            try
            {
                var entity = await _tableClient.GetEntityIfExistsAsync<User>(userId, "user");
                if (entity.HasValue)
                {
                    var user = entity.Value;
                    bool isValid = PasswordHelper.VerifyPassword(enteredPassword, user.PasswordHash, user.PasswordSalt);
                    _logger.LogInformation("Validation for user ID {UserId} returned {IsValid}", userId, isValid);
                    _telemetryClient.TrackEvent("UserValidated", new Dictionary<string, string> { { "UserId", userId }, { "IsValid", isValid.ToString() } });
                    return isValid;
                }
                else
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    _telemetryClient.TrackEvent("UserNotFound", new Dictionary<string, string> { { "UserId", userId } });
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user with ID {UserId}", userId);
                _telemetryClient.TrackException(ex);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            _logger.LogInformation("Checking if user with ID {UserId} exists", userId);
            _telemetryClient.TrackEvent("UserExists", new Dictionary<string, string> { { "UserId", userId } });

            try
            {
                var entity = await _tableClient.GetEntityAsync<User>(userId, "user");
                bool exists = entity.HasValue;
                _logger.LogInformation("User with ID {UserId} exists: {Exists}", userId, exists);
                _telemetryClient.TrackEvent("UserExistsResult", new Dictionary<string, string> { { "UserId", userId }, { "Exists", exists.ToString() } });
                return exists;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                _telemetryClient.TrackEvent("UserNotFound", new Dictionary<string, string> { { "UserId", userId } });
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of user with ID {UserId}", userId);
                _telemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}
