using ReposatoryPatternWithUOW.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.Interfaces
{
    public interface IUnitOfWork
    {
        IUserReposatory UserReposatory { get; }
        public IBaseRepository<UserConnection> UserConnection { get; }
        IChatRepository ChatRepository { get; }
        public IMessageRepository MessageRepository { get; }
        public IBaseRepository<FriendRequest> FriendRequestRepository { get; }
        public IBaseRepository<ChatMessage> ChatMessageRepository { get; }
        public IBaseRepository<UserChat> UserChat { get; }

        public Task<int> SaveChangesAsync();
    }
}
