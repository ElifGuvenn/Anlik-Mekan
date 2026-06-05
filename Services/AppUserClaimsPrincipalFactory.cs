using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using AnlikMekanCore.Models;

namespace AnlikMekanCore.Services;

public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser>
{
    public AppUserClaimsPrincipalFactory(UserManager<AppUser> userManager, IOptions<IdentityOptions> options)
        : base(userManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("Rol", user.Rol));
        if (!string.IsNullOrEmpty(user.FotoUrl))
            identity.AddClaim(new Claim("FotoUrl", user.FotoUrl));
        return identity;
    }
}
