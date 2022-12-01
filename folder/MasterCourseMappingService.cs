using HtmlAgilityPack;
using log4net;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdvashERP.BusinessModel.Dto;
using UdvashERP.BusinessModel.Dto.Administration;
using UdvashERP.BusinessModel.Entity.Administration;
using UdvashERP.BusinessModel.Entity.Exam;
using UdvashERP.BusinessModel.Entity.UserAuth;
using UdvashERP.BusinessModel.ViewModel.Administration;
using UdvashERP.BusinessRules;
using UdvashERP.Dao.Administration;
using UdvashERP.MessageExceptions;
using UdvashERP.Services.Base;
using UdvashERP.Services.Helper;

namespace UdvashERP.Services.Administration
{
    public interface IMasterCourseMappingService : IBaseService
    {
        #region Operational Function
        void SaveOrUpdate(MasterCourseMappingViewModel masterCourseMappingViewModel);
        void Delete(long courseId);
        #endregion

        #region Single Instance Loading Function
        #endregion

        #region List Loading Function
        List<MasterCourseMapping> LoadMasterCourseMapping(long courseId);
        IList<ManageMasterCourseMappingDto> LoadMasterCourseDoubtMappingDtos(List<UserMenu> userMenus, int start, int length, string orderBy, string orderDir,
            long organizationId, long programId, long courseId, long sessionId, int status, int onlineStatus);
        IList<DemoPaymentDto> LoadDemoDtos(int draw, int start, int length, string keyword = "", int paymentStatus = 1);
        IList<DemoPaymentDtoTwo> LoadDemoTwoDtos(int draw, int start, int length, string keyword = "", int paymentStatus = 1);
        List<MasterCourseMappingDetailsDto> LoadMasterCourseMappingDetails(long courseId);
        #endregion

        #region Others Function
        #endregion

        #region Online Portal Function
        List<MasterCourseMappingOnlinePortalDto> LoadMasterCourseMappingOnlinePortalDto(DateTime? lastModificationDateTime = null, int? status = null, List<long> masterCourseIds = null, List<long> masterSubjectIds = null, List<long> masterChapterIds = null, List<long> masterCourseMappingIds = null, List<long> courseIds = null, List<long> courseSubjectIds = null);
        #endregion

        #region public API
        #endregion
    }

    public class MasterCourseMappingService : BaseService, IMasterCourseMappingService
    {
        #region Logger
        private readonly ILog _logger = LogManager.GetLogger("AdministrationService");
        #endregion

        #region Properties & Object Initialization
        private readonly IMasterSubjectDao _masterSubjectDao;
        private readonly IMasterCourseDao _masterCourseDao;
        private readonly ICourseSubjectDao _couseSubjectDao;
        private readonly IMasterChapterDao _masterChapterDao;
        private readonly ICourseDao _courseDao;
        private readonly IMasterCourseMappingDao _masterCourseMappingDao;
        private readonly ICommonHelper _commonHelper;

        public MasterCourseMappingService(ISession session)
        {
            Session = session;
            _masterSubjectDao = new MasterSubjectDao() { Session = session };
            _masterCourseDao = new MasterCourseDao() { Session = session };
            _couseSubjectDao = new CourseSubjectDao() { Session = session };
            _masterChapterDao = new MasterChapterDao() { Session = session };
            _courseDao = new CourseDao() { Session = session };
            _masterCourseMappingDao = new MasterCourseMappingDao() { Session = session };
            _commonHelper = new CommonHelper();
        }
        #endregion

