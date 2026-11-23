using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.Settings;

namespace MicroNamaa.Api.Extensions;

public static class AuthenticationExtensions
{
    private const string MerchantBlocked = "The merchant is suspended or its subscription ended.";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,

                ValidAudience = jwtSettings.Audience,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromMinutes(1),

                NameClaimType = "name",

                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },

                // A suspended merchant, or one whose subscription ended, is locked out at once (cached for a few seconds).
                OnTokenValidated = async context =>
                {
                    var merchantClaim = context.Principal?.FindFirst(AppClaimTypes.MerchantId)?.Value;

                    if (!int.TryParse(merchantClaim, out var merchantId))
                    {
                        return;
                    }

                    var merchantAccess = context.HttpContext.RequestServices.GetRequiredService<IMerchantAccessService>();

                    if (!await merchantAccess.IsAllowedAsync(merchantId, context.HttpContext.RequestAborted))
                    {
                        context.Fail(MerchantBlocked);
                    }
                },

                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    var message = context.AuthenticateFailure?.Message == MerchantBlocked ? IMerchantAccessService.BlockedMessage : null;

                    await context.Response.WriteResultAsync(Result.Unauthorized(message), context.HttpContext.RequestAborted);
                },

                OnForbidden = context => context.Response.WriteResultAsync(Result.Forbidden(), context.HttpContext.RequestAborted)
            };
        });

        return services;
    }
}
