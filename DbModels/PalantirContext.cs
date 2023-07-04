using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Palantir.Model;

namespace Palantir.Model
{
    public partial class PalantirContext : DbContext
    {
        public PalantirContext()
        {
        }

        public PalantirContext(DbContextOptions<PalantirContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AccessToken> AccessTokens { get; set; }

        public virtual DbSet<BoostSplit> BoostSplits { get; set; }

        public virtual DbSet<BubbleTrace> BubbleTraces { get; set; }

        public virtual DbSet<DropBoost> DropBoosts { get; set; }

        public virtual DbSet<Event> Events { get; set; }

        public virtual DbSet<EventCredit> EventCredits { get; set; }

        public virtual DbSet<EventDrop> EventDrops { get; set; }

        public virtual DbSet<GuildLobby> GuildLobbies { get; set; }

        public virtual DbSet<GuildSetting> GuildSettings { get; set; }

        public virtual DbSet<Lobby> Lobbies { get; set; }

        public virtual DbSet<Member> Members { get; set; }

        public virtual DbSet<NextDrop> NextDrops { get; set; }

        public virtual DbSet<OnlineItem> OnlineItems { get; set; }

        public virtual DbSet<OnlineSprite> OnlineSprites { get; set; }

        public virtual DbSet<Palantiri> Palantiris { get; set; }

        public virtual DbSet<PalantiriNightly> PalantiriNightlies { get; set; }

        public virtual DbSet<PastDrop> PastDrops { get; set; }

        public virtual DbSet<Report> Reports { get; set; }

        public virtual DbSet<Scene> Scenes { get; set; }

        public virtual DbSet<SplitCredit> SplitCredits { get; set; }

        public virtual DbSet<Sprite> Sprites { get; set; }

        public virtual DbSet<SpriteProfile> SpriteProfiles { get; set; }

        public virtual DbSet<Status> Statuses { get; set; }

        public virtual DbSet<Theme> Themes { get; set; }

        public virtual DbSet<Webhook> Webhooks { get; set; }

        private readonly string conn = $"server={Program.DatabaseHost};user id={Program.DatabaseUser};database=palantir";
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseMySql(conn, ServerVersion.AutoDetect(conn));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .UseCollation("utf8mb4_general_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<AccessToken>(entity =>
            {
                entity.HasKey(e => e.Login).HasName("PRIMARY");

                entity.Property(e => e.Login)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)");
                entity.Property(e => e.AccessToken1)
                    .HasColumnType("text")
                    .HasColumnName("AccessToken");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("current_timestamp()");
            });

