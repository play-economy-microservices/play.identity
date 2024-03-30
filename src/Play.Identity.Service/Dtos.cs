using System;
using System.ComponentModel.DataAnnotations;

namespace Play.Identity.Service.Dtos;

/// <summary>
/// Specify all Dtos that will be used for Postman here.
/// </summary>

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    decimal Gil,
    DateTimeOffset CreatedDate
);

public record UpdateUserDto(
    [Required][EmailAddress] string Email,
    [Range(0, 1000000)] decimal Gil
);