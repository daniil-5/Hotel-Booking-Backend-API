using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Exceptions;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Application.Decorators;

public class CachedUserService : IUserService
{
    private readonly IUserService _userService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedUserService> _logger;
    private readonly ICacheInvalidationPublisher _publisher;

    // Cache key constants for better maintainability
    private const string USER_BY_ID_KEY = "user:id:{0}";
    private const string USER_BY_EMAIL_KEY = "user:email:{0}";
    private const string USER_BY_USERNAME_KEY = "user:username:{0}";
    private const string ALL_USERS_KEY = "users:all";
    private const string USER_SEARCH_KEY = "users:search:{0}";
    private const string USER_PREFIX = "user:";

    // Cache expiration times
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SearchCacheExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan BlacklistUserCacheExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AllUsersCacheExpiration = TimeSpan.FromMinutes(15);

    public CachedUserService(
        IUserService userService,
        ICacheService cacheService,
        ILogger<CachedUserService> logger,
        ICacheInvalidationPublisher publisher)
    {
        _userService = userService;
        _cacheService = cacheService;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto userDto)
    {
        _logger.LogInformation("Creating new user with email: {Email}", userDto.Email);

        // Create user through the underlying service
        var newUser = await _userService.CreateUserAsync(userDto);

        // Cache the newly created user
        await CacheUser(newUser);

        // Invalidate related caches since we have a new user
        await InvalidateListCaches();

        await _publisher.PublishAsync("user", newUser.Id.ToString());

        _logger.LogInformation("User created and cached with ID: {UserId}", newUser.Id);
        return newUser;
    }

    public async Task<UserDto> UpdateUserAsync(UpdateUserDto userDto)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", userDto.Id);

        // Get the current user to check what's changing
        var currentUser = await GetUserByIdAsync(userDto.Id);
        if (currentUser == null)
            throw new ApplicationException($"User with ID {userDto.Id} not found");

        // Update user through the underlying service
        var updatedUser = await _userService.UpdateUserAsync(userDto);

        // Invalidate old cache entries if email or username changed
        if (currentUser.Email != updatedUser.Email)
        {
            await _cacheService.RemoveAsync(string.Format(USER_BY_EMAIL_KEY, currentUser.Email));
        }

        if (currentUser.Username != updatedUser.Username)
        {
            await _cacheService.RemoveAsync(string.Format(USER_BY_USERNAME_KEY, currentUser.Username));
        }

        // Cache the updated user with new values
        await CacheUser(updatedUser);

        // Invalidate list caches since user data changed
        await InvalidateListCaches();

        await _publisher.PublishAsync("user", userDto.Id.ToString());

