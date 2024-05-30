using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;
using BookingApi.Models.Contacts;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    public class ContactController : BaseApiController
    {
        private readonly ILogger<ContactController> _logger;
        private readonly IContactRepository _repo;

        public ContactController(ILogger<ContactController> logger, IContactRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        [HttpPost]
        public async Task<ActionResult<ContactResponse>> PostAsync([FromBody] CreateContactRequest contact)
        {
            var response = await _repo.Create(contact.ToDomain());

            if (response is Success<Contact> success)
            {
                return Ok(success.Data.ToResponse());
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<ActionResult<ContactResponse[]>> GetAsync()
        {
            var response = await _repo.SearchAsync(ContactSearch.Empty());

            if (response is Success<ImmutableArray<Contact>> success)
            {
                return Ok(success.Data.Select(c => c.ToResponse()));
            }

            return BadRequest();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactResponse>> GetAsync(Guid id)
        {
            var response = await _repo.GetAsync(id);

            if (response is Success<Contact> success)
            {
                return Ok(success.Data.ToResponse());
            }

            return BadRequest();
        }
    }
}
