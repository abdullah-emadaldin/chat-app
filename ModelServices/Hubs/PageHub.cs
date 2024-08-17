using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.EF.Reposatories;
using System.Text.RegularExpressions;

namespace ModelServices.Hubs
{
    [Authorize]
    public class PageHub(IUnitOfWork unitOfWork) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            string id = TokenHandler.ExtractJwt(Context.GetHttpContext()!.Request)!.Payload["id"].ToString()!;
            var user = await unitOfWork.UserReposatory.GetByIdAsync(int.Parse(id));
            user!.UserConnections.Add(new() { ConnectionId = Context.ConnectionId });

            var IDsOfChats = user.Chat.Select(x => x.Id);
             foreach (var chatId in IDsOfChats)
             await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
         
            await Task.WhenAll(unitOfWork.SaveChangesAsync(), base.OnConnectedAsync());

        }




        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Task.WhenAll(unitOfWork.UserConnection.ExecuteDeleteAsync(x => x.ConnectionId == Context.ConnectionId),
                 base.OnDisconnectedAsync(exception));

            //await unitOfWork.UserConnection.RemoveWhereAsync(x => x.ConnectionId == Context.ConnectionId);

            //await base.OnDisconnectedAsync(exception);
        }
    }
}
