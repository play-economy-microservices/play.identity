namespace Play.Identity.Service.Settings;

public class IdentitySettings
{
	/// <summary>
	/// The email that is allowed as an admin.
	/// </summary>
	public string AdminUserEmail { get; init; }

	/// <summary>
	/// The password for the admin email that is kept as a secret.
	/// </summary>
	public string AdminUserPassword { get; init; }

	/// <summary>
	/// The Starting Gil
	/// </summary>
	public decimal StartingGil { get; init; }
}
