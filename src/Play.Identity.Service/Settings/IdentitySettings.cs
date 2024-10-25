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

	/// <summary>
	/// The path base for routing to this service
	/// </summary>
	public string PathBase { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public string CertificateCerFilePath { get; init; }

	public string CertificateKeyFilePath { get; init; }
}
