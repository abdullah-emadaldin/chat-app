using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.DTOs
{
    public class ResponseFriendRequestDto
    {
        public int id { get; set; }
        public bool IsAccepted { get; set; }
    }
}
