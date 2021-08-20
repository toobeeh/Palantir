using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public DbSet<OnlineSpritesEntity> OnlineSprites { get; set; }
        public DbSet<SpritesEntity> Sprites { get; set; }
        public DbSet<DropEntity> Drop { get; set; }
        public DbSet<PastDropEntity> PastDrops { get; set; }
        public DbSet<BubbleTraceEntity> BubbleTraces { get; set; }
        public DbSet<EventEntity> Events { get; set; }
        public DbSet<EventDropEntity> EventDrops { get; set; }
        public DbSet<EventCreditEntity> EventCredits { get; set; }
        public DbSet<TypoThemeEntity> Themes { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=/home/pi/Database/palantir.db");
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<EventCreditEntity>()
                .HasKey(e => new { e.Login, e.EventDropID });
        }

    }


    public class MemberEntity
    {
        [Key]
        public string Login { get; set; }
        public string Member { get; set; }
        public string Sprites { get; set; }
        public int Bubbles { get; set; }
        public int Drops { get; set; }
        public int Flag { get; set; }
        public string Emoji { get; set; }
        public string Patronize { get; set; }
        public string Customcard { get; set; }
    }
    public class PalantirEntity
    {
        [Key]
        public string Token { get; set; }
        public string Palantir { get; set; }
    }
    public class SpritesEntity
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }
        public int Cost { get; set; }
        public bool Special { get; set; }
        public int EventDropID { get; set; }
        public string Artist { get; set; }
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
        public string SessionID { get; set; }
        public string Status { get; set; }
        public string Date { get; set; }
    }

    public class OnlineSpritesEntity
    {
        [Key]
        public string ID { get; set; }
        public string LobbyKey { get; set; }
        public string LobbyPlayerID { get; set; }
        public string Sprite { get; set; }
        public string Date { get; set; }
        public int Slot { get; set; }
    }

    public class DropEntity
    {
        [Key]
        public string DropID { get; set; }
        public string CaughtLobbyPlayerID { get; set; }
        public string CaughtLobbyKey { get; set; }
        public string ValidFrom { get; set; }
        public int EventDropID { get; set; }
    }
    public class PastDropEntity
    {
        [Key]
        public string DropID { get; set; }
        public string CaughtLobbyPlayerID { get; set; }
        public string CaughtLobbyKey { get; set; }
        public string ValidFrom { get; set; }
        public int EventDropID { get; set; }
    }

    public class BubbleTraceEntity
    {
        [Key]
        public int ID { get; set; }
        public string Date { get; set; }
        public string Login { get; set; }
        public int Bubbles { get; set; }
    }

    public class EventEntity
    {
        [Key]
        public int EventID { get; set; }
        public string EventName { get; set; }
        public int DayLength { get; set; }
        public string ValidFrom { get; set; }
        public string Description { get; set; }
    }

    public class EventDropEntity
    {
        [Key]
        public int EventDropID { get; set; }
        public int EventID { get; set; }
        public string URL { get; set; }
        public string Name { get; set; }
    }

    public class EventCreditEntity
    {
        public string Login { get; set; }
        public int EventDropID { get; set; }
        public int Credit { get; set; }
    }
    public class TypoThemeEntity
    {
        [Key]
        public string Ticket { get; set; }
        public string Theme { get; set; }
        public string ThumbnailLanding { get; set; }
        public string ThumbnailGame { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
    }
}
