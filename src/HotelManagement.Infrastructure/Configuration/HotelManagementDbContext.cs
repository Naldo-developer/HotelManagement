using System;
using System.Collections.Generic;
using HotelManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Infrastructure.Configuration;

public partial class HotelManagementDbContext : DbContext
{
    public HotelManagementDbContext(DbContextOptions<HotelManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Consumption> Consumptions { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Guest> Guests { get; set; }

    public virtual DbSet<Hotel> Hotels { get; set; }

    public virtual DbSet<Maintenance> Maintenances { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<ReservationStatusHistory> ReservationStatusHistories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Stay> Stays { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("app_users_pkey");

            entity.ToTable("app_users", "hotel");

            entity.HasIndex(e => e.EmployeeId, "uq_app_users_employee").IsUnique();

            entity.HasIndex(e => e.Username, "uq_app_users_username").IsUnique();

            entity.Property(e => e.UserId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("user_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Username)
                .HasMaxLength(60)
                .HasColumnName("username");

            entity.HasOne(d => d.Employee).WithOne(p => p.AppUser)
                .HasForeignKey<AppUser>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_app_users_employee");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("fk_user_roles_role"),
                    l => l.HasOne<AppUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("fk_user_roles_user"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("user_roles_pkey");
                        j.ToTable("user_roles", "hotel");
                        j.IndexerProperty<long>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<long>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<Consumption>(entity =>
        {
            entity.HasKey(e => e.ConsumptionId).HasName("consumptions_pkey");

            entity.ToTable("consumptions", "hotel");

            entity.HasIndex(e => e.StayId, "idx_consumptions_stay");

            entity.Property(e => e.ConsumptionId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("consumption_id");
            entity.Property(e => e.ConsumedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("consumed_at");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Quantity)
                .HasPrecision(10, 2)
                .HasDefaultValue(1m)
                .HasColumnName("quantity");
            entity.Property(e => e.RecordedByUserId).HasColumnName("recorded_by_user_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.StayId).HasColumnName("stay_id");
            entity.Property(e => e.Subtotal)
                .HasPrecision(14, 2)
                .HasComputedColumnSql("(quantity * unit_price)", true)
                .HasColumnName("subtotal");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(12, 2)
                .HasColumnName("unit_price");

            entity.HasOne(d => d.RecordedByUser).WithMany(p => p.Consumptions)
                .HasForeignKey(d => d.RecordedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_consumptions_user");

            entity.HasOne(d => d.Service).WithMany(p => p.Consumptions)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_consumptions_service");

            entity.HasOne(d => d.Stay).WithMany(p => p.Consumptions)
                .HasForeignKey(d => d.StayId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_consumptions_stay");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("employees_pkey");

            entity.ToTable("employees", "hotel");

            entity.HasIndex(e => new { e.DocumentType, e.DocumentNumber }, "uq_employees_document").IsUnique();

            entity.Property(e => e.EmployeeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("employee_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentNumber)
                .HasMaxLength(30)
                .HasColumnName("document_number");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(20)
                .HasColumnName("document_type");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(80)
                .HasColumnName("first_name");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.Position)
                .HasMaxLength(80)
                .HasColumnName("position");

            entity.HasOne(d => d.Hotel).WithMany(p => p.Employees)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_employees_hotel");
        });

        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(e => e.GuestId).HasName("guests_pkey");

            entity.ToTable("guests", "hotel");

            entity.HasIndex(e => new { e.DocumentType, e.DocumentNumber }, "uq_guests_document").IsUnique();

            entity.Property(e => e.GuestId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("guest_id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentNumber)
                .HasMaxLength(30)
                .HasColumnName("document_number");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(20)
                .HasColumnName("document_type");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(80)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Nationality)
                .HasMaxLength(80)
                .HasColumnName("nationality");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.HasKey(e => e.HotelId).HasName("hotels_pkey");

            entity.ToTable("hotels", "hotel");

            entity.HasIndex(e => e.TaxId, "uq_hotels_tax_id").IsUnique();

            entity.Property(e => e.HotelId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("hotel_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .HasDefaultValueSql("'PEN'::bpchar")
                .IsFixedLength()
                .HasColumnName("currency_code");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.TaxId)
                .HasMaxLength(30)
                .HasColumnName("tax_id");
            entity.Property(e => e.Timezone)
                .HasMaxLength(50)
                .HasDefaultValueSql("'America/Lima'::character varying")
                .HasColumnName("timezone");
        });

        modelBuilder.Entity<Maintenance>(entity =>
        {
            entity.HasKey(e => e.MaintenanceId).HasName("maintenance_pkey");

            entity.ToTable("maintenance", "hotel");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "idx_maintenance_dates");

            entity.HasIndex(e => e.RoomId, "idx_maintenance_room");

            entity.Property(e => e.MaintenanceId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("maintenance_id");
            entity.Property(e => e.AssignedEmployeeId).HasColumnName("assigned_employee_id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Cost)
                .HasPrecision(12, 2)
                .HasColumnName("cost");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ReportedByUserId).HasColumnName("reported_by_user_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'SCHEDULED'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.AssignedEmployee).WithMany(p => p.Maintenances)
                .HasForeignKey(d => d.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_maintenance_employee");

            entity.HasOne(d => d.ReportedByUser).WithMany(p => p.Maintenances)
                .HasForeignKey(d => d.ReportedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_maintenance_reported_user");

            entity.HasOne(d => d.Room).WithMany(p => p.Maintenances)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_maintenance_room");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");

            entity.ToTable("payments", "hotel");

            entity.HasIndex(e => e.ReservationId, "idx_payments_reservation");

            entity.Property(e => e.PaymentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .HasColumnName("payment_method");
            entity.Property(e => e.RecordedByUserId).HasColumnName("recorded_by_user_id");
            entity.Property(e => e.Reference)
                .HasMaxLength(100)
                .HasColumnName("reference");
            entity.Property(e => e.ReservationId).HasColumnName("reservation_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'COMPLETED'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PAYMENT'::character varying")
                .HasColumnName("transaction_type");

            entity.HasOne(d => d.RecordedByUser).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RecordedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_payments_user");

            entity.HasOne(d => d.Reservation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReservationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payments_reservation");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("reservations_pkey");

            entity.ToTable("reservations", "hotel");

            entity.HasIndex(e => new { e.CheckInDate, e.CheckOutDate }, "idx_reservations_dates");

            entity.HasIndex(e => e.PrimaryGuestId, "idx_reservations_guest");

            entity.HasIndex(e => e.RoomId, "idx_reservations_room");

            entity.HasIndex(e => e.Status, "idx_reservations_status");

            entity.HasIndex(e => e.ConfirmationCode, "uq_reservations_confirmation_code").IsUnique();

            entity.Property(e => e.ReservationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("reservation_id");
            entity.Property(e => e.Adults)
                .HasDefaultValue((short)1)
                .HasColumnName("adults");
            entity.Property(e => e.CancellationFee)
                .HasPrecision(12, 2)
                .HasColumnName("cancellation_fee");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CheckInDate).HasColumnName("check_in_date");
            entity.Property(e => e.CheckOutDate).HasColumnName("check_out_date");
            entity.Property(e => e.Children).HasColumnName("children");
            entity.Property(e => e.ConfirmationCode)
                .HasMaxLength(30)
                .HasColumnName("confirmation_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.NightlyRate)
                .HasPrecision(12, 2)
                .HasColumnName("nightly_rate");
            entity.Property(e => e.PrimaryGuestId).HasColumnName("primary_guest_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.SpecialRequests).HasColumnName("special_requests");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_reservations_created_by");

            entity.HasOne(d => d.PrimaryGuest).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.PrimaryGuestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reservations_primary_guest");

            entity.HasOne(d => d.Room).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reservations_room");

            entity.HasMany(d => d.Guests).WithMany(p => p.ReservationsNavigation)
                .UsingEntity<Dictionary<string, object>>(
                    "ReservationGuest",
                    r => r.HasOne<Guest>().WithMany()
                        .HasForeignKey("GuestId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_reservation_guests_guest"),
                    l => l.HasOne<Reservation>().WithMany()
                        .HasForeignKey("ReservationId")
                        .HasConstraintName("fk_reservation_guests_reservation"),
                    j =>
                    {
                        j.HasKey("ReservationId", "GuestId").HasName("reservation_guests_pkey");
                        j.ToTable("reservation_guests", "hotel");
                        j.IndexerProperty<long>("ReservationId").HasColumnName("reservation_id");
                        j.IndexerProperty<long>("GuestId").HasColumnName("guest_id");
                    });
        });

        modelBuilder.Entity<ReservationStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("reservation_status_history_pkey");

            entity.ToTable("reservation_status_history", "hotel");

            entity.Property(e => e.HistoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("history_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedByUserId).HasColumnName("changed_by_user_id");
            entity.Property(e => e.NewStatus)
                .HasMaxLength(20)
                .HasColumnName("new_status");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OldStatus)
                .HasMaxLength(20)
                .HasColumnName("old_status");
            entity.Property(e => e.ReservationId).HasColumnName("reservation_id");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.ReservationStatusHistories)
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_reservation_history_user");

            entity.HasOne(d => d.Reservation).WithMany(p => p.ReservationStatusHistories)
                .HasForeignKey(d => d.ReservationId)
                .HasConstraintName("fk_reservation_history_reservation");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("roles_pkey");

            entity.ToTable("roles", "hotel");

            entity.HasIndex(e => e.Code, "uq_roles_code").IsUnique();

            entity.Property(e => e.RoleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("role_id");
            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .HasColumnName("code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(80)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("rooms_pkey");

            entity.ToTable("rooms", "hotel");

            entity.HasIndex(e => e.RoomTypeId, "idx_rooms_type");

            entity.HasIndex(e => new { e.HotelId, e.RoomNumber }, "uq_rooms_hotel_number").IsUnique();

            entity.Property(e => e.RoomId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("room_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Floor)
                .HasMaxLength(20)
                .HasColumnName("floor");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OperationalStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("operational_status");
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(20)
                .HasColumnName("room_number");
            entity.Property(e => e.RoomTypeId).HasColumnName("room_type_id");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasPrincipalKey(p => new { p.RoomTypeId, p.HotelId })
                .HasForeignKey(d => new { d.RoomTypeId, d.HotelId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_rooms_room_type_hotel");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasKey(e => e.RoomTypeId).HasName("room_types_pkey");

            entity.ToTable("room_types", "hotel");

            entity.HasIndex(e => new { e.HotelId, e.Name }, "uq_room_types_hotel_name").IsUnique();

            entity.HasIndex(e => new { e.RoomTypeId, e.HotelId }, "uq_room_types_room_type_hotel").IsUnique();

            entity.Property(e => e.RoomTypeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("room_type_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.BaseRate)
                .HasPrecision(12, 2)
                .HasColumnName("base_rate");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.MaxAdults)
                .HasDefaultValue((short)1)
                .HasColumnName("max_adults");
            entity.Property(e => e.MaxChildren).HasColumnName("max_children");
            entity.Property(e => e.Name)
                .HasMaxLength(80)
                .HasColumnName("name");

            entity.HasOne(d => d.Hotel).WithMany(p => p.RoomTypes)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_room_types_hotel");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("services_pkey");

            entity.ToTable("services", "hotel");

            entity.HasIndex(e => new { e.HotelId, e.Name }, "uq_services_hotel_name").IsUnique();

            entity.Property(e => e.ServiceId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("service_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DefaultPrice)
                .HasPrecision(12, 2)
                .HasColumnName("default_price");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Unit)
                .HasMaxLength(30)
                .HasDefaultValueSql("'UNIT'::character varying")
                .HasColumnName("unit");

            entity.HasOne(d => d.Hotel).WithMany(p => p.Services)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_services_hotel");
        });

        modelBuilder.Entity<Stay>(entity =>
        {
            entity.HasKey(e => e.StayId).HasName("stays_pkey");

            entity.ToTable("stays", "hotel");

            entity.HasIndex(e => e.Status, "idx_stays_status");

            entity.HasIndex(e => e.ReservationId, "uq_stays_reservation").IsUnique();

            entity.Property(e => e.StayId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("stay_id");
            entity.Property(e => e.CheckInAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("check_in_at");
            entity.Property(e => e.CheckInByUserId).HasColumnName("check_in_by_user_id");
            entity.Property(e => e.CheckOutAt).HasColumnName("check_out_at");
            entity.Property(e => e.CheckOutByUserId).HasColumnName("check_out_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ReservationId).HasColumnName("reservation_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.CheckInByUser).WithMany(p => p.StayCheckInByUsers)
                .HasForeignKey(d => d.CheckInByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_stays_check_in_user");

            entity.HasOne(d => d.CheckOutByUser).WithMany(p => p.StayCheckOutByUsers)
                .HasForeignKey(d => d.CheckOutByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_stays_check_out_user");

            entity.HasOne(d => d.Reservation).WithOne(p => p.Stay)
                .HasForeignKey<Stay>(d => d.ReservationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stays_reservation");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
