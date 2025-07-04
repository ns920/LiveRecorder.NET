using LiveRecorder.NET.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Services
{
    internal class AutoMigrations
    {
        private readonly AcfunlivedbDbContext _dbContext;
        public AutoMigrations(AcfunlivedbDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void CommitMigrations()
        {
            if (_dbContext.Database.GetPendingMigrations().Any())
            {
                _dbContext.Database.Migrate(); //执行迁移
            }
        }
    }
}
