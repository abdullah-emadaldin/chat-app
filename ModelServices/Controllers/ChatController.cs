using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.EF;
using ReposatoryPatternWithUOW.EF.Reposatories;


namespace ModelServices.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(IUnitOfWork unitOfWork) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            string id = TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString()!;
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();
            bool hasFriendRequests = await unitOfWork.FriendRequestRepository.ExistsAsync(x => x.RecipientId == resultId);
            var result = unitOfWork.ChatRepository.GetListWithTracking(x => x.Users.Any(x => x.Id == resultId));


            var filteredChats = result.Where(x => x.Users.Any(u => u.Id != resultId)).ToList();

          
            var chatData = filteredChats.Select(x => new
            {
                x.Id,
                IsRead = x.Messages.Any() ? !x.Messages.Any(m => !m.IsRead && m.SenderId != resultId) : true,
                Users = x.Users.Where(u => u.Id != resultId).Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.ProfilePicture,
                    u.Biography
                })
            });

            return Ok(new { HasFriendRequests = hasFriendRequests, Chats = chatData });
        }

        [HttpGet("message")]
        public async Task<IActionResult> GetMessages(string chatId)
        {
            string id = TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString()!;
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();

            var chat = await unitOfWork.ChatRepository.GetByIdAsync(chatId);

            //var receiver = chat.Users.FirstOrDefault(x => x.Id != resultId);

            var messages = chat!.Messages.Select(x=> new { x.MessageText,x.SenderId,x.ReceiverId,messageDate = x.SendAt.ToString("tt:hh:mm") }).ToList();


            return Ok(messages);
        }



    }
}