        #region Operational Function
        public void SaveOrUpdate(MasterCourseMappingViewModel masterCourseMappingViewModel)
        {
            try
            {
                #region Validation
                if (masterCourseMappingViewModel == null)
                    throw new InvalidDataException("No master course mapping found");

                var courseSubjectMappingDetails = masterCourseMappingViewModel.MappingDetails;
                if (courseSubjectMappingDetails == null || courseSubjectMappingDetails.Any() == false)
                    throw new InvalidDataException("No master course mapping found");

                courseSubjectMappingDetails = courseSubjectMappingDetails.Where(x => x.CourseSubjectId > 0 && x.MasterCourseId > 0 && x.MasterSubjectId > 0).ToList();

                var chapterIdsGroupedList = courseSubjectMappingDetails.GroupBy(x => new { x.CourseSubjectId, x.MasterSubjectId }).Select(g => g.Select(x => x.MasterChapterId).ToList());

                foreach (var chapterIdsGrouped in chapterIdsGroupedList)
                {
                    if (chapterIdsGrouped.Count > 1 && chapterIdsGrouped.Any(x => x == SelectionType.SelectAll))
                        throw new InvalidDataException("You have already selectd all chapters");
                }

                var courseId = masterCourseMappingViewModel.CourseId;
                var course = _courseDao.LoadById(courseId);

                if (course == null)
                    throw new InvalidDataException("Course not found");

                var masterCourseIds = courseSubjectMappingDetails.Select(x => x.MasterCourseId).Distinct().ToList();
                var masterCourseList = _masterCourseDao.LoadMasterCourse(masterCourseIds);

                if (masterCourseList == null || masterCourseList.Any() == false)
                    throw new InvalidDataException("Master Course not found");

                var courseSubejctIds = courseSubjectMappingDetails.Select(x => x.CourseSubjectId).Distinct().ToList();
                var courseSubjectList = _couseSubjectDao.LoadCourseSubjectByIdList(courseId, courseSubejctIds);

                if (courseSubjectList == null || courseSubjectList.Any() == false)
                    throw new InvalidDataException("Course subject not found");

                var masterSubjectIds = courseSubjectMappingDetails.Select(x => x.MasterSubjectId).Distinct().ToList();
                var masterSubjectList = _masterSubjectDao.LoadMasterSubject(masterSubjectIds);

                if (masterSubjectList == null || masterSubjectList.Any() == false)
                    throw new InvalidDataException("Master subject not found");

                var masterChapterIds = courseSubjectMappingDetails.Select(x => x.MasterChapterId).Distinct().ToList();
                var masterChapterList = _masterChapterDao.LoadMasterChapter(masterChapterIds);
                #endregion

                var masterCourseMappingSaveList = new List<MasterCourseMapping>();
                var masterCourseMappingUpdateList = new List<MasterCourseMapping>();
                var masterCourseMappingList = _masterCourseMappingDao.LoadMasterCourseMapping(masterCourseMappingViewModel.CourseId, false);

                long currentUserId = GetCurrentUserId();

                foreach (var masterCourseMapping in masterCourseMappingList)
                {
                    if (masterCourseMapping.Status == MasterCourseMapping.EntityStatus.Active)
                    {
                        masterCourseMapping.Status = MasterCourseMapping.EntityStatus.Delete;
                        masterCourseMappingUpdateList.Add(masterCourseMapping);
                    }
                    else if (masterCourseMapping.Status == MasterCourseMapping.EntityStatus.Delete && courseSubjectMappingDetails.Any(x => x.CourseSubjectId == masterCourseMapping.CourseSubject.Id && x.MasterCourseId == masterCourseMapping.MasterCourse.Id && x.MasterSubjectId == masterCourseMapping.MasterSubject.Id && x.MasterChapterId == (masterCourseMapping.MasterChapter?.Id ?? 0)) && masterCourseMappingUpdateList.Any(x => x.CourseSubject.Id == masterCourseMapping.CourseSubject.Id && x.MasterCourse.Id == masterCourseMapping.MasterCourse.Id && x.MasterSubject.Id == masterCourseMapping.MasterSubject.Id && (x.MasterChapter?.Id ?? 0) == (masterCourseMapping.MasterChapter?.Id ?? 0)) == false)
                    {
                        masterCourseMapping.Status = MasterCourseMapping.EntityStatus.Active;
                        masterCourseMappingUpdateList.Add(masterCourseMapping);
                    }
                }

                foreach (var details in masterCourseMappingViewModel.MappingDetails)
                {
                    var courseSubject = courseSubjectList.FirstOrDefault(x => x.Id == details.CourseSubjectId);
                    var masterCourse = masterCourseList.FirstOrDefault(x => x.Id == details.MasterCourseId);
                    var masterSubject = masterSubjectList.FirstOrDefault(x => x.Id == details.MasterSubjectId);
                    var masterChapter = masterChapterList.FirstOrDefault(x => x.Id == details.MasterChapterId);

                    if (courseSubject == null || masterCourse == null || masterSubject == null)
                        continue;

                    if (masterCourseMappingUpdateList.Any(x => x.Status == MasterCourseMapping.EntityStatus.Active && x.CourseSubject.Id == details.CourseSubjectId && x.MasterCourse.Id == details.MasterCourseId && x.MasterSubject.Id == details.MasterSubjectId && (x.MasterChapter?.Id ?? 0) == (details.MasterChapterId)))
                        continue;

                    var masterCourseMapping = new MasterCourseMapping
                    {
                        Status = MasterCourseMapping.EntityStatus.Active,
                        Course = course,
                        CourseSubject = courseSubject,
                        MasterCourse = masterCourse,
                        MasterSubject = masterSubject,
                        MasterChapter = masterChapter
                    };

                    masterCourseMappingSaveList.Add(masterCourseMapping);
                }

                using (var transaction = Session.BeginTransaction())
                {
                    try
                    {
                        foreach (var masterCourseMapping in masterCourseMappingSaveList)
                        {
                            _masterCourseMappingDao.Save(masterCourseMapping);
                            var mappingLog = GetMasterCourseMappingLog(masterCourseMapping, currentUserId);
                            Session.Save(mappingLog);
                        }

                        foreach (var masterCourseMapping in masterCourseMappingUpdateList)
                        {
                            _masterCourseMappingDao.Update(masterCourseMapping);
                            var mappingLog = GetMasterCourseMappingLog(masterCourseMapping, currentUserId);
                            Session.Save(mappingLog);
                        }

                        transaction.Commit();
                    }
                    catch (InvalidDataException)
                    {
                        if (transaction != null && transaction.IsActive)
                            transaction.Rollback();
                        throw;
                    }
                    catch (Exception)
                    {
                        if (transaction != null && transaction.IsActive)
                            transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public void Delete(long courseId)
        {
            try
            {
                _masterCourseMappingDao.DeleteByCourseId(courseId, GetCurrentUserId());
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
        }
        #endregion

        #region Single Instance Loading Function
        #endregion

        #region List Loading Function
        public List<MasterCourseMapping> LoadMasterCourseMapping(long courseId)
        {
            try
            {
                return _masterCourseMappingDao.LoadMasterCourseMapping(courseId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public IList<ManageMasterCourseMappingDto> LoadMasterCourseDoubtMappingDtos(List<UserMenu> userMenus, int start, int length, string orderBy, string orderDir,
            long organizationId, long programId, long courseId, long sessionId, int status, int onlineStatus)
        {
            try
            {
                var organizationIdList = _commonHelper.ConvertIdToList(organizationId);
                var programIdList = _commonHelper.ConvertIdToList(programId);
                var sessionIdList = _commonHelper.ConvertIdToList(sessionId);
                var courseIdList = _commonHelper.ConvertIdToList(courseId);

                var authorizeOrganizationIdList = AuthHelper.LoadOrganizationIdList(userMenus, CommonHelper.ConvertSelectedAllIdList(organizationIdList));

                var authorizeProgramIdList = AuthHelper.LoadProgramIdList(userMenus, authorizeOrganizationIdList, null, CommonHelper.ConvertSelectedAllIdList(programIdList));

                if (authorizeOrganizationIdList == null || authorizeOrganizationIdList.Any() == false)
                    throw new InvalidDataException("There is no authorized organization found here.");

                if (authorizeProgramIdList == null || authorizeProgramIdList.Any() == false)
                    throw new InvalidDataException("There is no authorized program found here.");

                return _masterCourseMappingDao.LoadMasterCourseDoubtMapping(start, length, orderBy, orderDir, organizationIdList, programIdList, courseIdList, sessionIdList,
                    status, onlineStatus);
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        public IList<DemoPaymentDto> LoadDemoDtos(int draw, int start, int length, string keyword = "", int paymentStatus = 1)
        {
            var payments = CustomPayment();
            return payments.Where(c => c.PaymentStatus == paymentStatus).ToList();
        }

        private IList<DemoPaymentDto> CustomPayment()
        {
            var demoPaymentDto = new List<DemoPaymentDto>
            {
                new DemoPaymentDto{ Id = 1, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus =1, TotalEffectiveTime = DateTime.Now  },
                new DemoPaymentDto{ Id = 2, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Unmesh", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus = -1, TotalEffectiveTime = DateTime.Now  },
                new DemoPaymentDto{ Id = 3, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus =1, TotalEffectiveTime = DateTime.Now  },
                new DemoPaymentDto{ Id = 3, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus = -1, TotalEffectiveTime = DateTime.Now  },
                new DemoPaymentDto{ Id = 3, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus =1, TotalEffectiveTime = DateTime.Now  },
            };
            return demoPaymentDto;
        }

        public IList<DemoPaymentDtoTwo> LoadDemoTwoDtos(int draw, int start, int length, string keyword = "", int paymentStatus = 1)
        {
            var payments = CustomPaymentTwo();
            return payments.Where(c => c.PaymentStatus == paymentStatus).ToList();
        }

        private IList<DemoPaymentDtoTwo> CustomPaymentTwo()
        {
            var demoPaymentDto = new List<DemoPaymentDtoTwo>
            {
                new DemoPaymentDtoTwo{ Id = 1, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus =1, TotalEffectiveTime = DateTime.Now, DateRange = DateTime.Now  },
                new DemoPaymentDtoTwo{ Id = 2, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Unmesh", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus = -1, TotalEffectiveTime = DateTime.Now,DateRange = DateTime.Now  },
                new DemoPaymentDtoTwo{ Id = 3, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus =1, TotalEffectiveTime = DateTime.Now,DateRange = DateTime.Now  },
                new DemoPaymentDtoTwo{ Id = 3, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus = -1, TotalEffectiveTime = DateTime.Now,DateRange = DateTime.Now  },
                new DemoPaymentDtoTwo{ Id = 3, AverageAnsweringTime = DateTime.Now, NewQuestionAnswered = 10, Organization = "Udvash", PaidAmount = 124.5, PaymentDate = DateTime.Now, QuestionEditCount = 10, ReviewQuestion = 12, PaymentStatus =1, TotalEffectiveTime = DateTime.Now,DateRange = DateTime.Now  },
            };
            return demoPaymentDto;
        }

        public List<MasterCourseMappingDetailsDto> LoadMasterCourseMappingDetails(long courseId)
        {
            try
            {
                return _masterCourseMappingDao.LoadMasterCourseMappingDetails(courseId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }
        #endregion

        #region Others Function        
        #endregion

        #region Online Portal Function
        public List<MasterCourseMappingOnlinePortalDto> LoadMasterCourseMappingOnlinePortalDto(DateTime? lastModificationDateTime = null, int? status = null, List<long> masterCourseIds = null, List<long> masterSubjectIds = null, List<long> masterChapterIds = null, List<long> masterCourseMappingIds = null, List<long> courseIds = null, List<long> courseSubjectIds = null)
        {
            try
            {
                return _masterCourseMappingDao.LoadMasterCourseMappingOnlinePortalDto(lastModificationDateTime, status, masterCourseIds, masterSubjectIds, masterChapterIds, masterCourseMappingIds, courseIds, courseSubjectIds);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }
        #endregion

        #region public API
        #endregion

        #region Helper Functions  
        MasterCourseMappingLog GetMasterCourseMappingLog(MasterCourseMapping masterCourseMapping, long currentUserId)
        {

            return new MasterCourseMappingLog
            {
                CreationDate = DateTime.Now,
                CreateBy = currentUserId,
                Status = masterCourseMapping.Status,
                MasterCourseMappingId = masterCourseMapping.Id,
                CourseId = masterCourseMapping.Course.Id,
                CourseSubjectId = masterCourseMapping.CourseSubject.Id,
                MasterCourseId = masterCourseMapping.MasterCourse.Id,
                MasterSubjectId = masterCourseMapping.MasterSubject.Id,
                MasterChapterId = masterCourseMapping.MasterChapter?.Id ?? null,
            };
        }
        #endregion
    }
}
