﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReposatoryPatternWithUOW.Core.ReturnedModels
{
    public class SendCodeResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
    }
}
