using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.Models
{
    public class UserChat
    {
        public int UserId { get; set; }
        public string ChatId { get; set; }
        public virtual ICollection<Message> Messages { get; set; } = null!;
    }
}
