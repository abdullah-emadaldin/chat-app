using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.DTOs
{
    public class UpdatePasswordDto
    {
        [StringLength(100)]
        public required string OldPassword { get; set; }

        [StringLength(100, MinimumLength = 8)]
        public required string NewPassword { get; set; }
    }
}
