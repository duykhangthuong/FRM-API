using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using kroniiapi.DB;
using kroniiapi.DB.Models;
using kroniiapi.DTO.ClassDTO;
using kroniiapi.DTO.PaginationDTO;
using Microsoft.EntityFrameworkCore;

namespace kroniiapi.Services
{
    public class ClassService : IClassService
    {
        private DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly ITraineeService _traineeService;
        // private readonly IAdminService _adminService;
        public ClassService(DataContext dataContext,
                            IMapper mapper,
                            ITraineeService traineeService
        // IAdminService adminService
        )
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _traineeService = traineeService;
            // _adminService = adminService;
        }
        /// <summary>
        /// Get Class List
        /// </summary>
        /// <param name="paginationParameter"></param>
        /// <returns> Tuple List of Class List </returns>
        public async Task<Tuple<int, IEnumerable<Class>>> GetClassList(PaginationParameter paginationParameter)
        {
            var listClass = await _dataContext.Classes.Where(c => c.IsDeactivated == false && c.ClassName.ToUpper().Contains(paginationParameter.SearchName.ToUpper())).ToListAsync();

            int totalRecords = listClass.Count();

            var rs = listClass.OrderBy(c => c.ClassId)
                     .Skip((paginationParameter.PageNumber - 1) * paginationParameter.PageSize)
                     .Take(paginationParameter.PageSize);

            return Tuple.Create(totalRecords, rs);
        }
        /// <summary>
        /// Get Class By ClassName
        /// </summary>
        /// <param name="className"></param>
        /// <returns> Class </returns>
        public async Task<Class> GetClassByClassName(string className)
        {
            return await _dataContext.Classes.Where(c => c.ClassName == className).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Get Request Deleted Class List
        /// </summary>
        /// <param name="paginationParameter"></param>
        /// <returns>Tuple List of  Class Delete Request</returns>
        public async Task<Tuple<int, IEnumerable<DeleteClassRequest>>> GetRequestDeleteClassList(PaginationParameter paginationParameter)
        {
            var listRequest = await _dataContext.DeleteClassRequests
                                    .Where(c => c.IsAccepted == null && c.Class.ClassName.ToUpper().Contains(paginationParameter.SearchName.ToUpper()))
                                    .Select(c => new DeleteClassRequest
                                    {
                                        DeleteClassRequestId = c.DeleteClassRequestId,
                                        Reason = c.Reason,
                                        CreatedAt = c.CreatedAt,
                                        IsAccepted = c.IsAccepted,
                                        AcceptedAt = c.AcceptedAt,
                                        ClassId = c.ClassId,
                                        Class = new Class
                                        {
                                            ClassId = c.ClassId,
                                            ClassName = c.Class.ClassName,
                                            Description = c.Class.Description,
                                            CreatedAt = c.Class.CreatedAt,
                                        },
                                        Admin = new Admin
                                        {
                                            AdminId = c.AdminId,
                                            Username = c.Admin.Username,
                                            Fullname = c.Admin.Fullname,
                                            AvatarURL = c.Admin.AvatarURL,
                                            Email = c.Admin.Email,
                                        }
                                    }
                                    ).ToListAsync();

            int totalRecords = listRequest.Count();

            var rs = listRequest.OrderBy(c => c.ClassId)
                     .Skip((paginationParameter.PageNumber - 1) * paginationParameter.PageSize)
                     .Take(paginationParameter.PageSize);

            return Tuple.Create(totalRecords, rs);
        }
        /// <summary>
        /// Update Deleted Class
        /// </summary>
        /// <param name="confirmDeleteClassInput"></param>
        /// <returns>1 if Success to Change & 0 if false to change & -1 if invalid & 2 if is rejected</returns>
        public async Task<int> UpdateDeletedClass(ConfirmDeleteClassInput confirmDeleteClassInput)
        {
            if (confirmDeleteClassInput.IsDeactivate == true)
            {
                var existedClass = await _dataContext.Classes.Where(i => i.ClassId == confirmDeleteClassInput.ClassId).FirstOrDefaultAsync();
                if (existedClass == null)
                {
                    return -1;
                }
                var existedRequest = await _dataContext.DeleteClassRequests.Where(d => d.DeleteClassRequestId == confirmDeleteClassInput.DeleteClassRequestId).FirstOrDefaultAsync();
                if (existedRequest == null)
                {
                    return -1;
                }
                if (existedClass.IsDeactivated == true || existedRequest.IsAccepted == true)
                {
                    return 0;
                }
                existedClass.IsDeactivated = true;
                existedClass.DeactivatedAt = DateTime.Now;
                existedRequest.IsAccepted = true;
                existedRequest.AcceptedAt = DateTime.Now;
                // Save Change 
                var rs = await _dataContext.SaveChangesAsync();
                if (rs == 2)
                {
                    return 1;
                }
            }
            else if (confirmDeleteClassInput.IsDeactivate == false)
            {
                var existedRequest = await _dataContext.DeleteClassRequests.Where(d => d.DeleteClassRequestId == confirmDeleteClassInput.DeleteClassRequestId).FirstOrDefaultAsync();
                existedRequest.IsAccepted = false;
                await _dataContext.SaveChangesAsync();
                return 2;
            }
            return -1;
        }
        public async Task<int> RejectAllOtherDeleteRequest(int deleteRequestId)
        {
            int classId = await _dataContext.DeleteClassRequests.Where(t => t.DeleteClassRequestId == deleteRequestId)
            .Select(t => t.ClassId).FirstOrDefaultAsync();
            var listRequest = await _dataContext.DeleteClassRequests.Where(t => t.ClassId == classId).ToListAsync();
            foreach (var i in listRequest)
            {
                i.IsAccepted = false;
            };
            var currentReq = await _dataContext.DeleteClassRequests.Where(t => t.DeleteClassRequestId == deleteRequestId).FirstOrDefaultAsync();
            currentReq.IsAccepted = true;
            int rs = await _dataContext.SaveChangesAsync();
            return rs;
        }
        /// <summary>
        ///  Get Deleted Class List
        /// </summary>
        /// <param name="paginationParameter"></param>
        /// <returns> Tuple List of Deleted Class </returns>
        public async Task<Tuple<int, IEnumerable<Class>>> GetDeletedClassList(PaginationParameter paginationParameter)
        {
            var listClass = await _dataContext.Classes.Where(c => c.IsDeactivated == true && c.ClassName.ToUpper().Contains(paginationParameter.SearchName.ToUpper())).ToListAsync();

            int totalRecords = listClass.Count();

            var rs = listClass.OrderBy(c => c.ClassId)
                     .Skip((paginationParameter.PageNumber - 1) * paginationParameter.PageSize)
                     .Take(paginationParameter.PageSize);

            return Tuple.Create(totalRecords, rs);
        }
        /// <summary>
        /// Get detail of a class 
        /// </summary>
        /// <param name="id">id of the class</param>
        /// <returns>if found return class and if not return 0</returns>
        public async Task<Class> GetClassDetail(int id)
        {
            var classGet = await _dataContext.Classes.Where(c => c.ClassId == id && c.IsDeactivated == false)
            .Select(c => new Class
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                StartDay = c.StartDay,
                EndDay = c.EndDay,
                IsDeactivated = c.IsDeactivated,
                DeactivatedAt = c.DeactivatedAt,
                Trainees = c.Trainees,
                AdminId = c.AdminId,
                Admin = new Admin
                {
                    AdminId = c.AdminId,
                    Fullname = c.Admin.Fullname,
                    AvatarURL = c.Admin.AvatarURL,
                    Email = c.Admin.Email,
                },
                TrainerId = c.TrainerId,
                Trainer = new Trainer
                {
                    Fullname = c.Trainer.Fullname,
                    AvatarURL = c.Trainer.AvatarURL,
                    Email = c.Trainer.Email,
                },
                RoomId = c.RoomId,
                Room = new Room
                {
                    RoomId = c.Room.RoomId,
                    RoomName = c.Room.RoomName,
                    Classes = c.Room.Classes,
                },
                ClassModules = c.ClassModules,
                Modules = c.Modules,
                DeleteClassRequests = c.DeleteClassRequests,
                Calendars = c.Calendars,
            })
            .FirstOrDefaultAsync();

            return classGet;
        }

