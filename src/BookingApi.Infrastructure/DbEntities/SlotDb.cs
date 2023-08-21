using BookingApi.Domain.Entities;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Infrastructure.DbEntities
{
    public class SlotDb
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public NpgsqlRange<DateTime> Duration { get; set; }

        public decimal Price { get; set; }

        [Required]
        public Guid FieldId { get; set; }

        [ForeignKey(nameof(FieldId))]
        public FieldDb Field { get; set; }

        [Required]
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public BookingDb Booking { get; set; }

        [Timestamp]
        public uint Version { get; set; }
    }

    public static class SlotExtensions
    {
        public static Slot ToDomain(this SlotDb entity)
        {
            return new Slot
            (
                entity.Id,
                entity.Name,
                entity.Duration.LowerBound,
                entity.Duration.UpperBound,
                entity.Price,
                entity.Field.ToDomain()
            );
        }

        public static SlotDb ToEntity(this Slot domain)
        {
            return new SlotDb()
            {
                Id = domain.Id,
                Name = domain.Name,
                Duration = new NpgsqlRange<DateTime>(domain.From.ToUniversalTime(), domain.To.ToUniversalTime()),
                Price = domain.Price,
                FieldId = domain.Field.Id
            };
        }

        // TODO: move to common
        private static DateTime GetDateTime(DateOnly date, TimeOnly time)
        {
            return new DateTime(date.Year, date.Month, date.Day) + time.ToTimeSpan();
        }
    }
}
