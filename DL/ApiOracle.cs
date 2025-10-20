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
    }
}
