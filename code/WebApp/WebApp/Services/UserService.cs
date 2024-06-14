using Azure;
using Azure.Data.Tables;
using WebApp.HelperClasses;
using WebApp.Models.Entities;

namespace WebApp.Services
{

    public class UserService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<UserService> _logger;

        public UserService(string connectionString, ILogger<UserService> logger)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("users");
            _tableClient.CreateIfNotExists();
            _logger = logger;
        }

        public async Task CreateUserAsync(string userId, string password)
        {
            _logger.LogInformation("Creating user with ID {UserId}", userId);

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with ID {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateUserAsync(string userId, string enteredPassword)
        {
            _logger.LogInformation("Validating user with ID {UserId}", userId);

            try
            {
                var entity = await _tableClient.GetEntityIfExistsAsync<User>(userId, "user");
                if (entity.HasValue)
                {
                    var user = entity.Value;
                    bool isValid = PasswordHelper.VerifyPassword(enteredPassword, user.PasswordHash, user.PasswordSalt);
                    _logger.LogInformation("Validation for user ID {UserId} returned {IsValid}", userId, isValid);
                    return isValid;
                }
                else
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user with ID {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            _logger.LogInformation("Checking if user with ID {UserId} exists", userId);

            try
            {
                var entity = await _tableClient.GetEntityAsync<User>(userId, "user");
                bool exists = entity.HasValue;
                _logger.LogInformation("User with ID {UserId} exists: {Exists}", userId, exists);
                return exists;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of user with ID {UserId}", userId);
                throw;
            }
        }
    }

}
