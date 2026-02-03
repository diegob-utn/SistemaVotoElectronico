using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    public enum TipoEleccion
    {
        Nominal = 0,
        Plancha = 1,
        Mixta = 2
    }

    public enum EstadoEleccion
    {
        Pendiente = 0,
        Activa = 1,
        Cerrada = 2,
        Cancelada = 3
    }
    public enum ModoUbicacion
    {
        Ninguna = 0,      // Global / online
        PorUbicacion = 1, // Por nodo del árbol (provincia/campus/facultad/etc)
        PorRecinto = 2    // Por recinto electoral
    }
}
