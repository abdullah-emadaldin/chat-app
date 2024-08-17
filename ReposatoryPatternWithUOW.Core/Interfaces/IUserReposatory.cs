using Microsoft.AspNetCore.JsonPatch;
using ReposatoryPatternWithUOW.Core.DTOs;
using ReposatoryPatternWithUOW.Core.Models;
using ReposatoryPatternWithUOW.Core.ReturnedModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.Interfaces
{
    public interface IUserReposatory:IBaseRepository<User>
    {
        public Task<bool> SignUpAsync(SignUpDto obj);
        public Task<LoginResult> LoginAsync(LoginDto obj);
        Task<SendCodeResult> SendVerficationCodeAsync(string email, bool? isForResetingPassword = false);

        public Task<bool> ValidateCode(string email, string code, bool isForResetPassword = false);
        public Task<bool> SignOut(string email, string token);
        Task<bool> ResetPasswordAsync(ResetAccountDto resetPasswordDto);
        Task<UpdatedTokens> UpdateTokensAsync(UpdateTokensDto updateTokensDto);
        Task<bool> UpdatePasswordAsync(int id, UpdatePasswordDto updatePasswordDto);
        Task<bool> UpdateInsensitiveDataAsync(JsonPatchDocument<User> patchDocument, int id);
        Task<bool> UpdateProfilePictureAsync(UpdatePictureDto updatePictureDto);
    }
}
