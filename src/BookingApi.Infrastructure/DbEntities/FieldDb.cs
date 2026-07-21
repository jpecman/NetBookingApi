using BookingApi.Domain.Entities;
using BookingApi.Infrastructure.Tenancy;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Infrastructure.DbEntities
{
    public class FieldDb : ITenantOwned
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string Name { get; set; } = null!;

        [Timestamp]
        public uint Version { get; set; }

        public ICollection<PitchDb> Pitches { get; set; } = null!;
    }

    public static class FieldExtensions
    {
        public static Field ToDomain(this FieldDb entity) => new(entity.Id, entity.Name, [..entity.Pitches.Select(x => x.ToDomain())]);

        public static FieldDb ToEntity(this Field entity)
        {
            return new FieldDb { Id = entity.Id, Name = entity.Name };
        }
    }
}
