using System;
using System.Collections.Generic;
using System.Text;

namespace Feuerwehr.Common.Models.Dto
{
    public class TrainingCourseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TrainingLevel Level { get; set; }
        public TrainingStatus Status { get; set; }
        public int AssignedFireDepartmentId { get; set; }
        public string AssignedFireDepartmentName { get; set; } = string.Empty;
        public int AssignedSeats { get; set; }
    }
}
