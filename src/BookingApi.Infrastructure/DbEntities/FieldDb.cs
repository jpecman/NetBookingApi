using BookingApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Infrastructure.DbEntities
{
    public class FieldDb
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        [Timestamp]
        public uint Version { get; set; }
    }

    public static class FieldExtensions
    {
        public static Field ToDomain(this FieldDb entity)
        {
            return new Field(entity.Id, entity.Name);
        }

        public static FieldDb ToEntity(this Field entity)
        {
            return new FieldDb { Id = entity.Id, Name = entity.Name };
        }
    }
}
