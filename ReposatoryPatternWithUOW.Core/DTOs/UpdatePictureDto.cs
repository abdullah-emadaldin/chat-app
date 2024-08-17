using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.DTOs
{
    public class UpdatePictureDto
    {
        public required int id { get; set; }
        public required IFormFile NewPicture { get; set; }
    }
}
