﻿using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SForum.Data;
using SForum.Data.Models;
using SForum.Models.ApplicationUser;

namespace SForum.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IUpload _uploadService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationUser _userService;

        public ProfileController(UserManager<ApplicationUser> userManager, IApplicationUser userService,
            IUpload uploadService, IConfiguration configuration)
        {
            _userManager = userManager;
            _userService = userService;
            _uploadService = uploadService;
            _configuration = configuration;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index(string typeQuery = "rating")
        {
            var model = new ProfileListModel();

            switch (typeQuery)
            {
                case "memberSince":
                {
                    var profiles = _userService.GetAll().OrderByDescending(user => user.MemberSince).Select(u =>
                        new ProfileModel
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            Username = u.UserName,
                            ProfileImageUrl = u.ProfileImageUrl,
                            UserRating = u.Rating.ToString(),
                            MemberSince = u.MemberSince
                        });
                    model = new ProfileListModel
                    {
                        Profiles = profiles,
                        TypeQuery = typeQuery
                    };
                    break;
                }
                case "userName":
                {
                    var profiles = _userService.GetAll().OrderBy(user => user.NormalizedUserName).Select(u =>
                        new ProfileModel
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            Username = u.UserName,
                            ProfileImageUrl = u.ProfileImageUrl,
                            UserRating = u.Rating.ToString(),
                            MemberSince = u.MemberSince
                        });
                    model = new ProfileListModel
                    {
                        Profiles = profiles,
                        TypeQuery = typeQuery
                    };
                    break;
                }
                default:
                {
                    var profiles = _userService.GetAll().OrderByDescending(user => user.Rating).Select(u =>
                        new ProfileModel
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            Username = u.UserName,
                            ProfileImageUrl = u.ProfileImageUrl,
                            UserRating = u.Rating.ToString(),
                            MemberSince = u.MemberSince
                        });
                    model = new ProfileListModel
                    {
                        Profiles = profiles,
                        TypeQuery = typeQuery
                    };
                    break;
                }
            }


            return View(model);
        }

        public IActionResult Detail(string id)
        {
            var user = _userService.GetById(id);
            var userRoles = _userManager.GetRolesAsync(user).Result;
            var model = new ProfileModel
            {
                UserId = user.Id,
                Username = user.UserName,
                UserRating = user.Rating.ToString(),
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                MemberSince = user.MemberSince,
                IsAdmin = userRoles.Contains("Admin")
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var userId = _userManager.GetUserId(User);
            var connectionString = _configuration.GetConnectionString("AzureStorageAccount");
            var container = _uploadService.GetBlobContainer(connectionString, "profile-images");
            var contentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
            var filename = contentDisposition.FileName.Trim('"');
            var blockBlob = container.GetBlockBlobReference(filename);
            await blockBlob.UploadFromStreamAsync(file.OpenReadStream());
            await _userService.SetProfileImage(userId, blockBlob.Uri);
            return RedirectToAction("Detail", "Profile", new {id = userId});
        }
    }
}