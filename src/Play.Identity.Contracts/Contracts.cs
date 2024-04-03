namespace Play.Identity.Contracts;

using System;

/// <summary>
/// Event that Debits Gil accordingly to the the user
/// </summary>
public record DebitGil(
    Guid UserId,
    decimal Gil,
    Guid CorrelationId);

/// <summary>
/// Event response for debitted gil.
/// </summary>
public record GilDebited(Guid CorrelationId);

/// <summary>
/// Event for when the User has been updated.
/// </summary>
public record UserUpdated(Guid UserId, string email, decimal NewTotalGil);
