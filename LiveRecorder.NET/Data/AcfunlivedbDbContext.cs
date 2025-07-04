using LiveRecorder.NET.Models.Acfun;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Data
{
    public class AcfunlivedbDbContext : DbContext
    {
        public AcfunlivedbDbContext(DbContextOptions<AcfunlivedbDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        public AcfunlivedbDbContext()
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 设计时使用的连接字符串
                optionsBuilder.UseSqlite("Data Source=./acfunlive.db;");
            }
        }

        public DbSet<AcfunLive> Lives { get; set; }
    }
}
