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
        public static string GetEndpoint(string pto_alm, string mode)
        {
            if (mode == "PRO")
            {
                if (pto_alm == "870")
                {
                    return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=VAL&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
                }
                else if(pto_alm == "521")
                {
                    return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=110678&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
                }
                else if(pto_alm == "876")
                {
                    return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=110408&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
                }
                    return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=110403&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
            }
            else 
            {
                if (pto_alm == "870")
                {
                    return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=VAL&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
                } 
                else if(pto_alm == "521")
                {
                    return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=110678&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
                } 
                else if(pto_alm == "876")
                {
                    return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=110408&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
                }
                    return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/allocation?order_dtl_id__order_id__order_nbr={########}&order_dtl_id__order_id__facility_id__code=110403&values_list=order_dtl_id__order_id__order_nbr:OrderNumber,order_dtl_id__order_id__status_id__description:OrderStatus,to_inventory_id__container_id__container_nbr:Olpn,to_inventory_id__item_id__description:Product,to_inventory_id__container_id__status_id__description:LpnStatus,to_inventory_id__item_id__part_a:Sku,packed_qty:Quantity&packed_qty__gt=0";
            }
            
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
            return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/order_hdr?company_id__code=GPOSAN&facility_id__code={???}&order_nbr={########}&values_list=order_nbr:OrderNumber,status_id:IdEstatus,stop_ship_flg:EnvioBloqueado";
        }
        private static string GetEndpointOrderDev()
        {
            return "https://te6.wms.ocs.oraclecloud.com:443/sears2_test/wms/lgfapi/v10/entity/order_hdr?company_id__code=GPOSAN&facility_id__code={???}&order_nbr={########}&values_list=order_nbr:OrderNumber,status_id:IdEstatus,stop_ship_flg:EnvioBloqueado";
        }
        public static string GetASNPerLoad(string mode)
        {
            //{#####}--LOAD
            //{$$$}--Facility
            if (mode == "PRO")
            {
                return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/ib_shipment?load_id__load_nbr={#####}&load_id__facility_id__code={$$$}&values_list=id:Id,shipment_nbr:NumAsn,cust_field_1:Factura,cust_field_2:Monto,cust_field_3:Pais,cust_field_4:NAR";
            }
            return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/ib_shipment?load_id__load_nbr={#####}&load_id__facility_id__code={$$$}&values_list=id:Id,shipment_nbr:NumAsn,cust_field_1:Factura,cust_field_2:Monto,cust_field_3:Pais,cust_field_4:NAR";
        }
        public static string PatchAsnById(string mode)
        {
            if (mode == "PRO")
            {
                return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/ib_shipment/{id}";
            }
            return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/ib_shipment/{id}";
        }
        public static string PatchLoadById(string mode)
        {
            if (mode == "PRO")
            {
                return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/load/{id}";
            }
            return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/load/{id}";
        }
        
        public static string ShipReshipment(string mode)
        {
            if (mode == "PRO")
            {
                return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/load/ship/";
            }
            return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/load/ship/";
        }

        public static string GetLoads(string mode)
        {
            if(mode == "PRO")
            {
                return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/load?facility_id__code={facility}&type=O&status_id__in=0,10,30,40,50,80,85&values_list=id:IdCarga,load_nbr:CargaSalida,status_id:IdEstatusCarga,cust_field_2:SigAlmacen";
            }
            return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/load?facility_id__code={facility}&type=O&status_id__in=0,10,30,40,50,80,85&values_list=id:IdCarga,load_nbr:CargaSalida,status_id:IdEstatusCarga,cust_field_2:SigAlmacen";
        }

        public static string GetIdStatusByLoad(string mode)
        {
            if(mode == "PRO")
            {
                return "https://e6.wms.ocs.oraclecloud.com/sears2/wms/lgfapi/v10/entity/load?facility_id__code={facility}&type=O&load_nbr={load}&values_list=status_id:IdEstatusCarga";
            }
            return "https://te6.wms.ocs.oraclecloud.com/sears2_test/wms/lgfapi/v10/entity/load?facility_id__code={facility}&type=O&load_nbr={load}&values_list=status_id:IdEstatusCarga";
        }
    }
}
