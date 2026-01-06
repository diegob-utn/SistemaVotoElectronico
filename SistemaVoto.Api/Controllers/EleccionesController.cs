using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones")]
    public class EleccionesController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        public EleccionesController(SistemaVotoDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Listar(
            int page = 1,
            int pageSize = 20,
            EstadoEleccion? estado = null,
            TipoEleccion? tipo = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.Elecciones.AsNoTracking().AsQueryable();
            if (estado is not null) q = q.Where(e => e.Estado == estado);
            if (tipo is not null) q = q.Where(e => e.Tipo == tipo);

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.Id,
                    e.Titulo,
                    e.Descripcion,
                    e.Tipo,
                    e.NumEscanos,
                    e.Estado,
                    e.FechaInicioUtc,
                    e.FechaFinUtc
                })
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(new
            {
                page,
                pageSize,
                total,
                items
            }));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detalle(int id)
        {
            var e = await _db.Elecciones.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            var listas = await _db.Listas.AsNoTracking()
                .Where(l => l.EleccionId == id)
                .OrderBy(l => l.Id)
                .Select(l => new { l.Id, l.Nombre, l.LogoUrl })
                .ToListAsync();

            var candidatos = await _db.Candidatos.AsNoTracking()
                .Where(c => c.EleccionId == id)
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.Nombre, c.PartidoPolitico, c.FotoUrl, c.Propuestas, c.ListaId })
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(new
            {
                e.Id,
                e.Titulo,
                e.Descripcion,
                e.Tipo,
                e.NumEscanos,
                e.Estado,
                e.FechaInicioUtc,
                e.FechaFinUtc,
                listas,
                candidatos
            }));
        }

        public record CrearEleccionRequest(
            string Titulo,
            string? Descripcion,
            DateTime FechaInicioUtc,
            DateTime FechaFinUtc,
            TipoEleccion Tipo,
            int NumEscanos
        );

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearEleccionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Titulo))
                return BadRequest(ApiResult<object>.Fail("El título es requerido."));

            if (req.FechaFinUtc <= req.FechaInicioUtc)
                return BadRequest(ApiResult<object>.Fail("FechaFinUtc debe ser mayor que FechaInicioUtc."));

            if (req.Tipo == TipoEleccion.Plancha && req.NumEscanos <= 0)
                return BadRequest(ApiResult<object>.Fail("Plancha requiere NumEscanos > 0."));

            var e = new Eleccion
            {
                Titulo = req.Titulo.Trim(),
                Descripcion = req.Descripcion,
                FechaInicioUtc = req.FechaInicioUtc,
                FechaFinUtc = req.FechaFinUtc,
                Tipo = req.Tipo,
                NumEscanos = req.Tipo == TipoEleccion.Nominal ? 0 : req.NumEscanos,
                Estado = EstadoEleccion.Pendiente
            };

            _db.Elecciones.Add(e);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<Eleccion>.Ok(e, "Elección creada."));
        }

        [HttpPost("{id:int}/activar")]
        public async Task<IActionResult> Activar(int id)
        {
            var e = await _db.Elecciones.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            if (e.Tipo == TipoEleccion.Plancha)
            {
                var listas = await _db.Listas.CountAsync(l => l.EleccionId == id);
                if (listas == 0)
                    return BadRequest(ApiResult<object>.Fail("Elección plancha requiere al menos 1 lista."));
            }
            else
            {
                var cand = await _db.Candidatos.CountAsync(c => c.EleccionId == id);
                if (cand == 0)
                    return BadRequest(ApiResult<object>.Fail("Elección nominal requiere al menos 1 candidato."));
            }

            e.Estado = EstadoEleccion.Activa;
            await _db.SaveChangesAsync();

            return Ok(ApiResult<object>.Ok(new { id = e.Id, estado = e.Estado }, "Elección activada."));
        }

        [HttpPost("{id:int}/cerrar")]
        public async Task<IActionResult> Cerrar(int id)
        {
            var e = await _db.Elecciones.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            e.Estado = EstadoEleccion.Cerrada;
            await _db.SaveChangesAsync();

            return Ok(ApiResult<object>.Ok(new { id = e.Id, estado = e.Estado }, "Elección cerrada."));
        }
    }
}