        /// <summary>
        /// Get Trainee List in a class with pagination
        /// </summary>
        /// <param name="id">id of the class</param>
        /// <param name="paginationParameter">pagination param to get approriate trainee in a page</param>
        /// <returns>tuple list of trainee</returns>
        public async Task<Tuple<int, IEnumerable<Trainee>>> GetTraineesByClassId(int id, PaginationParameter paginationParameter)
        {

            var traineeList = await _dataContext.Trainees.Where(t => t.ClassId == id && t.Fullname.ToUpper().Contains(paginationParameter.SearchName.ToUpper())).ToListAsync();
            int totalRecords = traineeList.Count();
            var rs = traineeList.OrderBy(c => c.TraineeId)
                     .Skip((paginationParameter.PageNumber - 1) * paginationParameter.PageSize)
                     .Take(paginationParameter.PageSize);
            return Tuple.Create(totalRecords, rs);
        }

        /// <summary>
        /// Insert New Request Delete Class to db
        /// </summary>
        /// <param name="requestDeleteClassInput"></param>
        /// <returns> -1: Class is already deactivated / 0: Insert fail / 1: Insert success </returns>
        public async Task<int> InsertNewRequestDeleteClass(DeleteClassRequest deleteClassRequest)
        {
            Class c = await GetClassByClassID(deleteClassRequest.ClassId);
            // Admin admin= await _adminService.GetAdminById(deleteClassRequest.AdminId);
            var admin = _dataContext.Admins.Any(a => a.AdminId == deleteClassRequest.AdminId);

            if (c == null || admin == false)
            {
                return 0;
            }

            if (c.IsDeactivated == true)
            {
                return -1;
            }

            int rowInserted = 0;
            _dataContext.DeleteClassRequests.Add(deleteClassRequest);
            rowInserted = await _dataContext.SaveChangesAsync();
            return rowInserted;
        }

