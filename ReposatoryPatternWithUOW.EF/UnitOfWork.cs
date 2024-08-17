using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.Core.Models;
using ReposatoryPatternWithUOW.Core.OptionsPatternClasses;
using ReposatoryPatternWithUOW.EF.Mapper;
using ReposatoryPatternWithUOW.EF.Reposatories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.EF
{
    public class UnitOfWork : IUnitOfWork
    {
        AppDbContext context;
        public IUserReposatory UserReposatory { get; }

        public IBaseRepository<UserConnection> UserConnection { get; }

        public IChatRepository ChatRepository { get; }
        public IMessageRepository MessageRepository { get; }
        public IBaseRepository<FriendRequest> FriendRequestRepository { get; }
        public IBaseRepository<ChatMessage> ChatMessageRepository { get; }
        public IBaseRepository<UserChat> UserChat { get; }
        



        public UnitOfWork(AppDbContext context,Mapperly mapper,TokenOptionsPattern options,ISenderService senderService)
        {
            this.context = context;
            UserReposatory = new UserReposatory(context, mapper,options,senderService);
            UserConnection = new BaseRepository<UserConnection>(context);
            ChatRepository = new ChatRepository(context);
            MessageRepository = new MessageRepository(context);
            FriendRequestRepository = new BaseRepository<FriendRequest>(context);
            ChatMessageRepository = new BaseRepository<ChatMessage>(context);
            UserChat = new BaseRepository<UserChat>(context);


        }


        public Task<int> SaveChangesAsync()
        {
            return context.SaveChangesAsync();
        }
    }
}
