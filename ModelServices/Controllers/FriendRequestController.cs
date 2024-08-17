using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

using ModelServices.Hubs;
using ReposatoryPatternWithUOW.Core.DTOs;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.Core.Models;
using ReposatoryPatternWithUOW.Core.ReturnedModels;
using ReposatoryPatternWithUOW.EF;
using ReposatoryPatternWithUOW.EF.Reposatories;
using System.Text.RegularExpressions;

namespace ModelServices.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FriendRequestController(IUnitOfWork unitOfWork, IHubContext<ChatHub> chatHub) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
             var id = ReposatoryPatternWithUOW.EF.Reposatories.TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString();
                if (id is null || !int.TryParse(id, out int resultId))
                    return BadRequest();
            var friendRequests = await unitOfWork.FriendRequestRepository.GetWhere(x => x.RecipientId == resultId);
            await Console.Out.WriteLineAsync("id is: " +friendRequests.Select(x=>x.SenderId).ToString());
            if (friendRequests is null) return BadRequest("sorry not found");
            var reqs = friendRequests?.Select(x => new FriendRequests
            {
                Id = x.Sender.Id,
                FirstName = x.Sender.FirstName,
                LastName = x.Sender.LastName,
                ProfilePicture = x.Sender.ProfilePicture,
                Biography = x.Sender.Biography
            }).ToList();

             return Ok(reqs);

        }


        [HttpPost("{id}")]
        public async Task<IActionResult> SendRequest(int id)//id اللي هيتبعتله ريكويست
        {
            var Id = ReposatoryPatternWithUOW.EF.Reposatories.TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString();
            if (Id is null || !int.TryParse(Id, out int resultId))
                return BadRequest();
            var me = await unitOfWork.UserReposatory.GetByIdAsync(resultId);
            if (me is null) return BadRequest();
            string fullName = me.FirstName + " " + me.LastName;
            var pic = me.ProfilePicture;
            var bio = me.Biography;

            var reciption = await unitOfWork.UserReposatory.GetByIdAsync(id);

            if (reciption is null || // اللي بعتله موجود
                reciption.Id==me.Id || // اني مش ببعت لنفسي
                reciption.Chat.Any(x=>x.Users.Any(x=>x.Id==resultId))|| // انه مش صديق عندي او مفيش بيني وبينه شات
                await unitOfWork.FriendRequestRepository.ExistsAsync(x => x.SenderId == resultId && x.RecipientId == reciption.Id || //ان مفيش طلب انا باعتهوله
                x.SenderId == reciption.Id && x.RecipientId == resultId)) // ان مفيش طلب هو باعتهولي
                return BadRequest();
                
            var request = new FriendRequest() { RecipientId = reciption.Id , SenderId = resultId };

            await unitOfWork.FriendRequestRepository.AddAsync(request);

            var connectionIdsOfReceipant = (await unitOfWork.UserConnection.GetWhere(x => x.User.Id == id)).ToList();
            if (!connectionIdsOfReceipant.IsNullOrEmpty())
            {
                for (int i = 0; i < connectionIdsOfReceipant.Count(); i++)
                {
                    await chatHub.Clients.Client(connectionIdsOfReceipant[i].ConnectionId).SendAsync("friendRequest", resultId, fullName,pic,bio);
                }

                await unitOfWork.SaveChangesAsync();
            }
            else
                await unitOfWork.SaveChangesAsync();
            return Ok();


        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var Id = ReposatoryPatternWithUOW.EF.Reposatories.TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString();
            if (Id is null || !int.TryParse(Id, out int resultId))
                return BadRequest();
            var recipient = await unitOfWork.UserReposatory.GetOneByAsync(x => x.Id == id);
            if (recipient is null)
                return NotFound();
            var result = await unitOfWork.FriendRequestRepository.ExecuteDeleteAsync(x => x.SenderId == resultId && x.RecipientId == recipient.Id);
            if (result == 0)
                return NotFound();
            var connectionIdsOfReceipant = (await unitOfWork.UserConnection.GetWhere(x => x.User.Id == id)).ToList();
            if (!connectionIdsOfReceipant.IsNullOrEmpty())
                for (int i = 0; i < connectionIdsOfReceipant.Count; i++)
                {
                    await chatHub.Clients.Client(connectionIdsOfReceipant[i].ConnectionId).SendAsync("friendRequestCancelled");
                }

            return Ok();




        }


        [HttpPost("response")]
        public async Task<IActionResult> Response(ResponseFriendRequestDto response)
        {
            var id = ReposatoryPatternWithUOW.EF.Reposatories.TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString();
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();
            var me = await unitOfWork.UserReposatory.GetByIdAsync(resultId);
            if (me is null) return BadRequest();
            string fullName = me.FirstName + " " + me.LastName;
            if (!response.IsAccepted)
            {
               
                int result = await unitOfWork.FriendRequestRepository.ExecuteDeleteAsync(x => x.RecipientId == resultId && x.Sender.Id == response.id);
                var senderConnections = (await unitOfWork.UserConnection.GetWhere(x => x.User.Id == response.id)).ToList();
                for (int i = 0; i < senderConnections.Count; i++)
                {
                    await chatHub.Clients.Client(senderConnections[i].ConnectionId).SendAsync("requestRejected", fullName);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    await Console.Out.WriteLineAsync("reqeust rejected");
                    Console.ForegroundColor = ConsoleColor.White;
                }
               
                return result > 0 ? Ok() : BadRequest();
            }

            var user2 = await unitOfWork.UserReposatory.GetByIdAsync(response.id); // the sender


            if (user2 is null) return BadRequest();
            if (!await unitOfWork.FriendRequestRepository.ExistsAsync(x => x.SenderId == user2.Id && x.RecipientId == resultId))
                return BadRequest();
            
            List<User> users = new List<User>() { me , user2};

            var newGroup = Guid.NewGuid().ToString()!;
            await unitOfWork.ChatRepository.AddAsync(new Chat { Id = newGroup, Users = users });
            await unitOfWork.FriendRequestRepository.ExecuteDeleteAsync(x => x.RecipientId == resultId && x.Sender.Id == response.id);
           
                for (int p = 0; p < users.Count; p++)
                    if (users[p].UserConnections is not null && users[p].UserConnections!.Any())
                    {
                        var connections = users[p].UserConnections!.ToList();
                        for (int i = 0; i < connections.Count; i++)
                        {
                            await chatHub.Groups.AddToGroupAsync(connections[i].ConnectionId, newGroup);
                            if (p == users.Count - 1)
                            {
                                await chatHub.Clients.Client(connections[i].ConnectionId).SendAsync("requestAccepted", me.Id, fullName, newGroup);
                            Console.ForegroundColor = ConsoleColor.Red;
                            await Console.Out.WriteLineAsync("reqeust accepted");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        }
                    }
          
          

            await unitOfWork.SaveChangesAsync();
            if (me!.UserConnections!.Any())
                await chatHub.Clients.Group(newGroup).SendAsync("newOneActive", fullName);
            if (user2.UserConnections!.Any())
                await chatHub.Clients.Group(newGroup).SendAsync("newOneActive", user2.FirstName + " " + user2.LastName);
            return Ok();




         
        }

    }
}
