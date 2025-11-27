using Microsoft.AspNetCore.Authorization;

namespace PMS.API.Helpers.AttributeRoles
{
    public class PermissionAuthorizeAttribute : AuthorizeAttribute
    {
        public PermissionAuthorizeAttribute(string permission)
        {
            Policy = permission;
        }
    }
}
