using System.Text.RegularExpressions;

namespace ReposatoryPatternWithUOW.Core.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string ChatId { get; set; }
        public DateTime SendAt { get; set; }
        public string MessageText { get; set; } = null!;
        public bool IsRead { get; set; }
        public virtual User Sender { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
        public virtual Chat Chat { get; set; } = null!;
    }
}