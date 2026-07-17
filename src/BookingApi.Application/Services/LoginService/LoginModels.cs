namespace BookingApi.Application.Services.LoginService;

public record LoginResponseModel(string Token, string Email, string UserName);

public record LoginRequestModel(string Email, string Password);
