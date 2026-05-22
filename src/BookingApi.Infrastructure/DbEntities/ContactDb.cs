using BookingApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Infrastructure.DbEntities
{
    public class ContactDb
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool Show { get; set; }

        [Timestamp]
        public uint Version { get; set; }
    }

    public static class ContactExtensions
    {
        public static Contact ToDomain(this ContactDb entity)
        {
            return new Contact
            (
                entity.Id,
                entity.FirstName,
                entity.LastName,
                entity.Email,
                entity.Phone,
                entity.Show
            );
        }

        public static ContactDb ToEntity(this Contact domain)
        {
            return new ContactDb()
            {
                Id = domain.Id,
                FirstName = domain.FirstName,
                LastName = domain.LastName,
                Email = domain.Email,
                Phone = domain.Phone,
                Show = domain.Show,
            };
        }
    }
}
