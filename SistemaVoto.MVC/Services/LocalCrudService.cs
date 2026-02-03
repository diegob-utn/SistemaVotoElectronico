using Microsoft.EntityFrameworkCore;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.MVC.Services;

/// <summary>
/// Servicio de CRUD local usando Entity Framework directamente
/// Este servicio reemplaza las llamadas a la API externa para operaciones locales
/// </summary>
public class LocalCrudService
{
    private readonly SistemaVotoDbContext _context;

    public LocalCrudService(SistemaVotoDbContext context)
    {
        _context = context;
    }

    // ==================== ELECCIONES ====================
    
    public List<Eleccion> GetElecciones()
    {
        return _context.Elecciones.ToList();
    }

    public Eleccion? GetEleccion(int id)
    {
        return _context.Elecciones.Find(id);
    }

    public Eleccion CreateEleccion(Eleccion eleccion)
    {
        _context.Elecciones.Add(eleccion);
        _context.SaveChanges();
        return eleccion;
    }

    public bool UpdateEleccion(Eleccion eleccion)
    {
        var existing = _context.Elecciones.Find(eleccion.Id);
        if (existing == null) return false;
        
        _context.Entry(existing).CurrentValues.SetValues(eleccion);
        _context.SaveChanges();
        return true;
    }

    public bool DeleteEleccion(int id)
    {
        var eleccion = _context.Elecciones.Find(id);
        if (eleccion == null) return false;
        
        _context.Elecciones.Remove(eleccion);
        _context.SaveChanges();
        return true;
    }

    // ==================== CANDIDATOS ====================
    
    public List<Candidato> GetCandidatos()
    {
        return _context.Candidatos.Include(c => c.Eleccion).Include(c => c.Lista).ToList();
    }

    public List<Candidato> GetCandidatosByEleccion(int eleccionId)
    {
        return _context.Candidatos.Where(c => c.EleccionId == eleccionId).ToList();
    }

    public Candidato? GetCandidato(int id)
    {
        return _context.Candidatos.Find(id);
    }

    public Candidato CreateCandidato(Candidato candidato)
    {
        _context.Candidatos.Add(candidato);
        _context.SaveChanges();
        return candidato;
    }

    public bool UpdateCandidato(Candidato candidato)
    {
        var existing = _context.Candidatos.Find(candidato.Id);
        if (existing == null) return false;
        
        _context.Entry(existing).CurrentValues.SetValues(candidato);
        _context.SaveChanges();
        return true;
    }

    public bool DeleteCandidato(int id)
    {
        var candidato = _context.Candidatos.Find(id);
        if (candidato == null) return false;
        
        _context.Candidatos.Remove(candidato);
        _context.SaveChanges();
        return true;
    }

    // ==================== LISTAS ====================
    
    public List<Lista> GetListas()
    {
        return _context.Listas.Include(l => l.Eleccion).ToList();
    }

    public List<Lista> GetListasByEleccion(int eleccionId)
    {
        return _context.Listas.Where(l => l.EleccionId == eleccionId).ToList();
    }

    public Lista? GetLista(int id)
    {
        return _context.Listas.Find(id);
    }

    public Lista CreateLista(Lista lista)
    {
        _context.Listas.Add(lista);
        _context.SaveChanges();
        return lista;
    }

    public bool UpdateLista(Lista lista)
    {
        var existing = _context.Listas.Find(lista.Id);
        if (existing == null) return false;
        
        _context.Entry(existing).CurrentValues.SetValues(lista);
        _context.SaveChanges();
        return true;
    }

    public bool DeleteLista(int id)
    {
        var lista = _context.Listas.Find(id);
        if (lista == null) return false;
        
        _context.Listas.Remove(lista);
        _context.SaveChanges();
        return true;
    }

    // ==================== VOTOS ====================
    
    // GetVotos moved to end with Includes

    public List<Voto> GetVotosByEleccion(int eleccionId)
    {
        return _context.Votos
            .Include(v => v.Eleccion)
            .Include(v => v.Candidato)
            .Include(v => v.Lista)
            .Where(v => v.EleccionId == eleccionId)
            .ToList();
    }

