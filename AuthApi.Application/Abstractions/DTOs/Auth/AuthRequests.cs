namespace AuthApi.Application.Abstractions.DTOs.Auth;

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record RegisterRequest(
    string Email,
    string FirstName,
    string LastName,
    string UserName,
    string PhoneNumber,
    DateOnly DateOfBirth,
    string Password);

public sealed record ResetPasswordRequest(
    string Email,
    string Otp,
    string NewPass);
