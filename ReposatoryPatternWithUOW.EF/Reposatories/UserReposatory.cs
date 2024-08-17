using Microsoft.EntityFrameworkCore;
using ReposatoryPatternWithUOW.Core.DTOs;
using ReposatoryPatternWithUOW.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReposatoryPatternWithUOW.EF.Mapper;
using ReposatoryPatternWithUOW.Core.Models;
using Microsoft.Extensions.Options;
using ReposatoryPatternWithUOW.Core.ReturnedModels;
using ReposatoryPatternWithUOW.Core.OptionsPatternClasses;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.JsonPatch;
using MimeKit.IO.Filters;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

namespace ReposatoryPatternWithUOW.EF.Reposatories
{
    public class UserReposatory:BaseRepository<User>,IUserReposatory
    {
        private readonly AppDbContext context;
        private readonly Mapperly mapper;
        private readonly ISenderService senderService;
        TokenOptionsPattern options;
        
        public UserReposatory(AppDbContext context, Mapperly mapper, TokenOptionsPattern options, ISenderService senderService):base(context)
        {
            this.context = context;
            this.mapper = mapper;
            this.options = options;
            this.senderService = senderService;
        }

        public async Task<bool> SignUpAsync(SignUpDto signupDto)
        {
            if (signupDto is null || await context.Users.AnyAsync(x => x.Email == signupDto.Email))
                return false;
            try
            {

                User user = mapper.MapToUser(signupDto);
                //user.EmailConfirmed = false;
                var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(user.Password);
                user.Password = hashedPassword;
                await context.AddAsync(user);
                return true;

            }
            catch
            {
                return false;
            }
        }

        public async Task<LoginResult> LoginAsync(LoginDto loginDto)

        {
            context.ChangeTracker.LazyLoadingEnabled = false;
            if (loginDto.Email is null || loginDto.Password is null)
                return new() { Success = false };

            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == loginDto.Email);
           
            if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(loginDto.Password, user.Password))
            {
                return new() { Success = false };
            }
            if (!user.EmailConfirmed)
            {
                return new()
                {
                    Success = true,
                    EmailConfirmed = false,
                };

            }
            var expirationOfJWT = DateTime.Now.AddHours(10);
            var expirationOfRefreshToken = DateTime.Now.AddHours(12);
            var refreshToken = new RefreshToken()
            {

                UserId = user.Id,
                CreatedAt = DateTime.Now,
                ExpiresAt = expirationOfRefreshToken,
                Token = TokenHandler.GenerateToken()

            };
            context.Attach(user);
            user.RefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();

