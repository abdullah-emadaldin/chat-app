using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public int SenderId { get; set; }
        public string ChatId { get; set; }
        public virtual User User { get; set; }
        public virtual Chat Chat { get; set; }
    }
}
