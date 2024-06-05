using Azure;
using Azure.Data.Tables;
using WebApp.HelperClasses;
using WebApp.Models.Entities;

namespace WebApp.Services
{

    public class UserService
    {
        private readonly TableClient _tableClient;

        public UserService(string connectionString)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("users");
            _tableClient.CreateIfNotExists();
        }

        public async Task CreateUserAsync(string userId, string password)
        {
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

            await _tableClient.AddEntityAsync(user);
        }

        public async Task<bool> ValidateUserAsync(string userId, string enteredPassword)
        {
            var entity = await _tableClient.GetEntityIfExistsAsync<User>(userId, "user");
            if (entity.HasValue)
            {
                var user = entity.Value;
                return PasswordHelper.VerifyPassword(enteredPassword, user.PasswordHash, user.PasswordSalt);
            }
            return false;
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            try
            {
                var entity = await _tableClient.GetEntityAsync<User>(userId, "user");
                return entity.HasValue;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Handle the case where the user is not found
                return false;
            }
        }
    }

}
