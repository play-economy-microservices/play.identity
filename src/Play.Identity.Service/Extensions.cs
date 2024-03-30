using Play.Identity.Service.Dtos;
using Play.Identity.Service.Entities;

namespace Play.Identity.Service;

public static class Extensions
{
    /// <summary>
    /// Include only the properties we want to return back. Note, ApplicationUsr
    /// returns an object with many properties.
    /// </summary>
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
