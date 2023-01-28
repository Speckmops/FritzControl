using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FritzControl
{
    class StaticRouteTable
    {
        public string pid { get; set; }
        public Hide hide { get; set; }
        public List<object> time { get; set; }
        public Data data { get; set; }
        public string sid { get; set; }
    }

    class Hide
    {
        public bool shareUsb { get; set; }
        public bool liveTv { get; set; }
        public bool faxSet { get; set; }
        public bool dectRdio { get; set; }
        public bool dectMoniEx { get; set; }
        public bool rss { get; set; }
        public bool mobile { get; set; }
        public bool dectMail { get; set; }
        public bool ssoSet { get; set; }
        public bool dectMoni { get; set; }
        public bool liveImg { get; set; }
    }

    class Data
    {
        public StaticRoutes staticRoutes { get; set; }
    }

    class StaticRoutes
    {
        public List<Route> route { get; set; }
    }

    class Route
    {
        public string _node { get; set; }
        public int id => _node == null ? -1 : int.Parse(_node.Replace("route", ""));
        public string ipaddr { get; set; }
        public string netmask { get; set; }
        public string activated { get; set; }
        public string gateway { get; set; }

        public override string ToString()
        {
            return $"{id}: {ipaddr}/{netmask} -> {gateway} {(activated == "1" ? "active":"inactive")}";
        }
    }

}
