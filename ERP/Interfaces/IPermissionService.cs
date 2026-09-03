using System.Security.Claims;
using System.Threading.Tasks;

namespace ERP.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string screenName, string action);
}
