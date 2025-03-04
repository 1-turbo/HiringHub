﻿namespace CV.Filtation.System.API.DTO
{
    public class UpdateJobPostingDto
    {
        public string Title { get; set; }
        public string Location { get; set; }
        public string SalaryRange { get; set; }
        public string Description { get; set; }
        public string WorkMode { get; set; }
        public string JobType { get; set; }
        public bool? IsFeatured { get; set; } 
        public bool? IsRecommended { get; set; } 
    }
}
