using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdvashERP.BusinessModel.Dto.Administration
{
    [Serializable]
    public class MasterCourseMappingDetailsDto
    {
        public string SubjectName { get; set; }
        public string MasterCourse { get; set; }
        public string MasterSubject { get; set; }
        public string MasterChapter { get; set; }
    }

    public class ManageMasterCourseMappingDto
    {
        public long Id { get; set; }
        public string Organization { get; set; }
        public string Program { get; set; }
        public string Session { get; set; }
        public string Course { get; set; }
        public long ProgramId { get; set; }
        public long SessionId { get; set; }
        public long CourseId { get; set; }
        public long OrganizationId { get; set; }
        public long CreatedById { get; set; }
        public long ModifiedById { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime CreationDate { get; set; }
        public int Status { get; set; }
        public int DataCount { get; set; }
        public long DataRank { get; set; }
        public long rn { get; set; }
    }

    public class DemoPaymentDto
    {
        public long Id { get; set; }
        public string Organization { get; set; }
        public DateTime TotalEffectiveTime { get; set; }
        public int NewQuestionAnswered { get; set; }
        public DateTime AverageAnsweringTime { get; set; }
        public int ReviewQuestion { get; set; }
        public int QuestionEditCount { get; set; }
        public double PaidAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public int PaymentStatus { get; set; }
        public int DataCount { get; set; }

    }

    public class DemoPaymentDtoTwo
    {
        public long Id { get; set; }
        public string Organization { get; set; }
        public DateTime DateRange { get; set; }
        public DateTime TotalEffectiveTime { get; set; }
        public int NewQuestionAnswered { get; set; }
        public DateTime AverageAnsweringTime { get; set; }
        public int ReviewQuestion { get; set; }
        public int QuestionEditCount { get; set; }
        public double PaidAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public int PaymentStatus { get; set; }
        public int DataCount { get; set; }

    }
}
