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

        public DateTime From { get; set; }

        public DateTime To { get; set; }

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
                From = domain.From,
                To = domain.To,
                Price = domain.Price
            };
        }
    }
}
