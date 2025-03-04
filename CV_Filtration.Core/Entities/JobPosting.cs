﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CV_Filtation_System.Core.Entities
{
    public class JobPosting
    {
        public int JobPostingId { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public string SalaryRange { get; set; }
        public string Description { get; set; }
        public int CompanyId { get; set; }
        public string JobType { get; set; } 
        public string WorkMode { get; set; }
        public string JobImageUrl { get; set; }
        public bool IsFeatured { get; set; } 
        public bool IsRecommended { get; set; } 
        public Company Company { get; set; }
    }
}

