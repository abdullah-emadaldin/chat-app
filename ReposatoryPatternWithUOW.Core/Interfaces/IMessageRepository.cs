using ReposatoryPatternWithUOW.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.Interfaces
{
    public interface IMessageRepository:IBaseRepository<Message>
    {
        Task MakeAllReadInGroup(string chatId);
    }
}
