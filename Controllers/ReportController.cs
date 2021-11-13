using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kroniiapi.DTO.ReportDTO;
using kroniiapi.Services.Report;
using Microsoft.AspNetCore.Mvc;

namespace kroniiapi.Controllers
{
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Export class report detail to an excel file then return it to user
        /// </summary>
        /// <param name="classId">Id of class</param>
        /// <param name="at">Choose time to export report</param>
        /// <returns></returns>
        [HttpGet("{classId:int}")]
        public async Task<ActionResult> GenerateReport(int classId, [FromQuery] DateTimeOffset? at = null)
        {
            return null;
        }
        [HttpGet("attendance/{classId:int}")]
        public async Task<ActionResult> GetAttReport(int classId, int month = 0)
        {
            // var rs = new Dictionary<int, List<AttendanceReport>>();
            if (month == 0)
            {
                return Ok(_reportService.GetTotalAttendanceReports(classId));
            }
            else
                return Ok(_reportService.GetAttendanceReportEachMonth(classId, month));
        }

    }
}