using System.ComponentModel.DataAnnotations;

namespace IRISA.Backend.Dtos;

/// <summary>Credentials used to sign in to the application.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    [Required, StringLength(100)]
    public required string Password { get; init; }
}
