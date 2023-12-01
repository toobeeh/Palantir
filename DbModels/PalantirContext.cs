using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

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
        public virtual DbSet<Award> Awards { get; set; }
        public virtual DbSet<Awardee> Awardees { get; set; }
        public virtual DbSet<BoostSplit> BoostSplits { get; set; }
        public virtual DbSet<BubbleTrace> BubbleTraces { get; set; }
        public virtual DbSet<CloudTag> CloudTags { get; set; }
        public virtual DbSet<DropBoost> DropBoosts { get; set; }
        public virtual DbSet<Event> Events { get; set; }
        public virtual DbSet<EventCredit> EventCredits { get; set; }
        public virtual DbSet<EventDrop> EventDrops { get; set; }
        public virtual DbSet<GuildLobby> GuildLobbies { get; set; }
        public virtual DbSet<GuildSetting> GuildSettings { get; set; }
        public virtual DbSet<Lob> Lobs { get; set; }
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
        public virtual DbSet<Sp> Sps { get; set; }
        public virtual DbSet<SplitCredit> SplitCredits { get; set; }
        public virtual DbSet<Sprite> Sprites { get; set; }
        public virtual DbSet<SpriteProfile> SpriteProfiles { get; set; }
        public virtual DbSet<Status> Statuses { get; set; }
        public virtual DbSet<Theme> Themes { get; set; }
        public virtual DbSet<ThemeShare> ThemeShares { get; set; }
        public virtual DbSet<UserTheme> UserThemes { get; set; }
        public virtual DbSet<Webhook> Webhooks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql($"server={Program.DatabaseHost};user id={Program.DatabaseUser};database=palantir", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.11.3-mariadb"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet("utf8mb4");

            modelBuilder.Entity<AccessToken>(entity =>
            {
                entity.HasKey(e => e.Login)
                    .HasName("PRIMARY");

                entity.Property(e => e.Login)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever();

                entity.Property(e => e.AccessToken1)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("AccessToken");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("date")
                    .HasDefaultValueSql("current_timestamp()");
            });

            modelBuilder.Entity<Award>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasColumnName("ID");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Rarity).HasColumnType("tinyint(4)");

                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("URL");
            });

            modelBuilder.Entity<Awardee>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasColumnName("ID");

                entity.Property(e => e.Award).HasColumnType("smallint(6)");

                entity.Property(e => e.AwardeeLogin).HasColumnType("int(6)");

                entity.Property(e => e.Date).HasColumnType("bigint(20)");

                entity.Property(e => e.ImageId)
                    .HasColumnType("bigint(20)")
                    .HasColumnName("ImageID");

                entity.Property(e => e.OwnerLogin).HasColumnType("int(6)");
            });

            modelBuilder.Entity<BoostSplit>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Value).HasColumnType("int(11)");
            });

            modelBuilder.Entity<BubbleTrace>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Bubbles).HasColumnType("int(11)");

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Login).HasColumnType("int(11)");
            });

            modelBuilder.Entity<CloudTag>(entity =>
            {
                entity.HasKey(e => new { e.Owner, e.ImageId })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity.Property(e => e.Owner).HasColumnType("int(11)");

                entity.Property(e => e.ImageId)
                    .HasColumnType("bigint(20)")
                    .HasColumnName("ImageID");

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasMaxLength(14);

                entity.Property(e => e.Date).HasColumnType("bigint(20)");

                entity.Property(e => e.Language)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(30);
            });

            modelBuilder.Entity<DropBoost>(entity =>
            {
                entity.HasKey(e => e.Login)
                    .HasName("PRIMARY");

                entity.Property(e => e.Login)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever();

                entity.Property(e => e.CooldownBonusS).HasColumnType("int(11)");

                entity.Property(e => e.DurationS).HasColumnType("int(11)");

                entity.Property(e => e.Factor)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.StartUtcs)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("StartUTCS");
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.EventId)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever()
                    .HasColumnName("EventID");

                entity.Property(e => e.DayLength).HasColumnType("int(11)");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.EventName)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Progressive).HasColumnType("tinyint(4)");

                entity.Property(e => e.ValidFrom)
                    .IsRequired()
                    .HasColumnType("text");
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
                entity.Property(e => e.EventDropId)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever()
                    .HasColumnName("EventDropID");

                entity.Property(e => e.EventId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Url)
                    .IsRequired()
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

                entity.Property(e => e.Lobbies)
                    .IsRequired()
                    .HasColumnType("text");
            });

            modelBuilder.Entity<GuildSetting>(entity =>
            {
                entity.HasKey(e => e.GuildId)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.Property(e => e.GuildId)
                    .HasColumnType("text")
                    .HasColumnName("GuildID");

                entity.Property(e => e.Settings)
                    .IsRequired()
                    .HasColumnType("text");
            });

            modelBuilder.Entity<Lob>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("lobs");

                entity.Property(e => e.JsonUnquoteDcName)
                    .HasColumnName("JSON_UNQUOTE(DcName)")
                    .HasComment("utf8mb3_general_ci")
                    .HasCharSet("utf8mb3");

                entity.Property(e => e.NameExp3)
                    .HasColumnType("mediumtext")
                    .HasColumnName("Name_exp_3")
                    .HasComment("utf8mb3_general_ci")
                    .HasCharSet("utf8mb3");

                entity.Property(e => e.PlayerLobbyId)
                    .HasColumnType("mediumtext")
                    .HasColumnName("PlayerLobbyID")
                    .HasComment("utf8mb3_general_ci")
                    .HasCharSet("utf8mb3");
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
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("Lobby");
            });

            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Login)
                    .HasName("PRIMARY");

                entity.Property(e => e.Login)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever();

                entity.Property(e => e.AwardPackOpened).HasColumnType("bigint(20)");

                entity.Property(e => e.Bubbles).HasColumnType("int(11)");

                entity.Property(e => e.Customcard).HasColumnType("text");

                entity.Property(e => e.Drops).HasColumnType("int(11)");

                entity.Property(e => e.Emoji).HasColumnType("text");

                entity.Property(e => e.Flag).HasColumnType("int(11)");

                entity.Property(e => e.Member1)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("Member");

                entity.Property(e => e.Patronize).HasColumnType("text");

                entity.Property(e => e.RainbowSprites).HasColumnType("text");

                entity.Property(e => e.Scenes)
                    .HasColumnType("text")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Sprites)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Streamcode)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasDefaultValueSql("''");
            });

            modelBuilder.Entity<NextDrop>(entity =>
            {
                entity.HasKey(e => e.DropId)
                    .HasName("PRIMARY");

                entity.ToTable("NextDrop");

                entity.Property(e => e.DropId)
                    .HasColumnType("bigint(20)")
                    .ValueGeneratedNever()
                    .HasColumnName("DropID");

                entity.Property(e => e.CaughtLobbyKey)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.CaughtLobbyPlayerId)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("CaughtLobbyPlayerID");

                entity.Property(e => e.EventDropId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventDropID");

                entity.Property(e => e.LeagueWeight).HasColumnType("int(11)");

                entity.Property(e => e.ValidFrom)
                    .IsRequired()
                    .HasColumnType("text");
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

                entity.Property(e => e.Date).HasColumnType("int(20)");

                entity.Property(e => e.ItemId)
                    .HasColumnType("bigint(11)")
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

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Id)
                    .IsRequired()
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

                entity.Property(e => e.Palantir)
                    .IsRequired()
                    .HasColumnType("text");
            });

            modelBuilder.Entity<PalantiriNightly>(entity =>
            {
                entity.HasKey(e => e.Token)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.ToTable("PalantiriNightly");

                entity.Property(e => e.Token).HasColumnType("text");

                entity.Property(e => e.Palantir)
                    .IsRequired()
                    .HasColumnType("text");
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

                entity.Property(e => e.ValidFrom)
                    .IsRequired()
                    .HasColumnType("text");
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

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Report1)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("Report");
            });

            modelBuilder.Entity<Scene>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Artist)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Color)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.EventId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventID");

                entity.Property(e => e.GuessedColor).HasColumnType("text");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("URL");
            });

            modelBuilder.Entity<Sp>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("sps");

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("ID");

                entity.Property(e => e.LobbyKey)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.LobbyPlayerId)
                    .HasColumnType("int(11)")
                    .HasColumnName("LobbyPlayerID");

                entity.Property(e => e.Slot).HasColumnType("int(11)");

                entity.Property(e => e.Sprite).HasColumnType("int(11)");
            });

            modelBuilder.Entity<SplitCredit>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasColumnName("ID");

                entity.Property(e => e.Comment)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Login).HasColumnType("int(11)");

                entity.Property(e => e.RewardDate)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Split).HasColumnType("int(11)");

                entity.Property(e => e.ValueOverride)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("-1");
            });

            modelBuilder.Entity<Sprite>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Artist)
                    .HasColumnType("text")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.Cost).HasColumnType("int(11)");

                entity.Property(e => e.EventDropId)
                    .HasColumnType("int(11)")
                    .HasColumnName("EventDropID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Rainbow).HasColumnType("int(11)");

                entity.Property(e => e.Url)
                    .IsRequired()
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

                entity.Property(e => e.Combo)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.RainbowSprites)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Scene)
                    .IsRequired()
                    .HasColumnType("text");
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

                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Status1)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("Status");
            });

            modelBuilder.Entity<Theme>(entity =>
            {
                entity.HasKey(e => e.Ticket)
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 32 });

                entity.Property(e => e.Ticket).HasColumnType("text");

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.Theme1)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("Theme");

                entity.Property(e => e.ThumbnailGame).HasColumnType("text");

                entity.Property(e => e.ThumbnailLanding)
                    .IsRequired()
                    .HasColumnType("text");
            });

            modelBuilder.Entity<ThemeShare>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasMaxLength(8)
                    .HasColumnName("ID");

                entity.Property(e => e.Theme)
                    .IsRequired()
                    .HasColumnType("text");
            });

            modelBuilder.Entity<UserTheme>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasMaxLength(8)
                    .HasColumnName("ID");

                entity.Property(e => e.Downloads).HasColumnType("int(11)");

                entity.Property(e => e.OwnerId)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("OwnerID");

                entity.Property(e => e.Version)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'1'");
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
                    .IsRequired()
                    .HasColumnType("text")
                    .HasColumnName("WebhookURL");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
