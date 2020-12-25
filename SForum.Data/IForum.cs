﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SForum.Data.Models;

namespace SForum.Data
{
    public interface IForum
    {
        Forum GetById(int id);
        IEnumerable<Forum> GetAll();
        IEnumerable<ApplicationUser> GetActiveUsers(int id);
        bool HasRecentPost(int id);
        Task Create(Forum forum);
        Task Edit(Forum forum);
        Task Delete(int forumId);
        public IEnumerable<Forum> GetPopularForums(int numberForums);
    }
}