using BookingApi.Domain.Entities;
using LanguageExt;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Infrastructure.DbEntities
{
    public class BookingDb
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public NpgsqlRange<DateTime> Duration { get; set; }

        public decimal Price { get; set; }

        public ContactDb Contact { get; set; }

        public FieldDb Field { get; set; }
    }

    public static class BookingExtensions
    {
        public static Booking ToDomain(this BookingDb entity)
        {
            return new Booking
            (
                entity.Id,
                entity.Name,
                DateOnly.FromDateTime(entity.Duration.LowerBound.Date),
                TimeOnly.FromDateTime(entity.Duration.LowerBound.Date),
                TimeOnly.FromDateTime(entity.Duration.UpperBound.Date),
                entity.Price
            );
        }

        public static BookingDb ToEntity(this Booking domain)
        {
            return new BookingDb()
            {
                Id = domain.Id,
                Name = domain.Name,
                Duration = new NpgsqlRange<DateTime>(GetDateTime(domain.Date, domain.From), GetDateTime(domain.Date, domain.To)),
                Price = domain.Price
            };
        }

        private static DateTime GetDateTime(DateOnly date, TimeOnly time)
        {
            return new DateTime(date.Year, date.Month, date.Day) + time.ToTimeSpan();
        }
    }
}
