using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Core.Domain.Constant
{
    public static class UserRoles
    {
        public const string CUSTOMER = "CUSTOMER";
        public const string SALES_STAFF = "SALES_STAFF";
        public const string PURCHASES_STAFF = "PURCHASES_STAFF";
        public const string WAREHOUSE_STAFF = "WAREHOUSE_STAFF";
        public const string MANAGER = "MANAGER";
        public const string ADMIN = "ADMIN";

        public static readonly string[] ALL =
        [
            CUSTOMER,
            SALES_STAFF,
            PURCHASES_STAFF,
            WAREHOUSE_STAFF,
            MANAGER,
            ADMIN
       ];
    }
}
