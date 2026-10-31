using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using Microsoft.EntityFrameworkCore;

using De.Openelp.Feuerwehr.Server.Services;

namespace De.Openelp.Feuerwehr.Server.Services;

public interface IFireDepartmentService
{
    Task<ServiceResult> Get();
    Task<ServiceResult> Add(FireDepartmentDto fireDepartmentDto);
    Task<ServiceResult> Get(int id);
}
