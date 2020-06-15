using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;

namespace Palantir
{
    public class PalantirDbContext : DbContext
    {
        public DbSet<MemberEntity> Members { get; set; }
        public DbSet<PalantirEntity> Palantiri { get; set; }
        public DbSet<ReportEntity> Reports { get; set; }
        public DbSet<LobbyEntity> Lobbies { get; set; }
        public DbSet<GuildLobbiesEntity> GuildLobbies { get; set; }
        public DbSet<GuildSettingsEntity> GuildSettings { get; set; }
        public DbSet<StatusEntity> Status { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=/home/pi/Database/palantir.db");

    }


    public class MemberEntity
    {
        [Key]
        public string Login { get; set; }
        public string Member { get; set; }
    }
    public class PalantirEntity
    {
        [Key]
        public string Token { get; set; }
        public string Palantir { get; set; }
    }
    public class ReportEntity
    {
        [Key]
        public string LobbyID { get; set; }
        public string ObserveToken { get; set; }
        public string Report { get; set; }
        public string Date { get; set; }
    }
    public class LobbyEntity
    {
        [Key]
        public string LobbyID { get; set; }
        public string Lobby { get; set; }
    }
    public class GuildLobbiesEntity
    {
        [Key]
        public string GuildID { get; set; }
        public string Lobbies { get; set; }
    }
    public class GuildSettingsEntity
    {
        [Key]
        public string GuildID { get; set; }
        public string Settings { get; set; }
    }
    public class StatusEntity
    {
        [Key]
        public string Status { get; set; }
        public string Date { get; set; }
    }

}