    public Voto? GetVoto(int id)
    {
        return _context.Votos.Find(id);
    }

    public Voto CreateVoto(Voto voto)
    {
        _context.Votos.Add(voto);
        _context.SaveChanges();
        return voto;
    }

    // Sistema de voto anónimo - no se rastrea por UsuarioId
    // Para verificar si un usuario votó, uso HistorialVoto

    // ==================== UBICACIONES ====================
    
    public List<Ubicacion> GetUbicaciones()
    {
        return _context.Ubicaciones.ToList();
    }

    public Ubicacion? GetUbicacion(int id)
    {
        return _context.Ubicaciones.Find(id);
    }

    public Ubicacion CreateUbicacion(Ubicacion ubicacion)
    {
        _context.Ubicaciones.Add(ubicacion);
        _context.SaveChanges();
        return ubicacion;
    }

    public bool UpdateUbicacion(Ubicacion ubicacion)
    {
        var existing = _context.Ubicaciones.Find(ubicacion.Id);
        if (existing == null) return false;
        
        _context.Entry(existing).CurrentValues.SetValues(ubicacion);
        _context.SaveChanges();
        return true;
    }

    public bool DeleteUbicacion(int id)
    {
        var ubicacion = _context.Ubicaciones.Find(id);
        if (ubicacion == null) return false;
        
        _context.Ubicaciones.Remove(ubicacion);
        _context.SaveChanges();
        return true;
    }

    // ==================== RECINTOS ====================
    
    public List<RecintoElectoral> GetRecintos()
    {
        return _context.Recintos.Include(r => r.Ubicacion).ToList();
    }

    public RecintoElectoral? GetRecinto(int id)
    {
        return _context.Recintos.Find(id);
    }

    public RecintoElectoral CreateRecinto(RecintoElectoral recinto)
    {
        _context.Recintos.Add(recinto);
        _context.SaveChanges();
        return recinto;
    }

    public bool UpdateRecinto(RecintoElectoral recinto)
    {
        var existing = _context.Recintos.Find(recinto.Id);
        if (existing == null) return false;
        
        _context.Entry(existing).CurrentValues.SetValues(recinto);
        _context.SaveChanges();
        return true;
    }

    public bool DeleteRecinto(int id)
    {
        var recinto = _context.Recintos.Find(id);
        if (recinto == null) return false;
        
        _context.Recintos.Remove(recinto);
        _context.SaveChanges();
        return true;
    }

    // ==================== ELECCION-UBICACION ====================
    
    public List<EleccionUbicacion> GetEleccionUbicaciones()
    {
        return _context.EleccionUbicaciones.ToList();
    }

    public List<EleccionUbicacion> GetEleccionUbicacionesByEleccion(int eleccionId)
    {
        return _context.EleccionUbicaciones.Where(eu => eu.EleccionId == eleccionId).ToList();
    }

    public EleccionUbicacion CreateEleccionUbicacion(EleccionUbicacion eu)
    {
        _context.EleccionUbicaciones.Add(eu);
        _context.SaveChanges();
        return eu;
    }

    public bool DeleteEleccionUbicacion(int id)
    {
        var eu = _context.EleccionUbicaciones.Find(id);
        if (eu == null) return false;
        
        _context.EleccionUbicaciones.Remove(eu);
        _context.SaveChanges();
        return true;
    }

    public List<Voto> GetVotos()
    {
        return _context.Votos
            .Include(v => v.Eleccion)
            .Include(v => v.Candidato)
            .Include(v => v.Lista)
            .ToList();
    }
    
    // ==================== HISTORIAL VOTO ====================

    public bool HasVoted(int eleccionId, string usuarioId)
    {
        return _context.HistorialVotos.Any(h => h.EleccionId == eleccionId && h.UsuarioId == usuarioId);
    }

    public void RegisterVotoHistorial(int eleccionId, string usuarioId)
    {
        var historial = new HistorialVoto
        {
            EleccionId = eleccionId,
            UsuarioId = usuarioId,
            FechaVotoUtc = DateTime.UtcNow
        };
        _context.HistorialVotos.Add(historial);
        _context.SaveChanges();
    }
}