        _logger.LogInformation("User updated and cache refreshed for ID: {UserId}", updatedUser.Id);
        return updatedUser;
    }

    public async Task DeleteUserAsync(int id)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);

        // Get user details before deletion for cache invalidation
        var userToDelete = await GetUserByIdAsync(id);

        // Delete user through the underlying service
        await _userService.DeleteUserAsync(id);

        // Remove all cache entries for this user
        if (userToDelete != null)
        {
            await _cacheService.RemoveAsync(string.Format(USER_BY_ID_KEY, id));
            await _cacheService.RemoveAsync(string.Format(USER_BY_EMAIL_KEY, userToDelete.Email));
            await _cacheService.RemoveAsync(string.Format(USER_BY_USERNAME_KEY, userToDelete.Username));
        }

        // Invalidate list caches since user was deleted
        await InvalidateListCaches();

        await _publisher.PublishAsync("user", id.ToString());

        _logger.LogInformation("User deleted and cache invalidated for ID: {UserId}", id);
    }

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var cacheKey = string.Format(USER_BY_ID_KEY, id);

        // Try to get from cache first
        var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cachedUser != null)
        {
            _logger.LogDebug("User found in cache for ID: {UserId}", id);
            return cachedUser;
        }

        _logger.LogDebug("User not found in cache, fetching from database for ID: {UserId}", id);

        // Get from underlying service if not in cache
        var user = await _userService.GetUserByIdAsync(id);

        // Cache the result if found
        if (user != null)
        {
            await CacheUser(user);
            _logger.LogDebug("User cached for ID: {UserId}", id);
        }

        return user;
    }

    public async Task<UserDto> GetUserByEmailAsync(string email)
    {
        var cacheKey = string.Format(USER_BY_EMAIL_KEY, email.ToLower());

        // Try to get from cache first
        var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cachedUser != null)
        {
            _logger.LogDebug("User found in cache for email: {Email}", email);
            return cachedUser;
        }

        _logger.LogDebug("User not found in cache, fetching from database for email: {Email}", email);

        // Get from underlying service if not in cache
        var user = await _userService.GetUserByEmailAsync(email);

        // Cache the result if found
        if (user != null)
        {
            await CacheUser(user);
            _logger.LogDebug("User cached for email: {Email}", email);
        }

        return user;
    }

    public async Task<UserDto> GetUserByUsernameAsync(string username)
    {
        var cacheKey = string.Format(USER_BY_USERNAME_KEY, username.ToLower());

        // Try to get from cache first
        var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cachedUser != null)
        {
            _logger.LogDebug("User found in cache for username: {Username}", username);
            return cachedUser;
        }

        _logger.LogDebug("User not found in cache, fetching from database for username: {Username}", username);

        // Get from underlying service if not in cache
        var user = await _userService.GetUserByUsernameAsync(username);

        // Cache the result if found
        if (user != null)
        {
            await CacheUser(user);
            _logger.LogDebug("User cached for username: {Username}", username);
        }

        return user;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var cacheKey = ALL_USERS_KEY;

        // Try to get from cache first
        var cachedUsers = await _cacheService.GetAsync<IEnumerable<UserDto>>(cacheKey);
        if (cachedUsers != null)
        {
            _logger.LogDebug("All users found in cache");
            return cachedUsers;
        }

        _logger.LogDebug("All users not found in cache, fetching from database");

        // Get from underlying service if not in cache
        var users = await _userService.GetAllUsersAsync();

        // Cache the result
        await _cacheService.SetAsync(cacheKey, users, AllUsersCacheExpiration);

        // Also cache individual users for better hit rate
        foreach (var user in users)
        {
            await CacheUser(user);
        }

        _logger.LogDebug("All users cached, count: {UserCount}", users.Count());
        return users;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        _logger.LogInformation("Changing password for user ID: {UserId}", changePasswordDto.UserId);

        // Change password through the underlying service
        var result = await _userService.ChangePasswordAsync(changePasswordDto);

        if (result)
        {
            // Get updated user and refresh cache
            var updatedUser = await _userService.GetUserByIdAsync(changePasswordDto.UserId);
            if (updatedUser != null)
            {
                await CacheUser(updatedUser);
                await _publisher.PublishAsync("user", updatedUser.Id.ToString());
            }

            _logger.LogInformation("Password changed and cache updated for user ID: {UserId}",
                changePasswordDto.UserId);
        }

        return result;
    }

    public async Task<bool> VerifyUserPasswordAsync(string email, string password)
    {
        var userBlacklistKey = $"auth:blacklist:{email}";
        var userAttemptsKey = $"auth:attempts:{email}";

        if (await _cacheService.ExistsAsync(userBlacklistKey))
        {
            _logger.LogDebug("User with email {email} was found in cache", email);

            var ttl = await _cacheService.GetTtlAsync(userBlacklistKey);
            var minutesLeft = ttl.HasValue ? Math.Ceiling(ttl.Value.TotalMinutes) : BlacklistUserCacheExpiration.Minutes;

            throw new AccountBannedException($"Too many failed attempts. Try again in {minutesLeft} minutes.");
        }

        if (await _userService.VerifyUserPasswordAsync(email, password))
        {
            await _cacheService.RemoveAsync(userBlacklistKey);
            return true;
        }

        var counter = await _cacheService.IncrementAsync(userAttemptsKey, BlacklistUserCacheExpiration);
        if (counter >= 3)
        {
            await _cacheService.SetAsync(userBlacklistKey, "blocked", BlacklistUserCacheExpiration);
            await _cacheService.RemoveAsync(userAttemptsKey);
        }

        return false;
    }

    public async Task<IEnumerable<UserDto>> GetActiveUsersOrderedByRegistrationDateAsync()
    {
        const string cacheKey = "users:active:ordered";
        var cachedUsers = await _cacheService.GetAsync<IEnumerable<UserDto>>(cacheKey);
        if (cachedUsers != null)
        {
            _logger.LogDebug("Active users ordered by registration date found in cache");
            return cachedUsers;
        }

        _logger.LogDebug("Active users ordered by registration date not found in cache, fetching from database");
        var users = await _userService.GetActiveUsersOrderedByRegistrationDateAsync();
        await _cacheService.SetAsync(cacheKey, users, AllUsersCacheExpiration);
        _logger.LogDebug("Active users ordered by registration date cached");
        return users;
    }

    public async Task<IEnumerable<UserDto>> GetUsersWithNoBookingsAsync()
    {
        const string cacheKey = "users:nobookings";
        var cachedUsers = await _cacheService.GetAsync<IEnumerable<UserDto>>(cacheKey);
        if (cachedUsers != null)
        {
            _logger.LogDebug("Users with no bookings found in cache");
            return cachedUsers;
        }

        _logger.LogDebug("Users with no bookings not found in cache, fetching from database");
        var users = await _userService.GetUsersWithNoBookingsAsync();
        await _cacheService.SetAsync(cacheKey, users, AllUsersCacheExpiration);
        _logger.LogDebug("Users with no bookings cached");
        return users;
    }

    #region Private Helper Methods

    /// <summary>
    /// Caches a user with all possible cache keys (by ID, email, username)
    /// </summary>
    private async Task CacheUser(UserDto user)
    {
        if (user == null) return;

        var tasks = new List<Task>
        {
            _cacheService.SetAsync(string.Format(USER_BY_ID_KEY, user.Id), user, DefaultCacheExpiration),
            _cacheService.SetAsync(string.Format(USER_BY_EMAIL_KEY, user.Email.ToLower()), user, DefaultCacheExpiration),
            _cacheService.SetAsync(string.Format(USER_BY_USERNAME_KEY, user.Username.ToLower()), user, DefaultCacheExpiration)
        };

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Invalidates all list-based caches (all users, search results)
    /// </summary>
    private async Task InvalidateListCaches()
    {
        var tasks = new List<Task>
        {
            _cacheService.RemoveAsync(ALL_USERS_KEY),
            _cacheService.RemoveByPrefixAsync("users:search:")
        };

        await Task.WhenAll(tasks);

        _logger.LogDebug("List caches invalidated");
    }

    /// <summary>
    /// Generates a consistent cache key for search parameters
    /// </summary>
    private static string GenerateSearchCacheKey(UserSearchDto searchDto)
    {
        var keyParts = new List<string>
        {
            $"term:{searchDto.SearchTerm ?? "null"}",
            $"role:{searchDto.Role?.ToString() ?? "null"}",
            $"sort:{searchDto.SortBy ?? "createdat"}",
            $"desc:{searchDto.SortDescending}",
            $"page:{searchDto.PageNumber}",
            $"size:{searchDto.PageSize}"
        };

        return string.Join("|", keyParts).ToLower();
    }

    /// <summary>
    /// Invalidates all user-related caches (useful for bulk operations)
    /// </summary>
    public async Task InvalidateAllUserCachesAsync()
    {
        await _cacheService.RemoveByPrefixAsync(USER_PREFIX);
        _logger.LogInformation("All user caches invalidated");
    }

    #endregion
}
