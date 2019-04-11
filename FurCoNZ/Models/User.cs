﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FurCoNZ.Models
{
    public class User
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public AgeBracket AgeBracket { get; set; }

        public string Pronouns { get; set; }

        public string Allergies { get; set; }

        public string DietryRequirements { get; set; }

        public DateTime DateOfBirth { get; set; }
    }
}
