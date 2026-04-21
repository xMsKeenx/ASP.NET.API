using ASP.NET.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<Reservation>> GetAll(
            [FromQuery] DateOnly? date,
            [FromQuery] string? status,
            [FromQuery] int? roomId)
        {
            var query = DataStore.Reservations.AsQueryable();

            if (date.HasValue)
                query = query.Where(r => r.Date == date.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

            if (roomId.HasValue)
                query = query.Where(r => r.RoomId == roomId.Value);

            return Ok(query.ToList());
        }

        [HttpGet("{id}")]
        public ActionResult<Reservation> GetById([FromRoute] int id)
        {
            var reservation = DataStore.Reservations.FirstOrDefault(r => r.Id == id);

            if (reservation == null)
                return NotFound(new { message = $"Reservation with ID {id} not found." });

            return Ok(reservation);
        }

        [HttpPost]
        public ActionResult<Reservation> Create([FromBody] Reservation newReservation)
        {
            var room = DataStore.Rooms.FirstOrDefault(r => r.Id == newReservation.RoomId);

            if (room == null)
                return BadRequest(new { message = "Cannot create reservation: Room does not exist." });

            if (!room.IsActive)
                return BadRequest(new { message = "Cannot create reservation: Room is currently inactive." });

            bool isOverlap = DataStore.Reservations.Any(r =>
                r.RoomId == newReservation.RoomId &&
                r.Date == newReservation.Date &&
                r.StartTime < newReservation.EndTime &&
                newReservation.StartTime < r.EndTime);

            if (isOverlap)
                return Conflict(new { message = "Cannot create reservation: Time overlaps with an existing reservation." });

            newReservation.Id = DataStore.NextReservationId;
            DataStore.Reservations.Add(newReservation);

            return CreatedAtAction(nameof(GetById), new { id = newReservation.Id }, newReservation);
        }

        [HttpPut("{id}")]
        public ActionResult<Reservation> Update([FromRoute] int id, [FromBody] Reservation updatedReservation)
        {
            var existingReservation = DataStore.Reservations.FirstOrDefault(r => r.Id == id);

            if (existingReservation == null)
                return NotFound(new { message = $"Reservation with ID {id} not found." });

            var room = DataStore.Rooms.FirstOrDefault(r => r.Id == updatedReservation.RoomId);

            if (room == null)
                return BadRequest(new { message = "Cannot update reservation: Room does not exist." });

            if (!room.IsActive)
                return BadRequest(new { message = "Cannot update reservation: Room is currently inactive." });

            bool isOverlap = DataStore.Reservations.Any(r =>
                r.Id != id &&
                r.RoomId == updatedReservation.RoomId &&
                r.Date == updatedReservation.Date &&
                r.StartTime < updatedReservation.EndTime &&
                updatedReservation.StartTime < r.EndTime);

            if (isOverlap)
                return Conflict(new { message = "Cannot update reservation: Time overlaps with an existing reservation." });

            existingReservation.RoomId = updatedReservation.RoomId;
            existingReservation.OrganizerName = updatedReservation.OrganizerName;
            existingReservation.Topic = updatedReservation.Topic;
            existingReservation.Date = updatedReservation.Date;
            existingReservation.StartTime = updatedReservation.StartTime;
            existingReservation.EndTime = updatedReservation.EndTime;
            existingReservation.Status = updatedReservation.Status;

            return Ok(existingReservation);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete([FromRoute] int id)
        {
            var reservation = DataStore.Reservations.FirstOrDefault(r => r.Id == id);

            if (reservation == null)
                return NotFound(new { message = $"Reservation with ID {id} not found." });

            DataStore.Reservations.Remove(reservation);

            return NoContent();
        }
    }
}
