using AutoMapper;
using Serilog;
using Wellmeet.Core.Filters;
using Wellmeet.Data;
using Wellmeet.DTO;
using Wellmeet.Exceptions;
using Wellmeet.Models;
using Wellmeet.Repositories;
using Wellmeet.Repositories.Interfaces;
using Wellmeet.Security;


namespace Wellmeet.Services.Interfaces
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;
        private readonly ILogger<UserService> _logger = new LoggerFactory().AddSerilog().CreateLogger<UserService>();
           

        public UserService(IUnitOfWork uow, IMapper mapper, IJwtService jwtService)
        {
            _uow = uow;
            _mapper = mapper;
            _jwtService = jwtService;
           
        }

        //VerifyAndGetUser ----------------------------------------
        public async Task<User> VerifyAndGetUserAsync(UserLoginDTO dto)
        {
            User? user = null;
            try
            {   
                user = await _uow.UserRepository.GetUserByUsernameAsync(dto.Username!);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: Username {Username} not found", dto.Username);
                    throw new EntityNotAuthorizedException("User", "Invalid username or password.");
                }

                if (!EncryptionUtil.IsValidPassword(dto.Password!, user.Password))
                {
                    _logger.LogWarning("Login failed: Wrong password for {Username}", dto.Username);
                    throw new EntityNotAuthorizedException("User", "Invalid username or password.");
                }

                _logger.LogInformation("User {Username} authenticated successfully", dto.Username);

               
            }
            catch (EntityNotAuthorizedException ex)
            {
                _logger.LogWarning(  ex, "Authentication failed for username {Username}", dto.Username
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected error while verifying credentials for {Username}", dto.Username
                );
                throw new ServerException("Server", "Unexpected authentication error.");
            }

            return user;   
        }

        // LOGIN -----------------------------------------------
        public async Task<JwtTokenDTO> LoginAsync(UserLoginDTO dto)
        {
            var user = await VerifyAndGetUserAsync(dto);

            var token = _jwtService.CreateToken(
                user.Id,
                user.Username,
                user.Email,
                user.UserRole
            );

            return new JwtTokenDTO
            {
                Token = token,
                Username = user.Username,
                Role = user.UserRole.ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(4)
            };
        }

        // REGISTER -------------------------------------------
        public async Task<UserReadOnlyDTO> RegisterAsync(UserRegisterDTO dto)
        {
            if (dto is null)
            {
                throw new InvalidArgumentException("User", "User registration data cannot be null.");
            }

            try
            {
                // Check username exists
                var existingUserByUsername = await _uow.UserRepository.GetUserByUsernameAsync(dto.Username!);
                if (existingUserByUsername != null)
                {
                    _logger.LogWarning("Registration failed: Username {Username} already exists", dto.Username);
                    throw new EntityAlreadyExistsException("User", $"Username '{dto.Username}' already exists.");
                }
                // Check email exists
                var existingUserByEmail = await _uow.UserRepository.GetUserByEmailAsync(dto.Email!);
                if (existingUserByEmail != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", dto.Email);
                    throw new EntityAlreadyExistsException("User", $"Email '{dto.Email}' already exists.");
                }
                //map
                var newUser = _mapper.Map<User>(dto);

                // Hash password
                newUser.Password = EncryptionUtil.Encrypt(dto.Password!);

                // Ensure default role (if needed)
                //newUser.UserRole = UserRole.User;

                newUser.InsertedAt = DateTime.UtcNow;

                await _uow.UserRepository.AddAsync(newUser);
                await _uow.SaveAsync();

                _logger.LogInformation("New user created with username: {Username}", newUser.Username);

                return _mapper.Map<UserReadOnlyDTO>(newUser);
            }
            catch (EntityAlreadyExistsException)

            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user registration for {Username}", dto.Username);
                throw new ServerException("Server", "An unexpected error occurred during registration.");
            }
        }
       

        //GET BY ID-------------------------------------------
        public async Task<UserReadOnlyDTO> GetByIdAsync(int id)
        {
            User? user = null;

            try
            {
                user = await _uow.UserRepository.GetAsync(id);
                if (user == null)
                {
                    throw new EntityNotFoundException("User", "User with id: " + id + " not found");
                }

                _logger.LogInformation("User found with ID: {Id}", id);
                return _mapper.Map<UserReadOnlyDTO>(user);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogError("Error retrieving user by ID: {Id}. {Message}", id, ex.Message);
                throw;
            }
        }

        // UPDATE ----------------------------------------------
        public async Task<UserReadOnlyDTO> UpdateAsync(int id, UserUpdateDTO dto)
        {
            try
            {
                var user = await _uow.UserRepository.GetAsync(id)
                    ?? throw new EntityNotFoundException("User", "User not found.");

                _mapper.Map(dto, user);
                user.ModifiedAt = DateTime.UtcNow;

                await _uow.UserRepository.UpdateAsync(user);
                await _uow.SaveAsync();

                return _mapper.Map<UserReadOnlyDTO>(user);
            }
            catch (EntityAlreadyExistsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user {Id}", id);
                throw new ServerException("Server", "Unexpected error updating user.");
            }
        }


        //  DELETE ----------------------------------------------
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var deleted = await _uow.UserRepository.DeleteAsync(id);

                if (!deleted)
                    throw new EntityNotFoundException("User", "User not found.");

                await _uow.SaveAsync();
                return true;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting user {Id}", id);
                throw new ServerException("Server", "Unexpected error deleting user.");
            }
        }

        // PAGINATION ----------------------------------------------
        public async Task<PaginatedResult<UserReadOnlyDTO>> GetPaginatedAsync(
            int pageNumber, int pageSize, UserFiltersDTO filters)
        {
            var predicates = PredicateBuilder.BuildUserPredicates(filters);

            var result = await _uow.UserRepository.GetUsersAsync(pageNumber, pageSize, predicates);

            return new PaginatedResult<UserReadOnlyDTO>
            {
                Data = _mapper.Map<List<UserReadOnlyDTO>>(result.Data),
                TotalRecords = result.TotalRecords,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }
      
    }
}
