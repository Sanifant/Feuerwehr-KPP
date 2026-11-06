using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IUsersService
    {
        Task<ServiceResult> GetAllUsers();
        Task<ServiceResult> GetUser(string id);
        Task<ServiceResult> CreateUser(RegisterUserRequest request);
        Task<ServiceResult> ResendInvitation(string id);
        Task<ServiceResult> UpdateUser(string id, UpdateUserRequest request);
        Task<ServiceResult> DeleteUser(string id, string? currentUserId);
        Task<ServiceResult> PermanentlyDeleteUser(string id, string? currentUserId);
    }
}
