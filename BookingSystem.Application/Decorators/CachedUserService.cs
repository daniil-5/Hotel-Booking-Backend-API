using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Application.Decorators;

public class CachedUserService : IUserService
{
    private readonly IUserService _userService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedUserService> _logger;
    
    private const string USER_BY_ID_KEY = "user:id:{0}";
    private const string USER_BY_EMAIL_KEY = "user:email:{0}";
    private const string USER_BY_USERNAME_KEY = "user:username:{0}";
    private const string ALL_USERS_KEY = "users:all";
    private const string USER_SEARCH_KEY = "users:search:{0}";
    private const string USER_PREFIX = "user:";
    
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SearchCacheExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AllUsersCacheExpiration = TimeSpan.FromMinutes(15);

    public CachedUserService(
        IUserService userService, 
        ICacheService cacheService,
        ILogger<CachedUserService> logger)
    {
        _userService = userService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto userDto)
    {
        _logger.LogInformation("Creating new user with email: {Email}", userDto.Email);
        
        var newUser = await _userService.CreateUserAsync(userDto);
        
        await CacheUser(newUser);
        
        await InvalidateListCaches();
        
        _logger.LogInformation("User created and cached with ID: {UserId}", newUser.Id);
        return newUser;
    }

    public async Task<UserDto> UpdateUserAsync(UpdateUserDto userDto)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", userDto.Id);
        
        var currentUser = await GetUserByIdAsync(userDto.Id);
        if (currentUser == null)
            throw new ApplicationException($"User with ID {userDto.Id} not found");
        
        var updatedUser = await _userService.UpdateUserAsync(userDto);
        
        if (currentUser.Email != updatedUser.Email)
        {
            await _cacheService.RemoveAsync(string.Format(USER_BY_EMAIL_KEY, currentUser.Email));
        }
        
        if (currentUser.Username != updatedUser.Username)
        {
            await _cacheService.RemoveAsync(string.Format(USER_BY_USERNAME_KEY, currentUser.Username));
        }
        
        await CacheUser(updatedUser);
        await InvalidateListCaches();
        
        _logger.LogInformation("User updated and cache refreshed for ID: {UserId}", updatedUser.Id);
        return updatedUser;
    }

    public async Task DeleteUserAsync(int id)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);
        
        var userToDelete = await GetUserByIdAsync(id);
        
        await _userService.DeleteUserAsync(id);
        
        if (userToDelete != null)
        {
            await _cacheService.RemoveAsync(string.Format(USER_BY_ID_KEY, id));
            await _cacheService.RemoveAsync(string.Format(USER_BY_EMAIL_KEY, userToDelete.Email));
            await _cacheService.RemoveAsync(string.Format(USER_BY_USERNAME_KEY, userToDelete.Username));
        }
        
        await InvalidateListCaches();
        
        _logger.LogInformation("User deleted and cache invalidated for ID: {UserId}", id);
    }

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var cacheKey = string.Format(USER_BY_ID_KEY, id);
        
        var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cachedUser != null)
        {
            _logger.LogDebug("User found in cache for ID: {UserId}", id);
            return cachedUser;
        }
        
        _logger.LogDebug("User not found in cache, fetching from database for ID: {UserId}", id);
        
        var user = await _userService.GetUserByIdAsync(id);
        
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
        
        var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cachedUser != null)
        {
            _logger.LogDebug("User found in cache for email: {Email}", email);
            return cachedUser;
        }
        
        _logger.LogDebug("User not found in cache, fetching from database for email: {Email}", email);
        
        var user = await _userService.GetUserByEmailAsync(email);
        
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
        
        var cachedUser = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cachedUser != null)
        {
            _logger.LogDebug("User found in cache for username: {Username}", username);
            return cachedUser;
        }
        
        _logger.LogDebug("User not found in cache, fetching from database for username: {Username}", username);
        
        var user = await _userService.GetUserByUsernameAsync(username);
        
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
        
        var cachedUsers = await _cacheService.GetAsync<IEnumerable<UserDto>>(cacheKey);
        if (cachedUsers != null)
        {
            _logger.LogDebug("All users found in cache");
            return cachedUsers;
        }
        
        _logger.LogDebug("All users not found in cache, fetching from database");
        
        var users = await _userService.GetAllUsersAsync();
        
        await _cacheService.SetAsync(cacheKey, users, AllUsersCacheExpiration);
        
        foreach (var user in users)
        {
            await CacheUser(user);
        }
        
        _logger.LogDebug("All users cached, count: {UserCount}", users.Count());
        return users;
    }

    public async Task<UserSearchResultDto> SearchUsersAsync(UserSearchDto searchDto)
    {
        var searchKey = GenerateSearchCacheKey(searchDto);
        var cacheKey = string.Format(USER_SEARCH_KEY, searchKey);
        
        var cachedResult = await _cacheService.GetAsync<UserSearchResultDto>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug("Search result found in cache for key: {SearchKey}", searchKey);
            return cachedResult;
        }
        
        _logger.LogDebug("Search result not found in cache, executing search for key: {SearchKey}", searchKey);
        
        var searchResult = await _userService.SearchUsersAsync(searchDto);
        
        await _cacheService.SetAsync(cacheKey, searchResult, SearchCacheExpiration);
        
        foreach (var user in searchResult.Users)
        {
            await CacheUser(user);
        }
        
        _logger.LogDebug("Search result cached for key: {SearchKey}, count: {ResultCount}", 
            searchKey, searchResult.Users.Count());
        
        return searchResult;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        _logger.LogInformation("Changing password for user ID: {UserId}", changePasswordDto.UserId);
        
        var result = await _userService.ChangePasswordAsync(changePasswordDto);
        
        if (result)
        {
            var updatedUser = await _userService.GetUserByIdAsync(changePasswordDto.UserId);
            if (updatedUser != null)
            {
                await CacheUser(updatedUser);
            }
            
            _logger.LogInformation("Password changed and cache updated for user ID: {UserId}", 
                changePasswordDto.UserId);
        }
        
        return result;
    }

    public async Task<bool> VerifyUserPasswordAsync(string email, string password)
    {
        return await _userService.VerifyUserPasswordAsync(email, password);
    }
    
    
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
    
    public async Task InvalidateAllUserCachesAsync()
    {
        await _cacheService.RemoveByPrefixAsync(USER_PREFIX);
        _logger.LogInformation("All user caches invalidated");
    }
}
