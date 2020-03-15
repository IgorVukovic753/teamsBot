using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamsAuth.APIHandlers;

namespace TeamsAuth
{
    public class AuthenticatedUser
    {
        public AuthenticatedUser()
        { }
        public bool IsAuthenticated { get; set; }
        public bool IsSubscriptionOK { get; set; }
        public string JwtSecurityToken { get; set; }
        public string JwtSecurityTokenForBonsaiPGT { get; set; }
        public string UserIdentifier { get; set; }
        public DateTime Expiration { get; set; }

        public APIResults APIResults { get; set; }

        public string GivenName
        {
            get; set;
        }
    }
}
