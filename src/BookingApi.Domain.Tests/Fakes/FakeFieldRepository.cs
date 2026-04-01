using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;

using LanguageExt;

namespace BookingApi.Domain.Tests.Fakes
{
    internal class FakeFieldRepository : IFieldRepository
    {
        public Task<Either<Error, Field>> Create(Field entity) => throw new NotImplementedException();
        public Task<Either<Error, bool>> Delete(Guid id) => throw new NotImplementedException();
        public Task<Either<Error, Field>> GetAsync(Guid id) => throw new NotImplementedException();
        public Task<Either<Error, ImmutableArray<Field>>> SearchAsync(FieldSearch search) => throw new NotImplementedException();
        public Task<Either<Error, Field>> Update(Field entity) => throw new NotImplementedException();
    }
}
