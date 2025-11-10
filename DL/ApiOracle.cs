using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DL
{
    public class ApiOracle
    {
        public static string GetOracleUsr(string mode)
        {
            return "intwms2";
        }
        public static string GetOraclePwd(string mode)
        {
            return "Oracle2024@90";
        }
        public static string GetEndpoint(string mode)
        {
            return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=VAL&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
        }
        public static string GetEndpointOrder(string mode)
        {
            if (mode == "PRO")
            {
                return GetEndpointOrderPro();
            }
            else
            {
                return GetEndpointOrderDev();
            }
        }
        private static string GetEndpointOrderPro()
        {
            return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/order_hdr?company_id__code=GPOSAN&facility_id__code=VAL&order_nbr={########}&values_list=order_nbr:OrderNumber,status_id:IdEstatus,stop_ship_flg:EnvioBloqueado";
        }
        private static string GetEndpointOrderDev()
        {
            return "https://te6.wms.ocs.oraclecloud.com:443/sears2_test/wms/lgfapi/v10/entity/order_hdr?company_id__code=GPOSAN&facility_id__code=VAL&order_nbr={########}&values_list=order_nbr:OrderNumber,status_id:IdEstatus,stop_ship_flg:EnvioBloqueado";
        }
    }
}
