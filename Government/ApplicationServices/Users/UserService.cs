using Mapster;
using SurvayBasket.Abstractions.Consts.cs;
using SurvayBasket.Contracts.User.cs;
using SurvayBasket.Errors;
using SurvayBasket.UsreErrors;

namespace Government.ApplicationServices.services
{
    public class UserService(UserManager<AppUser> userManager , AppDbContext context ) : IUserService
    {
        private readonly UserManager<AppUser> userManager = userManager;
        private readonly AppDbContext context = context;

     

        public async Task<Result<IEnumerable<UserResponse>>> GetAllAsync (CancellationToken cancellationToken = default)
        {
            var users = await (from u in context.Users
                               join ur in context.UserRoles
                               on u.Id equals ur.UserId
                               join r in context.Roles
                               on ur.RoleId equals r.Id into roles
                               select new
                               {
                                   u.Id,
                                   u.FirstName,
                                   u.LastName,
                                   u.Email,
                                   Roles = roles.Select(x => x.Name)

                               }

                              ).GroupBy(x => new { x.Id, x.FirstName, x.LastName, x.Email })
                              .Select(x => new UserResponse(
                                  x.Key.Id,
                                  x.Key.FirstName,
                                  x.Key.LastName,
                                  x.Key.Email,
                                  x.SelectMany(x => x.Roles).ToList()

                                  )).ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<UserResponse>>(users);
        }

        public async Task<Result<UserResponse>> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Result.Falire<UserResponse>(UsersErrors.NotFound);

            var Roles = await userManager.GetRolesAsync(user);

            var response = new UserResponse(user.Id, user.FirstName, user.LastName, user.Email!, Roles);

            return Result.Success(response);
        }


        public async Task<Result<UserResponse>> AddAsync(UserRequest request, CancellationToken cancellationToken = default)
        {
            var EmailIsExist = await context.Users.AnyAsync(x => x.Email == request.Email);

            if(EmailIsExist) 
                return Result.Falire<UserResponse>(UsersErrors.DublicatedEmail);

            var PhoneNumIsExist = await context.Users.AnyAsync(x => x.PhoneNumber == request.PhoneNumber);

            if (EmailIsExist)
                return Result.Falire<UserResponse>(UsersErrors.DublicatedPhoneNumber);

            var CurrentRoles = await context.Roles.Select(x => x.Name!).AsNoTracking().ToListAsync(cancellationToken);

            if (request.Roles.Except(CurrentRoles).Any())
                return Result.Falire<UserResponse>(RoleErrors.InvalidRoles);

            var NewUser = request.Adapt<AppUser>();
         
           var result=  await userManager.CreateAsync(NewUser, request.Password);

            if (result.Succeeded)
            {
               var response=    await userManager.AddToRolesAsync(NewUser, request.Roles);

               return Result.Success(new UserResponse(NewUser.Id , NewUser.FirstName , NewUser.LastName , NewUser.Email! ,request.Roles ));
            }

            var error = result.Errors.First();

            return Result.Falire<UserResponse>(new Error(error.Code, error.Description));
            
           
        }

        public async Task<Result> UpdateAsync(string id, UserUpdate request, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Result.Falire(UsersErrors.NotFound);


            var EmailIsExist = await context.Users.AnyAsync(x => x.Email == request.Email && x.Id != id);

            if (EmailIsExist)
                return Result.Falire<UserResponse>(UsersErrors.DublicatedEmail);

            var CurrentRoles = await context.Roles.Select(x => x.Name!).AsNoTracking().ToListAsync(cancellationToken);

            if (request.Roles.Except(CurrentRoles).Any())
                return Result.Falire<UserResponse>(RoleErrors.InvalidRoles);


            user = request.Adapt(user);

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var x = await context.UserRoles
                                .Where(x => x.UserId == id)
                                .ExecuteDeleteAsync();

                var response = await userManager.AddToRolesAsync(user, request.Roles);

                return Result.Success();
              
            }

            var error = result.Errors.First();

            return Result.Falire<UserResponse>(new Error(error.Code, error.Description));



        }

        public async Task<Result> ToggleAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
                return Result.Falire(UsersErrors.NotFound);

            user.IsDeleted = !user.IsDeleted;

            await context.SaveChangesAsync();

            return Result.Success();

        }

        //public async Task<Result> UnLockAsync(string id, CancellationToken cancellationToken = default)
        //{
        //    var user = await userManager.FindByIdAsync(id);
        //    if (user is null)
        //        return Result.Falire(UsersErrors.UserNotFound);

        //    await userManager.SetLockoutEndDateAsync(user, null); // ========= ( user.LockoutEnd = null;  ,    await context.SaveChangesAsync(); )

        //    return Result.Success();

        //}
    }
}
