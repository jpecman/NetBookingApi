namespace BookingApi.Domain.Entities;

public record Booking
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public uint Price { get; set; }
}