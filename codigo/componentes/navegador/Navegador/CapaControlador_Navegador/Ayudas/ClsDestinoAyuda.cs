//-----------------------------------------------------
// - Hecho por: Evelyn Sofía Andrade Luna
// - Carne: 9959-23-1224
// - Fecha: 26/10/2026
namespace CapaControlador_Navegador
{
    // Resultado de buscar la ayuda de una aplicación: en qué .chm está y qué página hay que abrir.
    // Si RutaArchivo viene vacía es porque no hay ayuda disponible y Mensaje explica el motivo.
    // Si Pagina viene vacía se abre el inicio del .chm.
    public class ClsDestinoAyuda
    {
        private string _RutaArchivo = "";
        private string _Pagina = "";
        private string _Mensaje = "";

        public string RutaArchivo
        {
            get { return _RutaArchivo; }
            set { _RutaArchivo = value; }
        }

        public string Pagina
        {
            get { return _Pagina; }
            set { _Pagina = value; }
        }

        public string Mensaje
        {
            get { return _Mensaje; }
            set { _Mensaje = value; }
        }
    }
}
//--------------------------------------------------
// - Final: Evelyn Sofía Andrade Luna
// - Carne: 9959-23-1224
