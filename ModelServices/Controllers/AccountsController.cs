using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


using ReposatoryPatternWithUOW.Core.DTOs;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.Core.Models;
using ReposatoryPatternWithUOW.EF.Reposatories;
using System.Security.Claims;

namespace ModelServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;

        public AccountsController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        [HttpPost("SignUp")]
        public async Task<IActionResult> SignUp(SignUpDto obj)
        {
            var result=await unitOfWork.UserReposatory.SignUpAsync(obj);
            if (result)
            {
               await unitOfWork.SaveChangesAsync();
                return Ok();
            }
            return BadRequest();
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto obj)
        {
            var result = await unitOfWork.UserReposatory.LoginAsync(obj);

            if (result.Success)
            {
                await Console.Out.WriteLineAsync("------------ " + result.ProfilePicture + "-----------------");
                await unitOfWork.SaveChangesAsync();
                return Ok(result);
            }
            return NotFound();

        }

        [HttpPost("SendCode")]
        public async Task<IActionResult> SendConfirmationCode(SendCodeDto sendCodeDto)
        {
            var result = await unitOfWork.UserReposatory.SendVerficationCodeAsync(sendCodeDto.Email, sendCodeDto.Reset is null or false ? false : true);
            if (result is null)
                return NotFound();
            
            return Ok();

        }
        [HttpPost("ValidateCode")]
        public async Task<IActionResult> ValidateConfirmationCode(ValidationCodeDto VCD)
        {
            
            var result = await unitOfWork.UserReposatory.ValidateCode(VCD.Email, VCD.Code, VCD.isForResetPassword);
            await unitOfWork.SaveChangesAsync();
            if (!result)
                return Forbid();
            return Ok();

        }

        //[HttpPost("ValidateResetCode")]
        //public async Task<IActionResult> ValidateResetCode(ValidationCodeDto VCD)
        //{

        //    var result = await unitOfWork.UserReposatory.ValidateCode(VCD.Email, VCD.Code,true);
        //    await unitOfWork.SaveChangesAsync();
        //    if (!result)
        //        return Forbid();
        //    return Ok();

        //}
        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword(ResetAccountDto resetPasswordDto)
        {
            var result = await unitOfWork.UserReposatory.ResetPasswordAsync(resetPasswordDto);
            if (!result)
                return BadRequest();
            await unitOfWork.SaveChangesAsync();
            return Ok();
        }


        
        [HttpPost("settings/password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordDto updatePasswordDto)
        {
            var id = TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString();
            Console.WriteLine("ff");
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();
            var result = await unitOfWork.UserReposatory.UpdatePasswordAsync(resultId, updatePasswordDto);
            if (!result)
            {
                return BadRequest();
            }
            await unitOfWork.SaveChangesAsync();
            return Ok();
        }


        [HttpPost("tokens")]
        public async Task<IActionResult> UpdateTokens(UpdateTokensDto updateTokenDto)
        {
            try
            {
                var result = await unitOfWork.UserReposatory.UpdateTokensAsync(updateTokenDto);
                await unitOfWork.SaveChangesAsync();
                if (!result.Success)
                {
                    return Unauthorized();
                }

                return Ok(result);
            }
            catch(Exception ex) 
            {
                return NotFound(ex);
            }

        }


        [Authorize]
        [HttpPatch("settings/insensitive-data")]
        public async Task<IActionResult> UpdateInsensitiveData([FromBody] JsonPatchDocument<User> patchDocument)
        {
            string id = TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString()!;
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();

            var result = await unitOfWork.UserReposatory.UpdateInsensitiveDataAsync(patchDocument, resultId);
            if (!result) return BadRequest("result is null");
            try
            {
                await unitOfWork.SaveChangesAsync();
            }
            catch
            {
                return BadRequest("couldn't save");
            }
            return Ok();
        }



        [Authorize]
        [HttpPost("settings/profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile newPicture)
        {

            string? id = TokenHandler.ExtractJwt(Request)?.Payload["id"].ToString()!;
            if (id is null || !int.TryParse(id, out int resultId))
                return BadRequest();


            var result = await unitOfWork.UserReposatory.UpdateProfilePictureAsync(new() { NewPicture = newPicture, id = resultId });
            if (!result) { return BadRequest(); }
            await unitOfWork.SaveChangesAsync();
            return Ok();
        }




        [HttpDelete("SignOut")]
        public async Task<IActionResult> SignOut(SignOutDto obj)
        {
            var result = await unitOfWork.UserReposatory.SignOut(obj.Email, obj.refreshToken);
            return result ? Ok() : BadRequest();
        }

    }
}
