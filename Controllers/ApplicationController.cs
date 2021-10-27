using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using kroniiapi.DB.Models;
using kroniiapi.DTO;
using kroniiapi.DTO.ApplicationDTO;
using kroniiapi.DTO.PaginationDTO;
using kroniiapi.Helper.Upload;
using kroniiapi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace kroniiapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationController : ControllerBase
    {
        private readonly ITraineeService _traineeService;
        private readonly IApplicationService _applicationService;
        private readonly IMapper _mapper;
        private readonly IMegaHelper _megaHelper;
        public ApplicationController(IMapper mapper,

                                 ITraineeService traineeService,

                                 IApplicationService applicationService,
                                 IMegaHelper megaHelper
                                 )
        {
            _mapper = mapper;
            _traineeService = traineeService;
            _applicationService = applicationService;
            _megaHelper = megaHelper;
        }
        /// <summary>
        /// get application list of trainee
        /// </summary>
        /// <param name="id">trainee id</param>
        /// <param name="paginationParameter">Pagination parameters from client</param>
        /// <returns>200: application list/ 400: Not found</returns>
        [HttpGet("{traineeId:int}")]
        public async Task<ActionResult<PaginationResponse<IEnumerable<ApplicationResponse>>>> ViewApplicationList(int traineeId, [FromQuery] PaginationParameter paginationParameter)
        {
            if (await _traineeService.GetTraineeById(traineeId) == null)
            {
                return BadRequest(new ResponseDTO(404, "id not found"));
            }
            (int totalRecord, IEnumerable<ApplicationResponse> application) = await _traineeService.GetApplicationListByTraineeId(traineeId, paginationParameter);
            if (totalRecord == 0)
            {
                return BadRequest(new ResponseDTO(404, "Trainee doesn't have any application"));
            }
            return Ok(new PaginationResponse<IEnumerable<ApplicationResponse>>(totalRecord, application));
        }

        /// <summary>
        /// Trainee submit application form (mega upload)
        /// </summary>
        /// <param name="applicationInput">detail of applcation input </param>
        /// <returns>201: created</returns>
        [HttpPost]
        public async Task<ActionResult> SubmitApplicationForm([FromForm] ApplicationInput applicationInput, [FromForm] IFormFile form)
        {
            var stream = form.OpenReadStream();
            String formURL = await _megaHelper.Upload(stream, form.FileName, "ApplicationForm");
            Application app = _mapper.Map<Application>(applicationInput);
            app.ApplicationURL = formURL;
            var rs = _applicationService.InsertNewApplication(app);
            return Created(nameof(ViewApplicationList), new ResponseDTO(201, "Successfully inserted")); ;
        }

        /// <summary>
        /// get all application type
        /// </summary>
        /// <returns>all applcation type</returns>
        [HttpGet("category")]
        public async Task<ActionResult<IEnumerable<ApplicationCategoryResponse>>> ViewApplicationCategory()
        {
            var applicationTypeList = await _applicationService.GetApplicationCategoryList();
            var rs = _mapper.Map<IEnumerable<ApplicationCategoryResponse>>(applicationTypeList);
            return Ok(rs);
        }
    }
}