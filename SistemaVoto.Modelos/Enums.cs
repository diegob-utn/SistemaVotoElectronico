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
        Plancha = 1
    }

    public enum EstadoEleccion
    {
        Pendiente = 0,
        Activa = 1,
        Cerrada = 2,
        Cancelada = 3
    }
}
