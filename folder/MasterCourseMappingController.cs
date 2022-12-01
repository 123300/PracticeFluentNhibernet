using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using log4net;
using Microsoft.Ajax.Utilities;
using UdvashERP.App_code;
using UdvashERP.App_Start;
using UdvashERP.Areas.Administration.Models.ViewModel;
using UdvashERP.BusinessModel.Dto;
using UdvashERP.BusinessModel.Dto.Administration;
using UdvashERP.BusinessModel.Entity.Administration;
using UdvashERP.BusinessModel.Entity.Base;
using UdvashERP.BusinessModel.Entity.UserAuth;
using UdvashERP.BusinessModel.ViewModel;
using UdvashERP.BusinessModel.ViewModel.Administration;
using UdvashERP.BusinessRules;
using UdvashERP.BusinessRules.Administration;
using UdvashERP.BusinessRules.CustomAttributes;
using UdvashERP.MessageExceptions;
using UdvashERP.Services.Administration;
using UdvashERP.Services.Helper;
using UdvashERP.Services.Students;
using UdvashERP.Services.UserAuth;

namespace UdvashERP.Areas.Administration.Controllers
{
    [UerpArea("Administration")]
    [Authorize]
    [AuthorizeAccess]
    public class MasterCourseMappingController : Controller
    {
        #region Logger
        private readonly ILog _logger = LogManager.GetLogger("AdministrationArea");
        #endregion

        #region Objects/Properties/Services/Dao & Initialization
        private readonly ICommonHelper _commonHelper;
        private readonly IUserService _userService;
        private readonly IStorageProviderService _storageProviderService;
        private readonly IMasterSubjectService _masterSubjectService;
        private readonly IMasterCourseService _masterCourseService;
        private readonly IOrganizationService _organizationService;
        private readonly IProgramService _programService;
        private readonly ISessionService _sessionService;
        private readonly ICourseSubjectService _courseSubjectService;
        private readonly IMasterCourseMappingService _masterCourseMappingService;
        private readonly ICourseService _courseService;
        private List<UserMenu> _userMenu;

        public MasterCourseMappingController()
        {
            var nHsession = NHibernateSessionFactory.OpenSession();
            _userService = new UserService(nHsession);
            _storageProviderService = new StorageProviderService(nHsession);
            _masterSubjectService = new MasterSubjectService(nHsession);
            _masterCourseService = new MasterCourseService(nHsession);
            _organizationService = new OrganizationService(nHsession);
            _courseSubjectService = new CourseSubjectService(nHsession);
            _masterCourseMappingService = new MasterCourseMappingService(nHsession);
            _programService = new ProgramService(nHsession);
            _sessionService = new SessionService(nHsession);
            _courseService = new CourseService(nHsession);
            _commonHelper = new CommonHelper();
        }
        #endregion

