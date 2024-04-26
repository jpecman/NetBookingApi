using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;
using BookingApi.Models.Slots;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

public class SlotController : BaseApiController
{
    private readonly ISlotRepository _slotRepository;

    public SlotController(ISlotRepository slotRepository)
    {
        _slotRepository = slotRepository;
    }

    [HttpGet]
    public async Task<ActionResult<SlotResponse[]>> GetAsync([FromQuery] DateTime from, DateTime to)
    {
        var response = await _slotRepository.GetAsync(new SlotRequest(from, to).ToDomain());

        return HandleResponse(response, slots => slots.Select(s => s.ToResponse()).ToArray());
    }
}
