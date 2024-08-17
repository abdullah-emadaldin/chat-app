﻿using Microsoft.IdentityModel.Tokens;
using ReposatoryPatternWithUOW.Core.Interfaces;
using ReposatoryPatternWithUOW.Core.Models;
using ReposatoryPatternWithUOW.Core.OptionsPatternClasses;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.EF.Reposatories
{
    public class TokenHandler : IToken
    {
        public static string GenerateToken(User? user = null, DateTime? expiresAt = null, TokenOptionsPattern? tokenOpts = null)
        {
            if (expiresAt is not null && user is not null && tokenOpts is not null)
                return GenerateJwt(user, (DateTime)expiresAt, tokenOpts);
            return GenerateRefreshToken();
        }


        public static JwtSecurityToken? ExtractJwt(HttpRequest Request)
        {
            var auth = Request.Headers["Authorization"].ToString();
            if (auth.IsNullOrEmpty())
                return null;
            var jwtString = auth.Split(" ")[1];
            return new JwtSecurityTokenHandler().ReadJwtToken(jwtString);
        }


        



        public static JwtSecurityToken? ExtractJwtFromQuery(HttpRequest Request)
        {
            var auth = Request.Query["access_token"].ToString();
            if (auth.IsNullOrEmpty())
                return null;

            return new JwtSecurityTokenHandler().ReadJwtToken(auth);
        }





        private static string GenerateJwt(User user, DateTime expiresAt, TokenOptionsPattern tokenOpts)
        {


            var claims = new List<Claim>() {
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim("id",user.Id.ToString()),
                new Claim("firstName",user.FirstName),
                new Claim("lastName",user.LastName)

            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOpts.SecretKey));
            var signingCreds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                expires: expiresAt,
                claims: claims,
                issuer: tokenOpts.Issuer,
                audience: tokenOpts.Audience,
                signingCredentials: signingCreds


                );

            return new JwtSecurityTokenHandler().WriteToken(jwt);

        }
        private static string GenerateRefreshToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);

        }
    }
}