            modelBuilder.Entity<BoostSplit>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)")
                    .HasColumnName("ID");
                entity.Property(e => e.Date).HasColumnType("text");
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Name).HasColumnType("text");
                entity.Property(e => e.Value).HasColumnType("int(11)");
            });

            modelBuilder.Entity<BubbleTrace>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)")
                    .HasColumnName("ID");
                entity.Property(e => e.Bubbles).HasColumnType("int(11)");
                entity.Property(e => e.Date).HasColumnType("text");
                entity.Property(e => e.Login).HasColumnType("int(11)");
            });

            modelBuilder.Entity<DropBoost>(entity =>
            {
                entity.HasKey(e => e.Login).HasName("PRIMARY");

                entity.Property(e => e.Login)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)");
                entity.Property(e => e.CooldownBonusS).HasColumnType("int(11)");
                entity.Property(e => e.DurationS).HasColumnType("int(11)");
                entity.Property(e => e.Factor).HasColumnType("text");
                entity.Property(e => e.StartUtcs)
                    .HasColumnType("text")
                    .HasColumnName("StartUTCS");
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventId).HasName("PRIMARY");

                entity.Property(e => e.EventId)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)")
                    .HasColumnName("EventID");
                entity.Property(e => e.DayLength).HasColumnType("int(11)");
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.EventName).HasColumnType("text");
                entity.Property(e => e.ValidFrom).HasColumnType("text");
            });

            modelBuilder.Entity<EventCredit>(entity =>
            {
                entity.HasKey(e => new { e.Login, e.EventDropId })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity.Property(e => e.Login).HasColumnType("int(11)");
                entity.Property(e => e.EventDropId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventDropID");
                entity.Property(e => e.Credit).HasColumnType("int(11)");
            });

            modelBuilder.Entity<EventDrop>(entity =>
            {
                entity.HasKey(e => e.EventDropId).HasName("PRIMARY");

                entity.Property(e => e.EventDropId)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)")
                    .HasColumnName("EventDropID");
                entity.Property(e => e.EventId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventID");
                entity.Property(e => e.Name).HasColumnType("text");
                entity.Property(e => e.Url)
                    .HasColumnType("text")
                    .HasColumnName("URL");
            });

            modelBuilder.Entity<GuildLobby>(entity =>
            {
                entity.HasKey(e => e.GuildId)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.Property(e => e.GuildId)
                    .HasColumnType("text")
                    .HasColumnName("GuildID");
                entity.Property(e => e.Lobbies).HasColumnType("text");
            });

            modelBuilder.Entity<GuildSetting>(entity =>
            {
                entity.HasKey(e => e.GuildId)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.Property(e => e.GuildId)
                    .HasColumnType("text")
                    .HasColumnName("GuildID");
                entity.Property(e => e.Settings).HasColumnType("text");
            });

            modelBuilder.Entity<Lobby>(entity =>
            {
                entity.HasKey(e => e.LobbyId)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.Property(e => e.LobbyId)
                    .HasColumnType("text")
                    .HasColumnName("LobbyID");
                entity.Property(e => e.Lobby1)
                    .HasColumnType("text")
                    .HasColumnName("Lobby");
            });

            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Login).HasName("PRIMARY");

                entity.Property(e => e.Login)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)");
                entity.Property(e => e.Bubbles).HasColumnType("int(11)");
                entity.Property(e => e.Customcard).HasColumnType("text");
                entity.Property(e => e.Drops).HasColumnType("int(11)");
                entity.Property(e => e.Emoji).HasColumnType("text");
                entity.Property(e => e.Flag).HasColumnType("int(11)");
                entity.Property(e => e.Member1)
                    .HasColumnType("text")
                    .HasColumnName("Member");
                entity.Property(e => e.Patronize).HasColumnType("text");
                entity.Property(e => e.RainbowSprites).HasColumnType("text");
                entity.Property(e => e.Scenes)
                    .HasDefaultValueSql("''")
                    .HasColumnType("text");
                entity.Property(e => e.Sprites)
                    .HasDefaultValueSql("''")
                    .HasColumnType("text");
                entity.Property(e => e.Streamcode)
                    .HasDefaultValueSql("''")
                    .HasColumnType("text");
            });

            modelBuilder.Entity<NextDrop>(entity =>
            {
                entity.HasKey(e => e.DropId).HasName("PRIMARY");

                entity.ToTable("NextDrop");

                entity.Property(e => e.DropId)
                    .ValueGeneratedNever()
                    .HasColumnType("bigint(20)")
                    .HasColumnName("DropID");
                entity.Property(e => e.CaughtLobbyKey).HasColumnType("text");
                entity.Property(e => e.CaughtLobbyPlayerId)
                    .HasColumnType("text")
                    .HasColumnName("CaughtLobbyPlayerID");
                entity.Property(e => e.EventDropId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventDropID");
                entity.Property(e => e.LeagueWeight).HasColumnType("int(11)");
                entity.Property(e => e.ValidFrom).HasColumnType("text");
            });

            modelBuilder.Entity<OnlineItem>(entity =>
            {
                entity.HasKey(e => new { e.ItemType, e.Slot, e.LobbyKey, e.LobbyPlayerId, e.Date })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32, 0, 32, 0, 0 });

                entity.Property(e => e.ItemType).HasColumnType("text");
                entity.Property(e => e.Slot).HasColumnType("int(11)");
                entity.Property(e => e.LobbyKey).HasColumnType("text");
                entity.Property(e => e.LobbyPlayerId)
                    .HasColumnType("int(11)")
                    .HasColumnName("LobbyPlayerID");
                entity.Property(e => e.Date).HasColumnType("int(11)");
                entity.Property(e => e.ItemId)
                    .HasColumnType("int(11)")
                    .HasColumnName("ItemID");
            });

            modelBuilder.Entity<OnlineSprite>(entity =>
            {
                entity.HasKey(e => new { e.LobbyKey, e.LobbyPlayerId, e.Slot })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32, 0, 0 });

                entity.Property(e => e.LobbyKey).HasColumnType("text");
                entity.Property(e => e.LobbyPlayerId)
                    .HasColumnType("int(11)")
                    .HasColumnName("LobbyPlayerID");
                entity.Property(e => e.Slot).HasColumnType("int(11)");
                entity.Property(e => e.Date).HasColumnType("text");
                entity.Property(e => e.Id)
                    .HasColumnType("text")
                    .HasColumnName("ID");
                entity.Property(e => e.Sprite).HasColumnType("int(11)");
            });

            modelBuilder.Entity<Palantiri>(entity =>
            {
                entity.HasKey(e => e.Token)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.ToTable("Palantiri");

                entity.Property(e => e.Token).HasColumnType("text");
                entity.Property(e => e.Palantir).HasColumnType("text");
            });

            modelBuilder.Entity<PalantiriNightly>(entity =>
            {
                entity.HasKey(e => e.Token)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.ToTable("PalantiriNightly");

                entity.Property(e => e.Token).HasColumnType("text");
                entity.Property(e => e.Palantir).HasColumnType("text");
            });

            modelBuilder.Entity<PastDrop>(entity =>
            {
                entity.HasKey(e => new { e.DropId, e.CaughtLobbyKey, e.CaughtLobbyPlayerId })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

                entity.Property(e => e.DropId)
                    .HasColumnType("bigint(11)")
                    .HasColumnName("DropID");
                entity.Property(e => e.CaughtLobbyKey).HasMaxLength(50);
                entity.Property(e => e.CaughtLobbyPlayerId)
                    .HasMaxLength(20)
                    .HasColumnName("CaughtLobbyPlayerID");
                entity.Property(e => e.EventDropId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventDropID");
                entity.Property(e => e.LeagueWeight).HasColumnType("int(11)");
                entity.Property(e => e.ValidFrom).HasColumnType("text");
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => new { e.LobbyId, e.ObserveToken })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32, 0 });

                entity.Property(e => e.LobbyId)
                    .HasColumnType("text")
                    .HasColumnName("LobbyID");
                entity.Property(e => e.ObserveToken).HasColumnType("int(11)");
                entity.Property(e => e.Date).HasColumnType("text");
                entity.Property(e => e.Report1)
                    .HasColumnType("text")
                    .HasColumnName("Report");
            });

            modelBuilder.Entity<Scene>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)")
                    .HasColumnName("ID");
                entity.Property(e => e.Artist).HasColumnType("text");
                entity.Property(e => e.Color).HasColumnType("text");
                entity.Property(e => e.EventId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventID");
                entity.Property(e => e.GuessedColor).HasColumnType("text");
                entity.Property(e => e.Name).HasColumnType("text");
                entity.Property(e => e.Url)
                    .HasColumnType("text")
                    .HasColumnName("URL");
            });

            modelBuilder.Entity<SplitCredit>(entity =>
            {
                entity.HasKey(e => new { e.Login, e.Split })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity.Property(e => e.Login).HasColumnType("int(11)");
                entity.Property(e => e.Split).HasColumnType("int(11)");
                entity.Property(e => e.Comment)
                    .HasDefaultValueSql("''")
                    .HasColumnType("text");
                entity.Property(e => e.RewardDate).HasColumnType("text");
                entity.Property(e => e.ValueOverride)
                    .HasDefaultValueSql("-1")
                    .HasColumnType("int(11)");
            });

            modelBuilder.Entity<Sprite>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnType("int(11)")
                    .HasColumnName("ID");
                entity.Property(e => e.Artist)
                    .HasDefaultValueSql("''")
                    .HasColumnType("text");
                entity.Property(e => e.Cost).HasColumnType("int(11)");
                entity.Property(e => e.EventDropId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventDropID");
                entity.Property(e => e.Name).HasColumnType("text");
                entity.Property(e => e.Rainbow).HasColumnType("int(11)");
                entity.Property(e => e.Url)
                    .HasColumnType("text")
                    .HasColumnName("URL");
            });

            modelBuilder.Entity<SpriteProfile>(entity =>
            {
                entity.HasKey(e => new { e.Login, e.Name })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 32 });

                entity.Property(e => e.Login).HasColumnType("int(11)");
                entity.Property(e => e.Name).HasColumnType("text");
                entity.Property(e => e.Combo).HasColumnType("text");
                entity.Property(e => e.RainbowSprites).HasColumnType("text");
                entity.Property(e => e.Scene).HasColumnType("text");
            });

            modelBuilder.Entity<Status>(entity =>
            {
                entity.HasKey(e => e.SessionId)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.ToTable("Status");

                entity.Property(e => e.SessionId)
                    .HasColumnType("text")
                    .HasColumnName("SessionID");
                entity.Property(e => e.Date).HasColumnType("text");
                entity.Property(e => e.Status1)
                    .HasColumnType("text")
                    .HasColumnName("Status");
            });

            modelBuilder.Entity<Theme>(entity =>
            {
                entity.HasKey(e => e.Ticket)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.Property(e => e.Ticket).HasColumnType("text");
                entity.Property(e => e.Author).HasColumnType("text");
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Name).HasColumnType("text");
                entity.Property(e => e.Theme1)
                    .HasColumnType("text")
                    .HasColumnName("Theme");
                entity.Property(e => e.ThumbnailGame).HasColumnType("text");
                entity.Property(e => e.ThumbnailLanding).HasColumnType("text");
            });

            modelBuilder.Entity<Webhook>(entity =>
            {
                entity.HasKey(e => new { e.ServerId, e.Name })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32, 32 });

                entity.Property(e => e.ServerId)
                    .HasColumnType("text")
                    .HasColumnName("ServerID");
                entity.Property(e => e.Name).HasColumnType("text");
                entity.Property(e => e.WebhookUrl)
                    .HasColumnType("text")
                    .HasColumnName("WebhookURL");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

}