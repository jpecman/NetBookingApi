using BookingApi.Domain.Entities;
using LanguageExt;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Infrastructure.DbEntities
{
    public class BookingDb
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        [Required]
        public Guid ContactId { get; set; }

        [ForeignKey(nameof(ContactId))]
        public ContactDb Contact { get; set; } = null!;

        public List<SlotDb> Slots { get; set; } = null!;

        [Timestamp]
        public uint Version { get; set; }
    }

    public static class BookingExtensions
    {
        public static Booking ToDomain(this BookingDb entity)
        {
            return new Booking
            (
                entity.Id,
                entity.Name,
                entity.Contact.ToDomain(),
                entity.Slots.Select(x => x.ToDomain()).ToImmutableList()
            );
        }

        public static BookingDb ToEntity(this Booking domain)
        {
            return new BookingDb()
            {
                Id = domain.Id,
                Name = domain.Name,
                ContactId = domain.Contact.Id,
                Slots = domain.Slots.Select(x => x.ToEntity()).ToList()
            };
        }
    }
}
