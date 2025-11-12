using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<Incident> Incidents => Set<Incident>();

    public AppDbContext(DbContextOptions<AppDbContext> opts)
        : base(opts) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Incident>().Property(x => x.TextVector).HasColumnType("jsonb"); // Postgres jsonb for float[]
    }
}
