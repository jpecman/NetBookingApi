using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Models.Fields;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    public class FieldController : BaseApiController
    {
        private readonly IFieldRepository _repo;

        public FieldController(IFieldRepository repo)
        {
            _repo = repo;
        }

        [HttpPost]
        public async Task<ActionResult<FieldResponse>> PostAsync(
            [FromBody] CreateFieldRequest field
        )
        {
            var response = await _repo.Create(field.ToDomain());

            if (response is Success<Field> success)
            {
                return Ok(success.Data.ToResponse());
            }

            return BadRequest();
        }
    }
}
