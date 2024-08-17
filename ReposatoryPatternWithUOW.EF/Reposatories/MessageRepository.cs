using Microsoft.EntityFrameworkCore;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.EF.Reposatories
{
    public class MessageRepository : BaseRepository<Message>,IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task MakeAllReadInGroup(string chatId)
        {
            await context.Messages.Where(x => x.ChatId == chatId).ExecuteUpdateAsync(x => x.SetProperty(x => x.IsRead, true));

        }
    }
}
