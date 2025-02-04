﻿using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using StarForum.Infrastructure.Entities;
using StarForum.Infrastructure.Services;
using StarForum.WebApi.Models.ApplicationUser;

namespace StarForum.WebApi.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IUpload _uploadService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationUser _userService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProfileController(UserManager<ApplicationUser> userManager, IApplicationUser userService,
        IUpload uploadService, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _userService = userService;
        _uploadService = uploadService;
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Index(string typeQuery = "rating", string oldTypeQuery = "")
    {
        IEnumerable<ProfileModel> profiles;
        switch (typeQuery)
        {
            case "memberSince" when oldTypeQuery == typeQuery:
                profiles = _userService.GetAll().OrderBy(user => user.MemberSince).Select(u =>
                    new ProfileModel
                    {
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        ProfileImageUrl = u.ProfileImageUrl,
                        UserRating = u.Rating.ToString(),
                        MemberSince = u.MemberSince
                    });
                oldTypeQuery = "0";
                break;
            case "memberSince":
                profiles = _userService.GetAll().OrderByDescending(user => user.MemberSince).Select(u =>
                    new ProfileModel
                    {
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        ProfileImageUrl = u.ProfileImageUrl,
                        UserRating = u.Rating.ToString(),
                        MemberSince = u.MemberSince
                    });
                oldTypeQuery = typeQuery;
                break;
            case "userName" when oldTypeQuery == typeQuery:
                profiles = _userService.GetAll().OrderByDescending(user => user.NormalizedUserName).Select(u =>
                    new ProfileModel
                    {
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        ProfileImageUrl = u.ProfileImageUrl,
                        UserRating = u.Rating.ToString(),
                        MemberSince = u.MemberSince
                    });
                oldTypeQuery = "0";
                break;
            case "userName":
                profiles = _userService.GetAll().OrderBy(user => user.NormalizedUserName).Select(u =>
                    new ProfileModel
                    {
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        ProfileImageUrl = u.ProfileImageUrl,
                        UserRating = u.Rating.ToString(),
                        MemberSince = u.MemberSince
                    });
                oldTypeQuery = typeQuery;
                break;
            case "email" when oldTypeQuery == typeQuery:
                profiles = _userService.GetAll().OrderByDescending(user => user.Email).Select(u =>
                    new ProfileModel
                    {
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        ProfileImageUrl = u.ProfileImageUrl,
                        UserRating = u.Rating.ToString(),
                        MemberSince = u.MemberSince
                    });
                oldTypeQuery = "0";
                break;
            case "email":
                profiles = _userService.GetAll().OrderBy(user => user.Email).Select(u =>
                    new ProfileModel
                    {
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        ProfileImageUrl = u.ProfileImageUrl,
                        UserRating = u.Rating.ToString(),
                        MemberSince = u.MemberSince
                    });
                oldTypeQuery = typeQuery;
                break;
            default:
            {
                if (oldTypeQuery == typeQuery)
                {
                    profiles = _userService.GetAll().OrderBy(user => user.Rating).Select(u =>
                        new ProfileModel
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            Username = u.UserName,
                            ProfileImageUrl = u.ProfileImageUrl,
                            UserRating = u.Rating.ToString(),
                            MemberSince = u.MemberSince
                        });
                    oldTypeQuery = "0";
                }
                else
                {
                    profiles = _userService.GetAll().OrderByDescending(user => user.Rating).Select(u =>
                        new ProfileModel
                        {
                            UserId = u.Id,
                            Email = u.Email,
                            Username = u.UserName,
                            ProfileImageUrl = u.ProfileImageUrl,
                            UserRating = u.Rating.ToString(),
                            MemberSince = u.MemberSince
                        });
                    oldTypeQuery = typeQuery;
                }

                break;
            }
        }

        var model = new ProfileListModel
        {
            Profiles = profiles,
            TypeQuery = typeQuery,
            OldTypeQuery = oldTypeQuery
        };
        return View(model);
    }

    public async Task<IActionResult> Detail(string id)
    {
        var user = await _userService.GetById(id);
        if (user == null) return RedirectToAction("Error", "Home");
        var userRoles = _userManager.GetRolesAsync(user).Result;
        var model = new ProfileModel
        {
            UserId = user.Id,
            Username = user.UserName,
            UserRating = user.Rating.ToString(),
            Email = user.Email,
            ProfileImageUrl = user.ProfileImageUrl,
            MemberSince = user.MemberSince,
            UserDescription = user.UserDescription,
            IsAdmin = userRoles.Contains("Admin")
        };
        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetById(id);
        if (user == null) return RedirectToAction("Error", "Home");
        var userRoles = _userManager.GetRolesAsync(user).Result;
        var model = new ProfileModel
        {
            UserId = user.Id,
            Username = user.UserName,
            Email = user.Email,
            UserDescription = user.UserDescription,
            ProfileImageUrl = user.ProfileImageUrl,
            IsAdmin = userRoles.Contains("Admin"),
            MemberSince = user.MemberSince
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditProfile(ProfileModel model)
    {
        var userId = _userManager.GetUserId(User);
        var imageUri = "";
        if (model.ImageUpload != null)
            //Azure
            //var blockBlob = UploadProfileImageForAzure(model.ImageUpload);
            //imageUri = blockBlob.Uri.AbsoluteUri;
            imageUri = UploadProfileImage(model.ImageUpload);

        var forum = new ApplicationUser
        {
            Id = model.UserId,
            UserDescription = model.UserDescription,
            ProfileImageUrl = imageUri
        };

        await _userService.Edit(forum);
        return RedirectToAction("Detail", "Profile", new {id = userId});
    }

    private CloudBlockBlob UploadProfileImageForAzure(IFormFile file)
    {
        if (file.Length > 4 * 1024 * 1024 && !file.ContentType.Contains("image"))
            return null;
        var connectionString = _configuration.GetConnectionString("AzureStorageAccount");
        var container = _uploadService.GetBlobContainer(connectionString, "profile-images");
        var contentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
        if (contentDisposition.FileName == null) return null;
        var blockBlob =
            container.GetBlockBlobReference(Guid.NewGuid() +
                                            Path.GetExtension(contentDisposition.FileName.Trim('"')));
        blockBlob.UploadFromStreamAsync(file.OpenReadStream()).Wait();

        return blockBlob;
    }

    private string UploadProfileImage(IFormFile file)
    {
        if (file.Length > 4 * 1024 * 1024 && !file.ContentType.Contains("image")) return null;
        var wwwRootPath = _webHostEnvironment.WebRootPath;
        var contentDisposition = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
        if (contentDisposition.FileName == null) return null;
        var filename = contentDisposition.FileName.Trim('"');
        var uniqueFileName = Guid.NewGuid() + Path.GetExtension(filename);
        var path = Path.Combine(wwwRootPath + "/images/profile-images/", uniqueFileName);
        using var fileStream = new FileStream(path, FileMode.Create);
        file.CopyTo(fileStream);

        return "/images/profile-images/" + uniqueFileName;
    }
}