        #region Operational Function
        #region Details Operation
        [HttpGet]
        public ActionResult Details(long courseId)
        {
            var masterCourseMappingDtoList = new List<MasterCourseMappingDetailsDto>();
            try
            {
                var courseList = _masterCourseMappingService.LoadMasterCourseMappingDetails(courseId);

                foreach (var item in courseList)
                {
                    if (item.MasterCourse != null && item.MasterSubject != null && item.SubjectName != null)
                    {
                        item.MasterChapter = item.MasterChapter == null ? "All" : item.MasterChapter;
                    }
                    else
                    {
                        item.MasterChapter = "-";
                    }

                    item.SubjectName = item.SubjectName == null ? "-" : item.SubjectName;
                    item.MasterCourse = item.MasterCourse == null ? "-" : item.MasterCourse;
                    item.MasterSubject = item.MasterSubject == null ? "-" : item.MasterSubject;

                    masterCourseMappingDtoList.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                ViewBag.ErrorMessage = WebHelper.CommonErrorMessage;
            }

            return View(masterCourseMappingDtoList);
        }
        #endregion

        #region Save Operation
        public ActionResult MasterCourseMappingAddOrEdit(long courseId = 0)
        {
            string errorMessage = "";
            ViewBag.OrganizationList = Enumerable.Empty<SelectListItem>();
            //ViewBag.ProgramSelectList = Enumerable.Empty<SelectListItem>();
            //ViewBag.SessionSelectList = Enumerable.Empty<SelectListItem>();
            //ViewBag.CourseSelectList = Enumerable.Empty<SelectListItem>();

            try
            {
                _userMenu = (List<UserMenu>)ViewBag.UserMenu;
                var organizationList = _organizationService.LoadAuthorizedOrganization(_userMenu).OrderBy(x => x.Rank).ToList();
                long organizationId = 0;
                if (courseId > 0)
                {
                    var course = _courseService.GetCourse(courseId);

                    if (course == null)
                    {
                        throw new InvalidDataException("Invalid course found");
                    }
                    organizationId = course.Program.Organization.Id;
                    var programId = course.Program.Id;
                    var sessionId = course.RefSession.Id;
                    var programList = _programService.LoadAuthorizedProgram(_userMenu, _commonHelper.ConvertIdToList(organizationId), null, null, null, null);
                    var sessionList = _sessionService.LoadAuthorizedSession(_userMenu, _commonHelper.ConvertIdToList(organizationId), _commonHelper.ConvertIdToList(programId));
                    var courseList = _courseService.LoadCourse(_commonHelper.ConvertIdToList(organizationId), _commonHelper.ConvertIdToList(programId), _commonHelper.ConvertIdToList(sessionId), null);

                    if (programList != null && programList.Any())
                    {
                        ViewBag.ProgramSelectList = new SelectList(programList, "Id", "Name", programId);
                    }
                    if (sessionList != null && sessionList.Any())
                    {
                        ViewBag.SessionSelectList = new SelectList(sessionList, "Id", "Name", sessionId);
                    }
                    if (courseList != null && courseList.Any())
                    {
                        ViewBag.CourseSelectList = new SelectList(courseList, "Id", "Name", courseId);
                    }

                    ViewBag.CourseId = course.Id;
                }

                ViewBag.OrganizationList = new SelectList(organizationList, "Id", "ShortName", organizationId);
            }
            catch (InvalidDataException ex)
            {
                errorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            if (string.IsNullOrWhiteSpace(errorMessage) == false)
            {
                ViewBag.ErrorMessage = errorMessage;
            }

            return View();
        }

        [HttpPost]
        public ActionResult MasterCourseMappingAddOrEdit(MasterCourseMappingViewModel masterCourseMappingViewModel)
        {
            string message = "";
            bool isSuccess = false;
            try
            {
                _masterCourseMappingService.SaveOrUpdate(masterCourseMappingViewModel);

                message = "Course mapping successfully done";
                isSuccess = true;
            }
            catch (InvalidDataException ex)
            {
                message = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                message = WebHelper.CommonErrorMessage;
            }

            return Json(new { Message = message, IsSuccess = isSuccess });
        }
        #endregion
        #endregion

        #region Index/Manage Page
        public ActionResult ManageMasterCourseMapping()
        {
            ViewBag.PaymentStatusList = new SelectList(GetPaymentStatus(), "Value", "Key");
            return View();
            //try
            //{
            //    string errorMessage = "";
            //    var organizationList = new List<Organization>();
            //    try
            //    {
            //        _userMenu = (List<UserMenu>)ViewBag.UserMenu;
            //        var organizations = _organizationService.LoadAuthorizedOrganization(_userMenu);
            //        ViewBag.OrganizationList = new SelectList(organizations.ToList(), "Id", "ShortName");
            //        List<long> organizationIdList = new List<long>();

            //        ViewBag.StatusList = new SelectList(_commonHelper.GetStatus(), "Value", "Key");
            //        ViewBag.OnlinestatusList = new SelectList(GetOnlineStatus(), "Value", "Key");
            //    }
            //    catch (InvalidDataException ex)
            //    {
            //        errorMessage = ex.Message;
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.Error(ex);
            //    }

            //    if (string.IsNullOrWhiteSpace(errorMessage) == false)
            //    {
            //        ViewBag.ErrorMessage = errorMessage;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.Error(ex.Message, ex);
            //    ViewBag.ErrorMessage = WebHelper.SetExceptionMessage(ex);
            //}

            //ViewBag.PageSize = Constants.PageSize;

            //return View();
        }

        private Dictionary<string, int> GetPaymentStatus(bool? includeAll = null)
        {
            try
            {
                var status = new Dictionary<string, int>();
                if (includeAll != null && includeAll == true)
                {
                    status.Add("All", 0);
                }
                status.Add("Due", BaseEntity<Course>.EntityStatus.Active);
                status.Add("Paid", BaseEntity<Course>.EntityStatus.Inactive);
                return status;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPost]
        public JsonResult MasterCourseMappingDataTable(int draw, int start, int length,string keyword = "", int paymentStatus = 1)
        {
            long recordsTotal = 0;
            long recordsFiltered = recordsTotal;
            var data = new List<object>();

            try
            {
                _userMenu = (List<UserMenu>)ViewBag.UserMenu;

                string orderBy = "a.CreationDate";
                string orderDir = "Asc";

                var str = new List<string>();
                if (paymentStatus == 1)
                {
                    IList<DemoPaymentDto> demoPaymentDtos = _masterCourseMappingService.LoadDemoDtos(draw, start, length, keyword, paymentStatus);
                    recordsTotal = demoPaymentDtos != null && demoPaymentDtos.Any() == true ? demoPaymentDtos.FirstOrDefault().DataCount : 0;
                    recordsFiltered = recordsTotal;
                    int sl = start + 1;

                    foreach (var item in demoPaymentDtos)
                    {
                        
                        str.Add(CommonHelper.DisplayText(item.Organization));
                        str.Add(CommonHelper.DisplayText("--"));
                        str.Add(CommonHelper.DisplayText(item.TotalEffectiveTime));
                        str.Add(CommonHelper.DisplayText(item.NewQuestionAnswered));
                        str.Add(CommonHelper.DisplayText(item.AverageAnsweringTime));
                        str.Add(CommonHelper.DisplayText(item.ReviewQuestion));
                        str.Add(CommonHelper.DisplayText(item.QuestionEditCount));

                        data.Add(str);
                        sl++;
                    }
                }
                else
                {
                    IList<DemoPaymentDtoTwo> demoPaymentDtos = _masterCourseMappingService.LoadDemoTwoDtos(draw, start, length, keyword, paymentStatus);
                    recordsTotal = demoPaymentDtos != null && demoPaymentDtos.Any() == true ? demoPaymentDtos.FirstOrDefault().DataCount : 0;
                    recordsFiltered = recordsTotal;
                    int sl = start + 1;

                    foreach (var item in demoPaymentDtos)
                    {
                        str.Add(CommonHelper.DisplayText(item.Organization));
                        str.Add(CommonHelper.DisplayText(item.DateRange));
                        str.Add(CommonHelper.DisplayText(item.TotalEffectiveTime));
                        str.Add(CommonHelper.DisplayText(item.NewQuestionAnswered));
                        str.Add(CommonHelper.DisplayText(item.AverageAnsweringTime));
                        str.Add(CommonHelper.DisplayText(item.ReviewQuestion));
                        str.Add(CommonHelper.DisplayText(item.QuestionEditCount));
                        str.Add(CommonHelper.DisplayText(item.PaidAmount));
                        str.Add(CommonHelper.DisplayText(item.PaymentDate));

                        data.Add(str);
                        sl++;
                    }

                    data.Add(str);
                }
                
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return Json(new { draw, recordsTotal, recordsFiltered, start, length, data });
        }

        //[HttpPost]
        //public JsonResult MasterCourseMappingDataTable(int draw, int start, int length, long organizationId = 0, long programId = 0, long sessionId = 0, long courseId = 0, string keyword = "", int status = 0, int onlineStatus = -1)
        //{
        //    long recordsTotal = 0;
        //    long recordsFiltered = recordsTotal;
        //    var data = new List<object>();

        //    try
        //    {
        //        _userMenu = (List<UserMenu>)ViewBag.UserMenu;

        //        string orderBy = "a.CreationDate";
        //        string orderDir = "Asc";

        //        IList<ManageMasterCourseMappingDto> manageMasterCourseDtos = _masterCourseMappingService.
        //            LoadMasterCourseDoubtMappingDtos(_userMenu, start, length, orderBy, orderDir, organizationId, programId, courseId, sessionId, status, onlineStatus);
        //        recordsTotal = manageMasterCourseDtos != null && manageMasterCourseDtos.Any() == true ? manageMasterCourseDtos.FirstOrDefault().DataCount : 0;
        //        recordsFiltered = recordsTotal;
        //        int sl = start + 1;
        //        foreach (var item in manageMasterCourseDtos)
        //        {
        //            var str = new List<string>();
        //            str.Add(sl.ToString());
        //            str.Add(CommonHelper.DisplayText(item.Organization));
        //            str.Add(CommonHelper.DisplayText(item.Program));
        //            str.Add(CommonHelper.DisplayText(item.Session));
        //            str.Add(CommonHelper.DisplayText(item.Course));
        //            //str.Add(CommonHelper.DisplayText("<a href=\"#\" target=\"_blank\" style=\"text-decoration: none;\">View</a>"));
        //            //str.Add(CommonHelper.DisplayText("<a href=\"#\" target=\"_blank\" style=\"text-decoration: none;\">View</a>"));
        //            str.Add(CommonHelper.DisplayText(item.ModifiedBy, "-"));
        //            str.Add(CommonHelper.GetReportCommonDateTimeFormat(item.ModificationDate));
        //            str.Add(CommonHelper.DisplayText(item.CreatedBy, "-"));
        //            str.Add(CommonHelper.GetReportCommonDateTimeFormat(item.CreationDate));
        //            //str.Add(StatusTypeText.GetStatusText(item.Status));
        //            str.Add($@"<a href='{Url.Action("MasterCourseMappingAddOrEdit")}?courseId={item.CourseId}' class='glyphicon glyphicon-pencil'></a>&nbsp;&nbsp; <a href='{Url.Action("Details")}?courseId={item.CourseId}' class='glyphicon glyphicon-th-list'></a> </a>&nbsp;&nbsp; <a id='{item.CourseId}' href='#' class='glyphicon glyphicon-trash'></a>");

        //            data.Add(str);
        //            sl++;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error(ex);
        //    }

        //    return Json(new { draw, recordsTotal, recordsFiltered, start, length, data });
        //}
        #endregion

        #region Helper Function
        private void GetStatusText()
        {
            ViewBag.STATUSTEXT = _commonHelper.GetStatus();
        }

        private Dictionary<string, int> GetOnlineStatus(bool? includeAll = null)
        {
            try
            {
                var onlineStatus = new Dictionary<string, int>();
                if (includeAll != null && includeAll == true)
                {
                    onlineStatus.Add("All", 0);
                }
                onlineStatus.Add("Yes", 1);
                onlineStatus.Add("No", 0);
                return onlineStatus;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #endregion

        #region Ajax Function
        #region Delete Operation
        [HttpPost]
        public ActionResult Delete(long courseId)
        {
            bool isSuccess = false;
            string message;
            try
            {
                _masterCourseMappingService.Delete(courseId);
                isSuccess = true;
                message = "Master course mapping delete successful";
            }
            catch (InvalidDataException ex)
            {
                message = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                message = WebHelper.SetExceptionMessage(ex);
            }

            return Json(new Response(isSuccess, message));
        }
        #endregion

        [HttpPost]
        public ActionResult GetCourseSubjectAssignPartialView(long courseId)
        {
            IList<NameValueDto> masterCourseList = new List<NameValueDto>();
            var masterCourseMappingList = new List<MasterCourseMapping>();
            var mappingViewModelList = new List<AddMasterCourseMappingViewModel>();
            try
            {
                masterCourseList = _masterCourseService.LoadMasterCourseNameValueList(isActive: true);
                masterCourseMappingList = _masterCourseMappingService.LoadMasterCourseMapping(courseId);

                var mappingListGroupDictionary = masterCourseMappingList.GroupBy(x => x.CourseSubject.Id).ToDictionary(x => x.Key, x => x.ToList());

                var oldMasterCourseList = masterCourseMappingList.Select(x => x.MasterCourse).ToList();
                var oldMasterSubjectList = masterCourseMappingList.Select(x => x.MasterCourse).ToList();
                var oldMasterChapterList = masterCourseMappingList.Select(x => x.MasterChapter).ToList();
                oldMasterChapterList = oldMasterChapterList.Where(x => x != null).ToList();
                var course = _courseService.GetCourse(courseId);
                if (course == null)
                    throw new InvalidDataException("Course not found");

                foreach (var courseSubject in course.CourseSubjects)
                {
                    bool isDeletedCourseSubject = courseSubject.Status != CourseSubject.EntityStatus.Active;

                    if (isDeletedCourseSubject && masterCourseMappingList.Any(x => x.CourseSubject.Subject.Id == courseSubject.Subject.Id) == false)
                        continue;

                    var mappingViewModel = new AddMasterCourseMappingViewModel();
                    mappingViewModel.CourseSubjectId = courseSubject.Id;
                    mappingViewModel.SubjectName = courseSubject.Subject.Name;

                    mappingListGroupDictionary.TryGetValue(courseSubject.Id, out List<MasterCourseMapping> mappingList);
                    var mappingRowList = new List<AddMasterCourseMappingRowViewModel>();
                    if (mappingList != null && mappingList.Any())
                    {
                        foreach (var mapping in mappingList)
                        {
                            var chapterList = mapping.MasterSubject.MasterChapterList.Where(x => x.Status == MasterChapter.EntityStatus.Active).ToList();
                            chapterList.Insert(0, new MasterChapter { Id = 0, Name = "All" });
                            mappingRowList.Add(
                                new AddMasterCourseMappingRowViewModel
                                {
                                    MasterCourseSelectList = new SelectList(masterCourseList, "Value", "Name", mapping.MasterCourse.Id),
                                    MasterSubjectSelectList = new SelectList(mapping.MasterCourse.MasterCourseSubjectList.Select(x => x.MasterSubject).ToList(), "Id", "Name", mapping.MasterSubject.Id),
                                    MasterChapterSelectList = new SelectList(chapterList, "Id", "Name", mapping.MasterChapter?.Id ?? 0),
                                    IsDeletedSubject = isDeletedCourseSubject
                                });
                        }
                    }
                    else
                    {
                        mappingRowList.Add(
                                new AddMasterCourseMappingRowViewModel
                                {
                                    MasterCourseSelectList = new SelectList(masterCourseList, "Value", "Name"),
                                    MasterSubjectSelectList = new SelectList(new List<NameValueDto>(), "Value", "Name"),
                                    MasterChapterSelectList = new SelectList(new List<NameValueDto>(), "Value", "Name"),
                                });
                    }

                    mappingViewModel.MappingRowList = mappingRowList;
                    mappingViewModelList.Add(mappingViewModel);
                }
            }
            catch (InvalidDataException)
            {

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            ViewBag.MasterCourseList = masterCourseList;
            ViewBag.MappingViewModelList = mappingViewModelList;

            return PartialView("Partial/_CourseSubjectAssignPartialView");
        }
        #endregion
    }
}
