using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.Core.Models;
using ReposatoryPatternWithUOW.EF.Reposatories;
using System.Text.RegularExpressions;

namespace ModelServices.Hubs
{

    [Authorize]
    public class ChatHub(IUnitOfWork unitOfWork) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            
            int id = int.Parse(TokenHandler.ExtractJwtFromQuery(Context.GetHttpContext()!.Request)!.Payload["id"].ToString()!);
            await Console.Out.WriteLineAsync("id is:  "+id);
            string name = TokenHandler.ExtractJwtFromQuery(Context.GetHttpContext()!.Request)!.Payload["firstName"].ToString()!;
            var user = await unitOfWork.UserReposatory.GetByIdAsync(id);
            user!.UserConnections!.Add(new() { ConnectionId = Context.ConnectionId });
            await Console.Out.WriteLineAsync("user: "+ name + " has joined");
            var IDsOfChats = user!.Chat!.Select(x => new { x.Id, Users = x.Users.Where(x => x.Id != id && x.UserConnections!.Any())?.Select(x => x.FirstName) }).ToList();
            foreach (var chatId in IDsOfChats)
                await Groups.AddToGroupAsync(Context.ConnectionId, chatId.Id);

            await Task.WhenAll(unitOfWork.SaveChangesAsync(), base.OnConnectedAsync());

        }


        public async Task SendMessage(string message, string chatId)
        {
           var chat = await unitOfWork.ChatRepository.GetByIdAsync(chatId);
            if (chat == null) { return; }
            var token = TokenHandler.ExtractJwtFromQuery(Context.GetHttpContext()!.Request);
            int id = int.Parse(token!.Payload["id"].ToString()!);
            string name = token.Payload["firstName"].ToString()!;
            var receiverId = chat.Users.FirstOrDefault(x => x.Id != id)!;
            string messageDate = DateTime.UtcNow.ToString("tt:hh:mm");
            //chat.Users.Add(new() { Message = message, SenderId = id, SentAt = DateTime.UtcNow });
            await unitOfWork.MessageRepository.AddAsync(new() { ChatId = chatId, MessageText = message, IsRead = false, SenderId = id, ReceiverId = receiverId.Id, SendAt=DateTime.UtcNow });
          //  await unitOfWork.ChatMessageRepository.AddAsync(new() { Message = message , SenderId = id , ChatId = chatId , SentAt = DateTime.UtcNow});
            await Task.WhenAll(unitOfWork.SaveChangesAsync(), Clients.OthersInGroup(chatId.ToString()).SendAsync("SendMessage", id, message, messageDate));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("message: " + message + "--------");
        }

        public async Task DeleteChat(string chatId)
        {
            var chat = await unitOfWork.ChatRepository.GetByIdAsync(chatId);
            if (chat == null) { return; }
            var token = TokenHandler.ExtractJwtFromQuery(Context.GetHttpContext()!.Request);
            int id = int.Parse(token!.Payload["id"].ToString()!);
            string name = token.Payload["firstName"].ToString()!;
            string message = $"{name} has left the chat";
            await Task.WhenAll(unitOfWork.UserChat.ExecuteDeleteAsync(x => x.UserId == id && x.ChatId == chatId), Clients.OthersInGroup(chatId.ToString()).SendAsync("DeleteChat", message ));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("message: " + message);
        }


        public async Task SendM(string message)
        {
            Console.WriteLine("message: " + message);
            await Clients.All.SendAsync("r", message);
        }

        public async Task IsTyping(string chatId)
        {
            await Clients.OthersInGroup(chatId).SendAsync("IsTyping");
        }


        public async Task IsOnline(string chatId)
        {
            int id = int.Parse(TokenHandler.ExtractJwtFromQuery(Context.GetHttpContext()!.Request)!.Payload["id"].ToString()!);
            var chat = await unitOfWork.ChatRepository.GetByIdAsync(chatId);
            var otherUserId= chat!.Users.FirstOrDefault(x => x.Id != id)?.Id?? -1;
            bool isOtherUserOnline = await unitOfWork.UserConnection.ExistsAsync(x => x.UserId == otherUserId);
            if (isOtherUserOnline)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("User with Id: " + otherUserId + " is online");
                await Clients.OthersInGroup(chatId).SendAsync("IsOnline",true);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("User with Id: " + otherUserId + " is offline");

                await Clients.OthersInGroup(chatId).SendAsync("IsOnline",false);
            }
        }


        public async Task MakeMessagesRead(string chatId)
        {
            await unitOfWork.MessageRepository.MakeAllReadInGroup(chatId);
            await Clients.OthersInGroup(chatId).SendAsync("read");
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
