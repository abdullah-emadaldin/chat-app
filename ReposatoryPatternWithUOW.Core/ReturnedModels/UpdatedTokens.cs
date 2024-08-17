using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.ReturnedModels
{
    public class UpdatedTokens
    {
        public string? Jwt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpirationOfJwt { get; set; }
        public DateTime? ExpirationOfRefreshToken { get; set; }
        public bool Success { get; init; }
    }
}
