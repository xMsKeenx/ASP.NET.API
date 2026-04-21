using ASP.NET.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<Room>> GetAll(
            [FromQuery] int? minCapacity,
            [FromQuery] bool? hasProjector,
            [FromQuery] bool? activeOnly)
        {
            var query = DataStore.Rooms.AsQueryable();

            if (minCapacity.HasValue)
                query = query.Where(r => r.Capacity >= minCapacity.Value);

            if (hasProjector.HasValue)
                query = query.Where(r => r.HasProjector == hasProjector.Value);

            if (activeOnly.HasValue)
                query = query.Where(r => r.IsActive == activeOnly.Value);

            return Ok(query.ToList());
        }

        [HttpGet("{id}")]
        public ActionResult<Room> GetById([FromRoute] int id)
        {
            var room = DataStore.Rooms.FirstOrDefault(r => r.Id == id);

            if (room == null)
                return NotFound(new { message = $"Room with ID {id} not found." });

            return Ok(room);
        }

        [HttpGet("building/{buildingCode}")]
        public ActionResult<IEnumerable<Room>> GetByBuilding([FromRoute] string buildingCode)
        {
            var rooms = DataStore.Rooms
                .Where(r => r.BuildingCode.Equals(buildingCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Ok(rooms);
        }

        [HttpPost]
        public ActionResult<Room> Create([FromBody] Room newRoom)
        {
            newRoom.Id = DataStore.NextRoomId;
            DataStore.Rooms.Add(newRoom);

            return CreatedAtAction(nameof(GetById), new { id = newRoom.Id }, newRoom);
        }

        [HttpPut("{id}")]
        public ActionResult<Room> Update([FromRoute] int id, [FromBody] Room updatedRoom)
        {
            var existingRoom = DataStore.Rooms.FirstOrDefault(r => r.Id == id);

            if (existingRoom == null)
                return NotFound(new { message = $"Room with ID {id} not found." });

            existingRoom.Name = updatedRoom.Name;
            existingRoom.BuildingCode = updatedRoom.BuildingCode;
            existingRoom.Floor = updatedRoom.Floor;
            existingRoom.Capacity = updatedRoom.Capacity;
            existingRoom.HasProjector = updatedRoom.HasProjector;
            existingRoom.IsActive = updatedRoom.IsActive;

            return Ok(existingRoom);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete([FromRoute] int id)
        {
            var room = DataStore.Rooms.FirstOrDefault(r => r.Id == id);

            if (room == null)
                return NotFound(new { message = $"Room with ID {id} not found." });

            bool hasReservations = DataStore.Reservations.Any(res => res.RoomId == id);
            if (hasReservations)
            {
                return Conflict(new { message = "Cannot delete this room because it has existing reservations ." });
            }

            DataStore.Rooms.Remove(room);

            return NoContent();
        }
    }
}