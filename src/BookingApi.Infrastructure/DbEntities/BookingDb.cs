using BookingApi.Domain.Entities;
using LanguageExt;
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

        public DateOnly Date { get; set; }

        public TimeOnly From { get; set; }

        public TimeOnly To { get; set; }

        public decimal Price { get; set; }
    }

    public static class BookingExtensions
    {
        public static Booking ToDomain(this BookingDb entity)
        {
            return new Booking
            (
                entity.Id,
                entity.Name,
                entity.Date,
                entity.From,
                entity.To,
                entity.Price
            );
        }

        public static BookingDb ToEntity(this Booking domain)
        {
            return new BookingDb()
            {
                Id = domain.Id,
                Name = domain.Name,
                Date = domain.Date,
                From = domain.From,
                To = domain.To,
                Price = domain.Price
            };
        }
    }
}
