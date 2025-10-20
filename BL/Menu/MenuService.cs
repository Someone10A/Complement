using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Menu
{
    public class MenuService
    {
        public static List<MenuItem> GetMenu(string usu_id, string cv_area, string sub_rol)
        {
            var menu = new List<MenuItem>();

            menu.Add(new MenuItem("Home", "Home", "Index", "bi-house"));

            if (sub_rol == "SIS")
            {
                menu.AddRange(GetAllOptions());
                return menu;
            }

            if (cv_area == "CIC")
            {
                menu.Add(new MenuItem("Ordenes", "Importations", "GetOrders", "bi-receipt"));
                menu.Add(new MenuItem("Consulta Hija", "Importations", "GetRelation", "bi-search"));
            }

            if (cv_area == "CON" || cv_area == "GGB")
            {
                menu.Add(new MenuItem("Rutas", "TrackingManager", "GetTrackingPerDay", "bi-truck"));
                menu.Add(new MenuItem("Busqueda", "BaseControl", "GetOrdersPerData", "bi-search"));
                menu.Add(new MenuItem("MatcxDia", "LastMileDelivery", "GetShipmentsByDay", "bi-calendar3-event"));
                menu.Add(new MenuItem("MatchxRango", "LastMileDelivery", "GetShipmentsByQuery", "bi-calendar2-week"));
            }

            if (sub_rol == "BAS" || sub_rol == "INT" || sub_rol == "SUP" || cv_area == "GGB")
            {
                menu.Add(new MenuItem("BaseControl", "BaseControl", "BaseControl", "bi-clipboard-check"));
            }

            if (sub_rol == "SUP" || cv_area == "GGB")
            {
                menu.Add(new MenuItem("BaseControlOTM", "BaseControl", "BaseControlPast", "bi-clipboard-check-fill"));
            }

            return menu;
        }

        private static List<MenuItem> GetAllOptions()
        {
            return new List<MenuItem>
            {
                //new("Privacy", "Home", "Privacy", "bi-shield-lock"),
                new("Ordenes", "Importations", "GetOrders", "bi-receipt"),
                new("Consulta Hija", "Importations", "GetRelation", "bi-search"),
                new("Rutas", "TrackingManager", "GetTrackingPerDay", "bi-truck"),
                new("Busqueda", "BaseControl", "GetOrdersPerData", "bi-search"),
                new("MatcxDia", "LastMileDelivery", "GetShipmentsByDay", "bi-calendar3-event"),
                new("MatchxRango", "LastMileDelivery", "GetShipmentsByQuery", "bi-calendar2-week"),
                new("BaseControl", "BaseControl", "BaseControl", "bi-clipboard-check"),
                new("BaseControlOTM", "BaseControl", "BaseControlPast", "bi-clipboard-check-fill"),
                new("Mantenimiento", "Maintenance", "InfoByScn", "bi-geo-alt")

            };
        }
    }

    public record MenuItem(string Title, string Controller, string Action, string Icon);

}