        /// <summary>
        /// Get Class By ClassID
        /// </summary>
        /// <param name="classID"></param>
        /// <returns> Class </returns>
        public async Task<Class> GetClassByClassID(int classId)
        {
            return await _dataContext.Classes.Where(c => c.ClassId == classId).FirstOrDefaultAsync();
        }

        public async Task<int> InsertNewClass(Class newClass)
        {

            if (_dataContext.Classes.Any(c => c.ClassName.Equals(newClass.ClassName)))
            {
                return -1;
            }
            int rowInserted = 0;
            _dataContext.Classes.Add(newClass);
            rowInserted = await _dataContext.SaveChangesAsync();
            return rowInserted;
        }
        /// <summary>
        /// add Class Id to Trainee model (after add new class)
        /// </summary>
        /// <param name="classId"></param>
        /// <param name="traineeIdList"></param>
        /// <returns></returns>
        public async Task AddClassIdToTrainee(int classId, ICollection<int> traineeIdList)
        {
            foreach (var traineeId in traineeIdList)
            {
                if (await _traineeService.IsTraineeHasClass(traineeId)) continue;
                var trainee = await _traineeService.GetTraineeById(traineeId);
                trainee.ClassId = classId;
            }
        }
        /// <summary>
        /// add module id and class id to class module model (after add new class)
        /// </summary>
        /// <param name="classId"></param>
        /// <param name="moduleIdList"></param>
        public async Task AddDataToClassModule(int classId, ICollection<int> moduleIdList)
        {
            foreach (var moduleId in moduleIdList)
            {
                ClassModule classModule = await _dataContext.ClassModules.Where(cm => cm.ClassId == classId && cm.ModuleId == moduleId).FirstOrDefaultAsync();
                if (classModule is not null) continue;
                classModule = new ClassModule()
                {
                    ClassId = classId,
                    ModuleId = moduleId
                };
                _dataContext.ClassModules.Add(classModule);
            }
        }
        /// <summary>
        /// Insert new class and save change
        /// </summary>
        /// <param name="newClassInput">detail of class input</param>
        /// <returns> -1: duplicate class name / -2: trainee already have class / 0: some unpredicted error / 1: insert succesfully </returns>
        public async Task<int> InsertNewClass(NewClassInput newClassInput)
        {
            var classSave = await InsertNewClassNoSave(newClassInput);
            if (classSave == -1)
            {
                return -1;
            }
            else if (classSave == -2)
            {
                return -2;
            }
            int rowInserted = 0;
            rowInserted = await SaveChange();
            var newClass = await GetClassByClassName(newClassInput.ClassName);
            await AddClassIdToTrainee(newClass.ClassId, newClassInput.TraineeIdList);
            await AddDataToClassModule(newClass.ClassId, newClassInput.ModuleIdList);
            await SaveChange();
            return rowInserted;
        }

        /// <summary>
        /// Insert class but not saving, check if trainee already have class
        /// </summary>
        /// <param name="newClassInput"></param>
        /// <returns>-1: duplicate class name / -2: trainee already have class / 1: insert succesfully</returns>
        public async Task<int> InsertNewClassNoSave(NewClassInput newClassInput)
        {
            var newClass = _mapper.Map<Class>(newClassInput);

            // Check duplicate class name
            if (_dataContext.Classes.Any(c => c.ClassName.Equals(newClass.ClassName)))
            {
                return -1;
            }

            // Check if trainee input already have class
            var traineeListId = newClassInput.TraineeIdList;
            foreach (var traineeId in traineeListId)
            {
                if (await _traineeService.IsTraineeHasClass(traineeId))
                {
                    return -2;
                }
            }

            _dataContext.Classes.Add(newClass);

            return 1;
        }

        /// <summary>
        /// save change to database
        /// </summary>
        /// <returns>number of row effeted</returns>
        public async Task<int> SaveChange()
        {
            return await _dataContext.SaveChangesAsync();
        }

        /// <summary>
        /// discard all change
        /// </summary>
        public void DiscardChanges()
        {
            _dataContext.ChangeTracker.Clear();
        }
    }
}