            await Console.Out.WriteLineAsync($"-------------------------- {user.ProfilePicture} ------------------");
            return new()
            {
                Success = true,
                EmailConfirmed = true,
                Jwt = TokenHandler.GenerateToken(user, expirationOfJWT, options),
                ExpirationOfJwt = expirationOfJWT,
                ExpirationOfRefreshToken = expirationOfRefreshToken,
                RefreshToken = refreshToken.Token,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture,
                Biography = user.Biography,
                UserId = user.Id

            };


        }

        public async Task<bool> ResetPasswordAsync(ResetAccountDto resetPasswordDto)
        {
            context.ChangeTracker.LazyLoadingEnabled = false;
            var user = await context.Users.Include(x => x.IdentityTokenVerification).AsNoTracking().FirstOrDefaultAsync(x => x.Email == resetPasswordDto.Email);
            if (user is null || user.IdentityTokenVerification is null || !user.IdentityTokenVerification.IsActive)
                return false;
            user.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(resetPasswordDto.NewPassword);
            context.Users.Update(user);
            return true;

        }

        public async Task<SendCodeResult> SendVerficationCodeAsync(string email, bool? isForResetingPassword = false)
        {
            context.ChangeTracker.LazyLoadingEnabled = false;
            var user = await context.Users.AsNoTracking().Include(x => x.IdentityTokenVerification).Include(x => x.EmailVerificationCode).FirstOrDefaultAsync(x => x.Email == email);

            if (user is null)
                return new() { Success = false };
            if (user.EmailVerificationCode is not null && user.EmailVerificationCode.ExpiresAt < DateTime.Now.AddSeconds(-5))
                context.Remove(user.EmailVerificationCode);
            else if (user.EmailVerificationCode is not null)
                return new() { Success = true, Token = user.IdentityTokenVerification.Token };

            string body;
            string subject;
            var rand = new Random();
            var verificationNum = rand.Next(100000, int.MaxValue);
            if (isForResetingPassword is null or false)
            {
                if (user.EmailConfirmed)
                    return new() { Success = false };
                body = $@"Dear <span style='text-transform:Capitalize'>{user.FirstName} {user.LastName}</span>,<br><br> 
                        you have signed up on our Chat application with your email {email}, <br><br>
                        and we have sent to you a verification code which is : <b>{verificationNum}</b> ";
                subject = "Email Confirmation";
            }
            else
            {
                body = $@"Dear <span style='text-transform:Capitalize'>{user.FirstName} {user.LastName}</span> ,<br><br>
                        There was a request to reset your password with your email {email} on our Chat application! <br><br>
                        If you did not make this request then please ignore this email,<br><br>
                        and we have sent to you a verification code which is : <b>{verificationNum}</b>";
                subject = "Reset Password";
            }

            user.EmailVerificationCode = new() { ExpiresAt = DateTime.Now.AddMinutes(3), Code = verificationNum.ToString() };
            context.Update(user);
            var identityToken = TokenHandler.GenerateToken();
            if (user.IdentityTokenVerification is not null)
                context.IdentityTokenVerifications.Remove(user.IdentityTokenVerification!);
            user.IdentityTokenVerification = new() { ExpiresAt = DateTime.Now.AddMinutes(25), Token = identityToken };


            Task t1 = senderService.SendEmailAsync(email, subject, body);
            Task t2 = context.SaveChangesAsync();
            await Task.WhenAll(t1, t2);
            return new() { Token = identityToken, Success = true };

        }


        public async Task<bool> ValidateCode(string email, string code, bool isForResetPassword = false)
        {
            context.ChangeTracker.LazyLoadingEnabled = false;

            var user = await context.Users.Include(x => x.EmailVerificationCode).FirstOrDefaultAsync(x => x.Email == email);

            if (user is null || user.EmailVerificationCode is null )
                return false;

            if (user.EmailVerificationCode.Code != code)
            {

                if (user.EmailVerificationCode.ExpiresAt < DateTime.Now)
                {
                    context.Remove(user.EmailVerificationCode);
                }
                return false;
            }
            if (user.EmailVerificationCode.ExpiresAt < DateTime.Now)
            {
                context.Remove(user.EmailVerificationCode);
                return false;
            }
            if (!isForResetPassword)
            {
                user.EmailConfirmed = true;
                //context.Remove(user.IdentityTokenVerification);
                context.Update(user);
            }
            context.Remove(user.EmailVerificationCode);
            return true;

        }

        public async Task<bool> SignOut(string email,string token)
        {
            var result =context.RefreshTokens.Where(x => x.User.Email == email && x.Token == token).ExecuteDelete();
            return (result > 0);
           
        }



        public async Task<UpdatedTokens> UpdateTokensAsync(UpdateTokensDto updateTokensDto)
        {
            context.ChangeTracker.LazyLoadingEnabled = false;
            var tokenObj = await context.Set<RefreshToken>().Include(x => x.User).AsNoTracking().FirstOrDefaultAsync(x => x.Token == updateTokensDto.RefreshToken);
            if (tokenObj is null || updateTokensDto.Email != tokenObj.User.Email)
                throw new Exception("Invalid Token");
            else if (!tokenObj.IsActive)
            {
                context.Remove(tokenObj);
                return new UpdatedTokens() { Success = false };
            }
            var expirationOfJwt = DateTime.Now.AddMinutes(15);
            var expirationOfRefreshToken = DateTime.Now.AddHours(24);

            var jwt = TokenHandler.GenerateToken(tokenObj.User, expirationOfJwt, options);
            var newRefreshToken = TokenHandler.GenerateToken();
            context.Remove(tokenObj);
            await context.AddAsync(new RefreshToken() { ExpiresAt = expirationOfRefreshToken, Token = newRefreshToken, UserId = tokenObj.UserId });






            return new()
            {
                ExpirationOfJwt = expirationOfJwt
                          ,
                ExpirationOfRefreshToken = expirationOfRefreshToken
                          ,
                Jwt = jwt
                          ,
                RefreshToken = newRefreshToken
                          ,
                Success = true
            };

        }



        public async Task<bool> UpdatePasswordAsync(int id, UpdatePasswordDto updatePasswordDto)
        {
            context.ChangeTracker.LazyLoadingEnabled = false;
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var user = await context.Users.FindAsync(id);
            if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(updatePasswordDto.OldPassword, user.Password))
            {
                return false;
            }
            user.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(updatePasswordDto.NewPassword);
            context.Users.Update(user);

            return true;
        }



        public async Task<bool> UpdateInsensitiveDataAsync(JsonPatchDocument<User> patchDocument, int id)
        {
            await Console.Out.WriteLineAsync("kgdhfkfghklhk");
            if (!patchDocument.Operations.Exists(x => x.path.Equals("firstname", StringComparison.OrdinalIgnoreCase)
           || x.path.Equals("lastname", StringComparison.OrdinalIgnoreCase)
           || x.path.Equals("biography", StringComparison.OrdinalIgnoreCase)
           ))

                return false;

            await Console.Out.WriteLineAsync("kgdhldfhfkhgjflkjhklfgkljhklfkfghklfhklhkllhk");
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var user = await context.Users.FindAsync(id);
            if (user == null)
                return false;
            try
            {
                patchDocument.ApplyTo(user);
                context.Update(user);
                return true;
            }
            catch
            {
                await Console.Out.WriteLineAsync("iii");
                return false;
            }
        }



        public async Task<bool> UpdateProfilePictureAsync(UpdatePictureDto updatePictureDto)
        {
            context.ChangeTracker.LazyLoadingEnabled = false;
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var user = await context.Users.FindAsync(updatePictureDto.id);
            if (user is null)
                return false;
            string RootPath = "wwwroot";
            if (updatePictureDto.NewPicture != null)
            {
                string filename = Guid.NewGuid().ToString();
                var upload = Path.Combine(RootPath, "Users");
                var ext = Path.GetExtension(updatePictureDto.NewPicture.FileName);

                if (user.ProfilePicture != null)
                {
                    var oldimg = Path.Combine(RootPath,user.ProfilePicture.TrimStart('\\'));
                    if (System.IO.File.Exists(oldimg))
                        System.IO.File.Delete(oldimg);

                }

                using (var filestream = new FileStream(Path.Combine(upload, filename + ext), FileMode.Create))
                {
                    await updatePictureDto.NewPicture.CopyToAsync(filestream);
                }
                user.ProfilePicture = Path.Combine("Users", filename + ext);
                await Console.Out.WriteLineAsync($"-------------------------- {user.ProfilePicture} ------------------");

            }
            context.Update(user);

            return true;

        }




    }
}
