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
        public Guid PitchId { get; set; }

        [ForeignKey(nameof(PitchId))]
        public PitchDb Pitch { get; set; }

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
                entity.PitchId
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
                PitchId = domain.PitchId
            };
        }
    }
}
