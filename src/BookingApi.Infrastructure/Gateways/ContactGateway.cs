using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

public class ContactGateway(BookingApiDbContext db) : Gateway, IContactGateway
{
    public async Task<Either<Error, Contact[]>> GetAllAsync()
    {
        try
        {
            var entities = await db.Contacts.ToListAsync();
            return Right(entities.Select(e => e.ToDomain()).ToArray());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Contact[]>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, Contact>> GetAsync(Guid id)
    {
        try
        {
            var entity = await db.Contacts.FirstOrDefaultAsync(x => x.Id == id);

            if (entity is null)
                return Left(new Error(ErrorType.NotFound, $"Contact {id} not found."));

            return Right(entity.ToDomain());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Contact>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, Contact>> UpdateAsync(Contact contact)
    {
        try
        {
            var entity = await db.Contacts.FirstOrDefaultAsync(x => x.Id == contact.Id);

            if (entity is null)
                return Left(new Error(ErrorType.NotFound, $"Contact {contact.Id} not found."));

            entity.FirstName = contact.FirstName;
            entity.LastName = contact.LastName;
            entity.Email = contact.Email;
            entity.Phone = contact.Phone;
            entity.Show = contact.Show;

            await db.SaveChangesAsync();

            return Right(entity.ToDomain());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Contact>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, bool>> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await db.Contacts.FirstOrDefaultAsync(x => x.Id == id);

            if (entity is null)
                return Left(new Error(ErrorType.NotFound, $"Contact {id} not found."));

            db.Contacts.Remove(entity);
            await db.SaveChangesAsync();

            return Right(true);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<bool>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, Contact>> CreateAsync(Contact contact)
    {
        try
        {
            var entity = new ContactDb
            {
                Id = Guid.NewGuid(),
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                Phone = contact.Phone,
                Show = contact.Show,
            };

            db.Contacts.Add(entity);
            await db.SaveChangesAsync();

            return Right(entity.ToDomain());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Contact>(ex, ErrorType.General);
        }
    }
}
