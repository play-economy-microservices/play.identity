using Play.Identity.Service.Dtos;
using Play.Identity.Service.Entities;

namespace Play.Identity.Service
{
    public static class Extensions
    {
        public static UserDto AsDto(this ApplicationUser user)
        {
            return new UserDto(
                user.Id, 
                user.UserName, 
                user.Email, 
                user.Gil, 
                user.CreatedOn);
        }
    }
}