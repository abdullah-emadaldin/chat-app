using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.EF.Reposatories;

namespace ModelServices.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SearchController(IUnitOfWork unitOfWork) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            string id = TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString()!;
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();

            var currentChats = await unitOfWork.ChatRepository.GetWhere(x => x.Users.Any(x => x.Id == resultId));

            var ids = currentChats.SelectMany(x => x.Users).Where(x => x.Id != resultId).Select(x => x.Id).ToHashSet();

            var users = await unitOfWork.UserReposatory.GetWhere(x => x.Id != resultId && !ids.Contains(x.Id));
            var serchResult = users.Select(x => new
            {
                x.Id,
                x.FirstName,
                x.LastName,
                x.ProfilePicture,
                x.Biography
            });


            return Ok(serchResult);
        }


        [HttpGet("friends")]
        public async Task<IActionResult> GetFriends()
        {
            string id = TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString()!;
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();

            var currentChats = await unitOfWork.ChatRepository.GetWhere(x => x.Users.Any(x => x.Id == resultId));
            var users = currentChats.SelectMany(x => x.Users).Where(x => x.Id != resultId).Select(x => new
            {
                x.Id,
                name = x.FirstName + x.LastName,
                x.ProfilePicture,
                x.Biography
            });



            return Ok(users);
        }


    }
}
